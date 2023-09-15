import TaskCompletionSource from '../TaskCompletionSource';

describe('TaskCompletionSource', () => {
  it('should resolve the promise with the correct value', async () => {
    // Create a new task completion source
    const tcs = new TaskCompletionSource<string>();

    // Set the result of the task completion source
    tcs.result = 'Hello, world!';

    // Wait for the promise to be resolved
    const result = await tcs.promise;

    // Verify that the promise was resolved with the correct value
    expect(result).toEqual('Hello, world!');
  });

  it('should resolve the promise then the correct value', async () => {
    // Create a new task completion source
    const tcs = new TaskCompletionSource<string>();

    // Set the result of the task completion source
    let result = 'OK';
    tcs.promise.then((r) => {
        result = r;
    });
    
    tcs.result = 'Hello, world!';    // Wait for the promise to be resolved    

    await tcs.promise;
    // Verify that the promise was resolved with the correct value
    expect(result).toEqual('Hello, world!');
  });
});