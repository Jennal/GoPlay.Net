import {describe, expect, test} from '@jest/globals';
import IdGen from '../src/IdGen';

describe('IdGen', () => {
  test('should generate unique IDs', () => {
    const idGen = new IdGen(255);
    const id1 = idGen.next();
    const id2 = idGen.next();
    expect(id1).not.toBe(id2);
  });

  test('should start at 1 (never emit 0)', () => {
    const idGen = new IdGen(16);
    expect(idGen.next()).toBe(1);
  });

  test('should be monotonic within the range', () => {
    const idGen = new IdGen(8);
    const seq = [];
    for (let i = 0; i < 6; i++) seq.push(idGen.next());
    expect(seq).toEqual([1, 2, 3, 4, 5, 6]);
  });

  test('should wrap back to 1 when exceeding max (inclusive upper bound)', () => {
    const idGen = new IdGen(3);
    const seq = [];
    for (let i = 0; i < 7; i++) seq.push(idGen.next());
    // 1,2,3 -> wrap -> 1,2,3 -> wrap -> 1
    expect(seq).toEqual([1, 2, 3, 1, 2, 3, 1]);
  });

  test('should never yield 0 even across wraps', () => {
    const idGen = new IdGen(5);
    for (let i = 0; i < 100; i++) {
      expect(idGen.next()).not.toBe(0);
    }
  });

  test('should handle uint32 max (0xFFFFFFFF) without overflow to negatives', () => {
    // P0-7：Package.ts 用 new IdGen(0xFFFFFFFF)，这里验证在 uint32 上限附近行为正确。
    const idGen = new IdGen(0xFFFFFFFF);
    // 手动快进到接近上限
    (idGen as any).id = 0xFFFFFFFE;
    const a = idGen.next();
    const b = idGen.next();
    const c = idGen.next();
    expect(a).toBe(0xFFFFFFFF);
    // 命中上限后 wrap 回 1
    expect(b).toBe(1);
    expect(c).toBe(2);
    // 保持在 uint32 无符号范围
    expect(a >>> 0).toBe(a);
    expect(b >>> 0).toBe(b);
  });
});
