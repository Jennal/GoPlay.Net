import {describe, expect, it, afterEach} from '@jest/globals';
import goplay from '../../src/goplay';
import { GoPlay } from '../../src/pkg.pb';

/**
 * 覆盖 P0-3 / P0-4：
 *  - disconnect 重入保护：并发两次 disconnect 不应覆盖 disconnectTask
 *  - onclose 广播 NETWORK_ERROR：在飞 request 立即失败，不用等 REQUEST 超时
 */

const URL = process.env.GOPLAY_TEST_URL || 'ws://127.0.0.1:8888';

jest.setTimeout(15000);

async function ensureDisconnected() {
  if ((goplay as any).isConnected) {
    await goplay.disconnect();
  }
  // 清掉过滤器，避免跨测漏状态
  (goplay as any).sendFilters = [];
  (goplay as any).recvFilters = [];
  (goplay as any).errorFilters = [];
  goplay.removeAllListeners();
}

describe('goplay disconnect semantics (integration)', () => {
  afterEach(async () => {
    await ensureDisconnected();
  });

  it('concurrent disconnect() calls both resolve to true without hanging', async () => {
    expect(await goplay.connect(URL)).toBe(true);

    const p1 = goplay.disconnect();
    const p2 = goplay.disconnect();

    const [r1, r2] = await Promise.all([p1, p2]);
    expect(r1).toBe(true);
    expect(r2).toBe(true);
    // 完成后置 null，不得残留
    expect((goplay as any).disconnectTask).toBeNull();
  });

  it('disconnect() when never connected should return true fast', async () => {
    const r = await goplay.disconnect();
    expect(r).toBe(true);
  });

  it('onclose should broadcast NETWORK_ERROR to in-flight requests', async () => {
    expect(await goplay.connect(URL)).toBe(true);

    // 先用 sendFilter 拦住请求，让它永远在 requestMap 里而不真的走到 server。
    const block = () => false;
    goplay.addSendFilter(block);

    const req = new GoPlay.Core.Protocols.PbString();
    req.Value = 'stuck';
    const pending = goplay.request('test.echo', req, GoPlay.Core.Protocols.PbString);

    // 让事件循环跑一轮，确保 request 已入 requestMap + 挂了 once listener
    await new Promise(r => setTimeout(r, 50));

    await goplay.disconnect();

    const resp: any = await pending;
    expect(resp.status.Code).toBe(GoPlay.Core.Protocols.StatusCode.Error);
    expect(resp.status.Message).toBe('NETWORK_ERROR');

    goplay.removeSendFilter(block);
  });

  it('reconnect after disconnect should succeed (P0-5: connectTimeout cleaned)', async () => {
    expect(await goplay.connect(URL)).toBe(true);
    await goplay.disconnect();

    expect(await goplay.connect(URL)).toBe(true);
    // 连接超时 timer 必须在 handshake 成功时清掉
    expect((goplay as any).connectTimeOutId).toBeNull();

    // 请求仍能正常走通
    const req = new GoPlay.Core.Protocols.PbString();
    req.Value = 'after-reconnect';
    const resp: any = await goplay.request('test.echo', req, GoPlay.Core.Protocols.PbString);
    expect(resp.status.Code).toBe(GoPlay.Core.Protocols.StatusCode.Success);
  });
});
