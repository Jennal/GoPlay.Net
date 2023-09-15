module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  modulePaths: ['<rootDir>', 'node_modules'],
  moduleFileExtensions: ['js', 'mjs', 'cjs', 'jsx', 'ts', 'tsx', 'json', 'node', 'd.ts'],
  testMatch: ['**/*.test.ts'],
  testPathIgnorePatterns: ['/node_modules/', '/dist/']
};