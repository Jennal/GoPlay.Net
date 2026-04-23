module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  modulePaths: ['<rootDir>', 'node_modules'],
  moduleFileExtensions: ['js', 'mjs', 'cjs', 'jsx', 'ts', 'tsx', 'json', 'node', 'd.ts'],
  testMatch: ['**/*.test.ts'],
  // e2e 默认不跑：需要真实起 Demo.WsServer。跑 `bash scripts/test.sh` 会显式把 e2e 放回来。
  testPathIgnorePatterns: ['/node_modules/', '/dist/', '/unit_test/e2e/'],
};
