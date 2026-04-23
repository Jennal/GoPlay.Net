export default class IdGen {
    private id: number;
    private max: number;

    public constructor(max: number) {
        this.id = 0;
        this.max = max;
    }

    public next(): number {
        this.id = (this.id + 1) >>> 0;
        if (this.id > this.max || this.id === 0) this.id = 1;
        return this.id;
    }
}