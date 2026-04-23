import {describe, expect, it, beforeEach, jest} from '@jest/globals';
import goplay from '../src/goplay';
import Package from '../src/Package';
import { GoPlay } from '../src/pkg.pb';

/**
 * P2-15：filter pipeline 的离线单测。
 * 不依赖 ws：send 里 filter 是最早的短路点；即便 ws 为 null，drain 里会 early-return。
 * 通过 private 字段 sendQueue 的长度间接验证"包是否被短路 / 是否入队"。
 */

function mkNotifyPack(): Package<any> {
  return Package.createFromData(
    1,
    null,
    GoPlay.Core.Protocols.PackageType.Notify,
    GoPlay.Core.Protocols.EncodingType.Protobuf,
  );
}

function resetGoplayState() {
  const g = goplay as any;
  g.sendFilters = [];
  g.recvFilters = [];
  g.errorFilters = [];
  g.sendQueue.length = 0;
  g.requestMap = {};
  g.pushMap = {};
  g.chunkMap = {};
  goplay.removeAllListeners();
}

describe('goplay filter pipeline', () => {
  beforeEach(() => {
    resetGoplayState();
  });

  describe('sendFilter', () => {
    it('returning false should short-circuit send (nothing enqueued)', () => {
      const fn = jest.fn(() => false);
      goplay.addSendFilter(fn);
      goplay.send(mkNotifyPack());
      expect(fn).toHaveBeenCalledTimes(1);
      expect((goplay as any).sendQueue.length).toBe(0);
    });

    it('returning true should let pack through to sendQueue', () => {
      goplay.addSendFilter(() => true);
      goplay.send(mkNotifyPack());
      // 空 data 包 split 返回 [self]，encode 出一条 buffer 入队。
      expect((goplay as any).sendQueue.length).toBe(1);
    });

    it('returning non-false (undefined) should be treated as pass', () => {
      // 仅 "=== false" 才阻断；undefined / null 不应阻断
      goplay.addSendFilter(() => undefined as any);
      goplay.send(mkNotifyPack());
      expect((goplay as any).sendQueue.length).toBe(1);
    });

    it('multiple filters: first returning false short-circuits and later are NOT called', () => {
      const a = jest.fn(() => false);
      const b = jest.fn(() => true);
      goplay.addSendFilter(a);
      goplay.addSendFilter(b);
      goplay.send(mkNotifyPack());
      expect(a).toHaveBeenCalledTimes(1);
      expect(b).not.toHaveBeenCalled();
      expect((goplay as any).sendQueue.length).toBe(0);
    });

    it('removeSendFilter should restore pass-through', () => {
      const block = () => false;
      goplay.addSendFilter(block);
      goplay.send(mkNotifyPack());
      expect((goplay as any).sendQueue.length).toBe(0);

      goplay.removeSendFilter(block);
      goplay.send(mkNotifyPack());
      expect((goplay as any).sendQueue.length).toBe(1);
    });

    it('removeSendFilter should only remove the requested function', () => {
      const f1 = jest.fn(() => true);
      const f2 = jest.fn(() => true);
      goplay.addSendFilter(f1);
      goplay.addSendFilter(f2);
      goplay.removeSendFilter(f1);

      goplay.send(mkNotifyPack());
      expect(f1).not.toHaveBeenCalled();
      expect(f2).toHaveBeenCalledTimes(1);
    });
  });

  describe('recvFilter', () => {
    it('returning false should block dispatch to Push handler', () => {
      // 构造一个"已解码"的 Push 包，直接走 private dispatchPack
      const header = new GoPlay.Core.Protocols.Header();
      header.PackageInfo = new GoPlay.Core.Protocols.PackageInfo();
      header.PackageInfo.Type = GoPlay.Core.Protocols.PackageType.Push;
      header.PackageInfo.Route = 42;
      header.PackageInfo.Id = 1;
      header.PackageInfo.EncodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;

      const pack = new Package(header, null, null) as any;

      // 没 handshake 时 getPushKey 走 getRoute -> "" 分支；用 "" 订阅也行，但我们直接监听 emitter。
      const seen = jest.fn();
      // 注入一个 pushMap 条目，走 type 分支以命中 onPush 的 emit 路径
      (goplay as any).pushMap[''] = null;
      goplay.on('', seen);

      const blocker = jest.fn(() => false);
      goplay.addRecvFilter(blocker);
      (goplay as any).dispatchPack(pack);

      expect(blocker).toHaveBeenCalledTimes(1);
      expect(seen).not.toHaveBeenCalled();
    });

    it('returning true should allow dispatch to continue', () => {
      const header = new GoPlay.Core.Protocols.Header();
      header.PackageInfo = new GoPlay.Core.Protocols.PackageInfo();
      header.PackageInfo.Type = GoPlay.Core.Protocols.PackageType.Push;
      header.PackageInfo.Route = 42;
      header.PackageInfo.Id = 1;
      header.PackageInfo.EncodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;

      const pack = new Package(header, null, null) as any;

      const seen = jest.fn();
      goplay.on('', seen);

      goplay.addRecvFilter(() => true);
      (goplay as any).dispatchPack(pack);
      expect(seen).toHaveBeenCalledTimes(1);
    });

    it('removeRecvFilter should remove the filter', () => {
      const block = () => false;
      goplay.addRecvFilter(block);
      expect((goplay as any).recvFilters.length).toBe(1);
      goplay.removeRecvFilter(block);
      expect((goplay as any).recvFilters.length).toBe(0);
    });
  });

  describe('errorFilter', () => {
    it('should be invoked on onerror and NOT swallow ERROR event', () => {
      const filter = jest.fn();
      const listener = jest.fn();
      goplay.addErrorFilter(filter);
      goplay.on((goplay as any).Consts.Events.ERROR, listener);

      const err = new Error('x');
      goplay.onerror(err);

      expect(filter).toHaveBeenCalledTimes(1);
      expect(filter).toHaveBeenCalledWith(err);
      expect(listener).toHaveBeenCalledTimes(1);
    });

    it('throwing errorFilter should NOT break ERROR event dispatch', () => {
      const errSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
      goplay.addErrorFilter(() => { throw new Error('filter-boom'); });
      const listener = jest.fn();
      goplay.on((goplay as any).Consts.Events.ERROR, listener);

      expect(() => goplay.onerror(new Error('x'))).not.toThrow();
      expect(listener).toHaveBeenCalledTimes(1);
      errSpy.mockRestore();
    });

    it('removeErrorFilter should detach the filter', () => {
      const fn = jest.fn();
      goplay.addErrorFilter(fn);
      goplay.removeErrorFilter(fn);
      goplay.onerror(new Error('x'));
      expect(fn).not.toHaveBeenCalled();
    });
  });
});
