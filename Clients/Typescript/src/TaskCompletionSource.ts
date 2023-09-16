export default class TaskCompletionSource<T> {
    private _promise: Promise<T>;
    private _resolve!: (value: T | PromiseLike<T>) => void;
  
    constructor() {
      this._promise = new Promise<T>((resolve, reject) => {
        this._resolve = resolve;
      });
    }
  
    get promise(): Promise<T> {
      return this._promise;
    }
  
    set result(value: T) {
      this._resolve(value);
    }
  }