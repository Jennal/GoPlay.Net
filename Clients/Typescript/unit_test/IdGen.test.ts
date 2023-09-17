import {describe, expect, test} from '@jest/globals';
import IdGen from '../src/IdGen';

describe('IdGen', () => {
  test('should generate unique IDs', () => {
    const idGen = new IdGen(255);
    const id1 = idGen.next();
    const id2 = idGen.next();
    console.log(id1, id2);
    expect(id1).not.toBe(id2);
  });
});