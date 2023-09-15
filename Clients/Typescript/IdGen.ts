export default class IdGen {
    private id: number;
    private max: number;

    public constructor(max: number) {
        this.id = 0;
        this.max = max;
    }

    public next(): number {
        if (this.id++ > this.max) {
            this.id = 0;
        }

        return this.id;
    }
}