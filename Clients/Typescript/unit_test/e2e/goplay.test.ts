import {describe, expect, it, afterAll} from '@jest/globals';
import goplay from '../../src/goplay';

describe('goplay', () => {
  afterAll(async () => {
    // 不 disconnect 会让 HeartBeat setInterval + ws 保持 node event loop 活着，
    // jest 会打 "open handles" 警告并延迟退出。
    await goplay.disconnect();
  });

  it('should reconnect', async () => {
    var ok = await goplay.connect('ws://localhost:8888');
    expect(ok).toBe(true);
    ok = await goplay.connect('ws://127.0.0.1:8888');
    expect(ok).toBe(true);
  });
});
