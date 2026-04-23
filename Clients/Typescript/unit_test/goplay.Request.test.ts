import {describe, expect, it, beforeAll, afterAll} from '@jest/globals';
import goplay from '../src/goplay';
import { GoPlay } from '../src/pkg.pb';

/**
 * 覆盖 P0-1/3/6 + 端到端请求回路：
 *  - echo: Request/Response 正常链路
 *  - inc:  PbLong 类型回路
 *  - err:  服务端抛 ProcessorMethodException -> 非 Success 状态
 *  - 多并发：验证 getCallbackKey 只用 Id 情况下不会串响应
 */

const URL = process.env.GOPLAY_TEST_URL || 'ws://127.0.0.1:8888';

jest.setTimeout(15000);

describe('goplay request (integration)', () => {
  beforeAll(async () => {
    const ok = await goplay.connect(URL);
    expect(ok).toBe(true);
  });

  afterAll(async () => {
    await goplay.disconnect();
  });

  it('echo round-trip returns Success with server reply', async () => {
    const req = new GoPlay.Core.Protocols.PbString();
    req.Value = 'hello';
    const resp: any = await goplay.request('test.echo', req, GoPlay.Core.Protocols.PbString);
    expect(resp.status.Code).toBe(GoPlay.Core.Protocols.StatusCode.Success);
    expect(resp.data.Value).toBe('[Test] Server reply: hello');
  });

  it('inc round-trip on PbLong returns value+1', async () => {
    const req = new GoPlay.Core.Protocols.PbLong();
    req.Value = 41 as any;
    const resp: any = await goplay.request('test.inc', req, GoPlay.Core.Protocols.PbLong);
    expect(resp.status.Code).toBe(GoPlay.Core.Protocols.StatusCode.Success);
    // protobufjs 开启 Long 后返回 Long；用 Number() 统一折算。
    expect(Number(resp.data.Value)).toBe(42);
  });

  it('err route returns non-Success status (ProcessorMethodException)', async () => {
    const req = new GoPlay.Core.Protocols.PbString();
    req.Value = 'whatever';
    const resp: any = await goplay.request('test.err', req, GoPlay.Core.Protocols.PbString);
    expect(resp.status.Code).not.toBe(GoPlay.Core.Protocols.StatusCode.Success);
  });

  it('multiple concurrent requests resolve to correct responses (Id-only callback key)', async () => {
    // P0-6：getCallbackKey 仅用 Id；并发发多条 echo，校验各自返回的 Value 与入参一致。
    const inputs = ['a', 'b', 'c', 'd', 'e'];
    const resps: any[] = await Promise.all(
      inputs.map(v => {
        const r = new GoPlay.Core.Protocols.PbString();
        r.Value = v;
        return goplay.request('test.echo', r, GoPlay.Core.Protocols.PbString);
      })
    );
    expect(resps).toHaveLength(inputs.length);
    for (let i = 0; i < inputs.length; i++) {
      expect(resps[i].status.Code).toBe(GoPlay.Core.Protocols.StatusCode.Success);
      expect(resps[i].data.Value).toBe(`[Test] Server reply: ${inputs[i]}`);
    }
  });
});
