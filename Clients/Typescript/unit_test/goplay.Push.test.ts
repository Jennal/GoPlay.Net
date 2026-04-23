import {describe, expect, it, beforeAll, afterAll} from '@jest/globals';
import goplay from '../src/goplay';
import { GoPlay } from '../src/pkg.pb';

/**
 * 覆盖 P1-11 waitFor 与 push 链路：
 *  - notify -> server.Notify -> server.Push -> client "test.push" 事件
 *  - waitFor<T> 走 onceType 解码分支
 */

const URL = process.env.GOPLAY_TEST_URL || 'ws://127.0.0.1:8888';

jest.setTimeout(15000);

describe('goplay push & waitFor (integration)', () => {
  beforeAll(async () => {
    const ok = await goplay.connect(URL);
    expect(ok).toBe(true);
  });

  afterAll(async () => {
    goplay.removeAllListeners();
    // 清理 pushMap，避免后续测试用同名事件绑定不同类型时报错
    (goplay as any).pushMap = {};
    await goplay.disconnect();
  });

  it('onType("test.push") receives decoded PbString after notify', async () => {
    const received: string[] = [];
    const done = new Promise<void>(resolve => {
      goplay.onType('test.push', GoPlay.Core.Protocols.PbString, (data: any) => {
        received.push(data.Value);
        resolve();
      });
    });

    const req = new GoPlay.Core.Protocols.PbString();
    req.Value = 'ping';
    goplay.notify('test.notify', req);

    await done;
    expect(received[0]).toBe('Push: ping');
    // 清理订阅，避免影响下一个 test（静态 emitter）
    goplay.off('test.push');
  });

  it('waitFor<PbString>("test.push") resolves to decoded push payload', async () => {
    // 清掉上一条测试残留的 pushMap["test.push"] 绑定（同类型仍 OK，但保持干净）
    const waiter = goplay.waitFor('test.push', GoPlay.Core.Protocols.PbString);

    const req = new GoPlay.Core.Protocols.PbString();
    req.Value = 'pong';
    goplay.notify('test.notify', req);

    const data: any = await waiter;
    expect(data.Value).toBe('Push: pong');
    goplay.off('test.push');
  });

  it('waitFor without type should resolve to raw payload on first emit', async () => {
    // 非网络路径：直接 emit 验证 once 分支；不需要 server。
    const p = goplay.waitFor<number>('__local_evt__');
    goplay.emit('__local_evt__', 42);
    await expect(p).resolves.toBe(42);
  });
});
