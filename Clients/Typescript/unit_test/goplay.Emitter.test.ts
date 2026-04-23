import {describe, expect, it, beforeEach, jest} from '@jest/globals';
import goplay from '../src/goplay';

/**
 * goplay 对 Emitter 的静态转发层"存在性 + 贯通性"冒烟测试。
 * 底层 Emitter 的完整契约由 Emitter.test.ts 负责；这里只回答一个问题：
 *   goplay.on / off / once / emit / removeAllListeners / listeners / hasListeners
 *   这些静态方法是否真的把调用转交给了内部 emitter 实例。
 *
 * 一旦 goplay.ts 不小心把转发方法误删或误改签名，这个文件会第一时间红。
 */
describe('goplay Emitter facade', () => {
  beforeEach(() => {
    goplay.removeAllListeners();
  });

  it('exposes internal emitter instance with callbacks map', () => {
    expect(goplay.emitter).not.toBeNull();
    expect(goplay.emitter.callbacks).not.toBeNull();
  });

  it('on + emit + off round-trip through the facade', () => {
    const fn = jest.fn();

    goplay.on('evt', fn);
    expect(goplay.hasListeners('evt')).toBe(true);
    expect(goplay.listeners('evt')).toContain(fn);

    goplay.emit('evt', 'payload');
    expect(fn).toHaveBeenCalledWith('payload');

    goplay.off('evt', fn);
    expect(goplay.hasListeners('evt')).toBe(false);
  });

  it('once through the facade fires exactly once', () => {
    const fn = jest.fn();
    goplay.once('evt', fn);
    goplay.emit('evt');
    goplay.emit('evt');
    expect(fn).toHaveBeenCalledTimes(1);
  });

  it('removeAllListeners clears everything', () => {
    const fn1 = jest.fn();
    const fn2 = jest.fn();
    goplay.on('a', fn1);
    goplay.on('b', fn2);
    goplay.removeAllListeners();
    goplay.emit('a');
    goplay.emit('b');
    expect(fn1).not.toHaveBeenCalled();
    expect(fn2).not.toHaveBeenCalled();
  });
});
