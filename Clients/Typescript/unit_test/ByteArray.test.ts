import {describe, expect, it} from '@jest/globals';
import { ByteArray, copyArray, strencode, strdecode } from '../src/ByteArray';

describe('ByteArray', () => {
  it('should write and read uint8 values', () => {
    const buffer = new ByteArray(1);
    buffer.writeUint8(0x12);
    expect(buffer.readUint8()).toEqual(0x12);
  });

  it('should write and read uint16 values', () => {
    const buffer = new ByteArray(2);
    buffer.writeUint16(0x1234);
    expect(buffer.readUint16()).toEqual(0x1234);
  });

  it('should write and read uint32 values', () => {
    const buffer = new ByteArray(4);
    buffer.writeUint32(0x12345678);
    expect(buffer.readUint32()).toEqual(0x12345678);
  });

  it('should write and read strings', () => {
    let buffer = new ByteArray(10);
    buffer = buffer.writeString('hello');
    console.log("buffer", buffer);
    let result = buffer.readString(5);
    console.log("result", result);
    expect(result).toEqual('hello');
  });

  it('should write and read bytes', () => {
    let buffer1 = new ByteArray(6);
    buffer1.writeUint8(0x12);
    buffer1.writeUint16(0x3456);
    const buffer2 = new ByteArray(3);
    buffer2.writeUint8(0x78);
    buffer2.writeUint16(0x9abc);
    buffer1 = buffer1.writeBytes(buffer2);
    expect(buffer1.readUint8()).toEqual(0x12);
    expect(buffer1.readUint16()).toEqual(0x3456);
    expect(buffer1.readUint8()).toEqual(0x78);
    expect(buffer1.readUint16()).toEqual(0x9abc);
  });

  it('should encode and decode strings', () => {
    const str = 'hello world';
    const encoded = strencode(str);
    const decoded = strdecode(encoded);
    expect(decoded).toEqual(str);
  });

  it('should copy arrays', () => {
    const src = new ByteArray(5);
    src.writeUint8(0x12);
    src.writeUint16(0x3456);
    var dest = new ByteArray(5);
    dest = copyArray(dest, 0, src, 0, 5);
    expect(dest.readUint8()).toEqual(0x12);
    expect(dest.readUint16()).toEqual(0x3456);
  });

  it('should write null', () => {
    let buffer1 = new ByteArray(3);
    buffer1.writeUint8(0x12);
    buffer1.writeUint16(0x3456);
    buffer1 = buffer1.writeBytes(null);

    const buffer2 = new ByteArray(3);
    buffer2.writeUint8(0x12);
    buffer2.writeUint16(0x3456);

    expect(buffer1).toEqual(buffer2);
  });

  it('should writeBytes exceed', () => {
    let buffer1 = new ByteArray(3);
    buffer1.writeUint8(0x12);
    buffer1.writeUint16(0x3456);
    
    const buffer2 = new ByteArray(3);
    buffer2.writeUint8(0x12);
    buffer2.writeUint16(0x3456);
    
    buffer1 = buffer1.writeBytes(buffer2);
    // console.log("buffer1", buffer1);
    expect(buffer1.length).toEqual(6);
  });
});