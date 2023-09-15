export default class Emitter {
    private _callbacks = {};

    public get callbacks() {
        return this._callbacks;
    }

    /**
     * Listen on the given `event` with `fn`.
     *
     * @param {String} event
     * @param {Function} fn
     * @return {Emitter}
     * @api public
     */
    public on = this.addEventListener;
    public addListener = this.addEventListener;
    public addEventListener(event, fn) {
        (this._callbacks[event] = this._callbacks[event] || []).push(fn);
        return this;
    }

    /**
     * Adds an `event` listener that will be invoked a single
     * time then automatically removed.
     *
     * @param {String} event
     * @param {Function} fn
     * @return {Emitter}
     * @api public
     */
    public once = this.addEventListenerOnce;
    public addListenerOnce = this.addEventListenerOnce;
    public addEventListenerOnce(event, fn) {
        var self = this;

        function on() {
            self.off(event, on);
            fn.apply(this, arguments);
        }

        on.fn = fn;
        this.on(event, on);
        return this;
    }

    /**
     * Remove the given callback for `event` or all
     * registered callbacks.
     *
     * @param {String} event
     * @param {Function} fn
     * @return {Emitter}
     * @api public
     */
    public off = this.removeEventListener;
    public removeListener = this.removeEventListener;
    public removeEventListener(...args: any[]) {
        let [event, fn] = args;

        // remove all handlers
        if (1 == args.length) {
            delete this._callbacks[event];
            return this;
        }

        // specific event
        var callbacks = this._callbacks[event];
        if (!callbacks) return this;

        // remove specific handler
        var cb;
        for (var i = 0; i < callbacks.length; i++) {
            cb = callbacks[i];
            if (cb === fn || cb.fn === fn) {
                callbacks.splice(i, 1);
                break;
            }
        }
        return this;
    }

    public removeAllListeners() {
        this._callbacks = {};
        return this;
    }

    /**
     * Emit `event` with the given args.
     *
     * @param {String} event
     * @param {Mixed} ...
     * @return {Emitter}
     */
    public emit(event, ...args: any[]) {
        var callbacks = this._callbacks[event];

        if (callbacks) {
            callbacks = callbacks.slice(0);
            for (var i = 0, len = callbacks.length; i < len; ++i) {
                callbacks[i].apply(this, args);
            }
        }

        return this;
    };

    /**
     * Return array of callbacks for `event`.
     *
     * @param {String} event
     * @return {Array}
     * @api public
     */
    public listeners(event) {
        return this._callbacks[event] || [];
    };

    /**
     * Check if this emitter has `event` handlers.
     *
     * @param {String} event
     * @return {Boolean}
     * @api public
     */
    public hasListeners(event) {
        return !!this.listeners(event).length;
    };
}