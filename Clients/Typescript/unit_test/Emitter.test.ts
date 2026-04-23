import {describe, expect, it, beforeEach, jest} from '@jest/globals';
import Emitter from '../src/Emitter';

describe('Emitter', () => {
  let emitter: Emitter;

  beforeEach(() => {
    emitter = new Emitter();
  });

  it('should add and remove event listeners', () => {
    const listener1 = jest.fn();
    const listener2 = jest.fn();
    emitter.on('event1', listener1);
    emitter.on('event2', listener2);
    emitter.emit('event1');
    emitter.emit('event2');
    expect(listener1).toHaveBeenCalled();
    expect(listener2).toHaveBeenCalled();
    emitter.off('event1', listener1);
    emitter.emit('event1');
    expect(listener1).toHaveBeenCalledTimes(1);
    expect(listener2).toHaveBeenCalledTimes(1);
    emitter.off('event2');
    emitter.emit('event2');
    expect(listener1).toHaveBeenCalledTimes(1);
    expect(listener2).toHaveBeenCalledTimes(1);
  });

  it('should add and remove one-time event listeners', () => {
    const listener1 = jest.fn();
    const listener2 = jest.fn();
    emitter.once('event1', listener1);
    emitter.once('event2', listener2);
    emitter.emit('event1');
    emitter.emit('event1');
    emitter.emit('event2');
    emitter.emit('event2');
    expect(listener1).toHaveBeenCalledTimes(1);
    expect(listener2).toHaveBeenCalledTimes(1);
  });

  it('should remove all event listeners', () => {
    const listener1 = jest.fn();
    const listener2 = jest.fn();
    emitter.on('event1', listener1);
    emitter.on('event2', listener2);
    emitter.removeAllListeners();
    emitter.emit('event1');
    emitter.emit('event2');
    expect(listener1).not.toHaveBeenCalled();
    expect(listener2).not.toHaveBeenCalled();
  });

  it('should return an array of event listeners', () => {
    const listener1 = jest.fn();
    const listener2 = jest.fn();
    emitter.on('event1', listener1);
    emitter.on('event1', listener2);
    const listeners = emitter.listeners('event1');
    expect(listeners).toContain(listener1);
    expect(listeners).toContain(listener2);
  });

  it('should check if an event has listeners', () => {
    const listener1 = jest.fn();
    const listener2 = jest.fn();
    emitter.on('event1', listener1);
    expect(emitter.hasListeners('event1')).toBe(true);
    expect(emitter.hasListeners('event2')).toBe(false);
    emitter.on('event2', listener2);
    expect(emitter.hasListeners('event2')).toBe(true);
    emitter.off('event1', listener1);
    expect(emitter.hasListeners('event1')).toBe(false);
    emitter.off('event2');
    expect(emitter.hasListeners('event2')).toBe(false);
  });

  it('should pass all parameters to listeners', () => {
    const listener = function(...args) {
      expect(args).toEqual([1, 2, 3]);
    };

    const listener2 = function(a, b, c) {
      expect(a).toEqual(1);
      expect(b).toEqual(2);
      expect(c).toEqual(3);
    };
    emitter.on('event1', listener);
    emitter.on('event1', listener2);
    expect(emitter.hasListeners('event1')).toBe(true);
    expect(emitter.hasListeners('event2')).toBe(false);
    
    emitter.emit('event1', 1, 2, 3);
  });

  it('should await all async functions', async () => {
    const listener = function(...args) {
      expect(args).toEqual([1, 2, 3]);
    };

    let count = 0;
    const listener2 = async function(a, b, c) {
      await new Promise(resolve => setTimeout(resolve, 100));
      expect(a).toEqual(1);
      expect(b).toEqual(2);
      expect(c).toEqual(3);

      count++;
    };
    emitter.on('event1', listener);
    emitter.on('event1', listener2);
    expect(emitter.hasListeners('event1')).toBe(true);
    expect(emitter.hasListeners('event2')).toBe(false);
    
    await emitter.emitAsync('event1', 1, 2, 3);
    expect(count).toBe(1);
  });

  // P1-12: emit/emitAsync 对单个 listener 抛错做 try/catch 隔离，
  // 保证后续 listener 仍然正常被调用、整个 emit 调用不抛。
  it('emit: throwing listener should NOT break subsequent listeners', () => {
    const errSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    const before = jest.fn(() => { throw new Error('boom'); });
    const after = jest.fn();
    emitter.on('event1', before);
    emitter.on('event1', after);

    expect(() => emitter.emit('event1', 'x')).not.toThrow();
    expect(before).toHaveBeenCalledTimes(1);
    expect(after).toHaveBeenCalledTimes(1);
    expect(after).toHaveBeenCalledWith('x');
    expect(errSpy).toHaveBeenCalled();
    errSpy.mockRestore();
  });

  it('emitAsync: throwing sync listener should NOT break subsequent async listeners', async () => {
    const errSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    const before = jest.fn(() => { throw new Error('boom'); });
    let asyncRan = 0;
    const after = async () => { await new Promise(r => setTimeout(r, 10)); asyncRan++; };
    emitter.on('event1', before);
    emitter.on('event1', after);

    await emitter.emitAsync('event1');
    expect(before).toHaveBeenCalledTimes(1);
    expect(asyncRan).toBe(1);
    expect(errSpy).toHaveBeenCalled();
    errSpy.mockRestore();
  });

  it('emitAsync: rejecting async listener should NOT break sibling listeners', async () => {
    const errSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    const reject = async () => { throw new Error('async-boom'); };
    let sibRan = 0;
    const sibling = async () => { sibRan++; };
    emitter.on('event1', reject);
    emitter.on('event1', sibling);

    await emitter.emitAsync('event1');
    expect(sibRan).toBe(1);
    expect(errSpy).toHaveBeenCalled();
    errSpy.mockRestore();
  });
});