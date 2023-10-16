import {describe, expect, it, beforeEach, jest} from '@jest/globals';
import goplay from '../src/goplay';

describe('goplay', () => {
  it('should reconnect', async () => {
    var ok = await goplay.connect('ws://localhost:8888');
    expect(ok).toBe(true);
    ok = await goplay.connect('ws://127.0.0.1:8888');
    expect(ok).toBe(true);
  });
});