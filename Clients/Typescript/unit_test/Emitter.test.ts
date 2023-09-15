import Emitter from '../Emitter';

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
});