import {describe, expect, it, beforeEach, jest} from '@jest/globals';
import Package from '../src/Package';
import { GoPlay } from '../src/pkg.pb';
import { ByteArray } from '../src/ByteArray';

describe('Package', () => {
  it('should encode and decode a package correctly', () => {
    // Create a test package
    const header = new GoPlay.Core.Protocols.Header();
    header.PackageInfo = new GoPlay.Core.Protocols.PackageInfo();
    header.PackageInfo.Route = 1;
    header.PackageInfo.EncodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;
    header.PackageInfo.Type = GoPlay.Core.Protocols.PackageType.Request;
    header.PackageInfo.Id = 123;
    const data = new GoPlay.Core.Protocols.ReqHankShake();
    data.AppKey = 'app-key';
    data.ClientVersion = '1.0.0';
    data.ServerTag = GoPlay.Core.Protocols.ServerTag.FrontEnd;
    const packageObj = new Package(header, data, null);

    // Encode the package
    const encodedBytes = packageObj.encode();

    // Decode the package
    const decodedPackage = Package.decode(GoPlay.Core.Protocols.ReqHankShake, encodedBytes);

    // Verify that the decoded package matches the original package
    expect(decodedPackage.header.PackageInfo.Route).toBe(header.PackageInfo.Route);
    expect(decodedPackage.header.PackageInfo.EncodingType).toBe(header.PackageInfo.EncodingType);
    expect(decodedPackage.header.PackageInfo.Type).toBe(header.PackageInfo.Type);
    expect(decodedPackage.header.PackageInfo.Id).toBe(header.PackageInfo.Id);
    expect(decodedPackage.data).toEqual(data);
  });

  it('should decode a raw package correctly', () => {
    // Create a test package
    const header = new GoPlay.Core.Protocols.Header();
    header.PackageInfo = new GoPlay.Core.Protocols.PackageInfo();
    header.PackageInfo.Route = 1;
    header.PackageInfo.EncodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;
    header.PackageInfo.Type = GoPlay.Core.Protocols.PackageType.Request;
    header.PackageInfo.Id = 123;
    const data = new GoPlay.Core.Protocols.ReqHankShake();
    data.AppKey = 'app-key';
    data.ClientVersion = '1.0.0';
    data.ServerTag = GoPlay.Core.Protocols.ServerTag.FrontEnd;
    const packageObj = new Package(header, data, null);

    // Encode the package
    const encodedBytes = packageObj.encode();

    // Decode the raw package
    const rawPackage = Package.tryDecodeRaw(encodedBytes);
    const rawEncodedBytes = rawPackage.encode();

    // Verify that the decoded package matches the original package
    expect(rawPackage.header.PackageInfo.Route).toBe(header.PackageInfo.Route);
    expect(rawPackage.header.PackageInfo.EncodingType).toBe(header.PackageInfo.EncodingType);
    expect(rawPackage.header.PackageInfo.Type).toBe(header.PackageInfo.Type);
    expect(rawPackage.header.PackageInfo.Id).toBe(header.PackageInfo.Id);
    expect(rawEncodedBytes.buffer).toEqual(encodedBytes.buffer);

    const decodedPackage = rawPackage.decodeFromRaw(GoPlay.Core.Protocols.ReqHankShake);
    expect(decodedPackage.data).toEqual(data);
  });

  it('should create a package from data correctly', () => {
    // Create a test package from data
    const route = 1;
    const data = { message: 'Hello, world!' };
    const type = GoPlay.Core.Protocols.PackageType.Request;
    const encoding = GoPlay.Core.Protocols.EncodingType.Protobuf;
    const packageObj = Package.createFromData(route, data, type, encoding);

    // Verify that the package was created correctly
    expect(packageObj.header.PackageInfo.Route).toBe(route);
    expect(packageObj.header.PackageInfo.EncodingType).toBe(encoding);
    expect(packageObj.header.PackageInfo.Type).toBe(type);
    expect(packageObj.data).toEqual(data);
  });

  it('should create a raw package correctly', () => {
    // Create a test raw package
    const header = new GoPlay.Core.Protocols.Header();
    header.PackageInfo = new GoPlay.Core.Protocols.PackageInfo();
    header.PackageInfo.Route = 1;
    header.PackageInfo.EncodingType = GoPlay.Core.Protocols.EncodingType.Protobuf;
    header.PackageInfo.Type = GoPlay.Core.Protocols.PackageType.Request;
    header.PackageInfo.Id = 123;
    const rawData = new ByteArray();
    rawData.writeUint8(1);
    rawData.writeUint16(2);
    rawData.writeUint32(3);
    const packageObj = Package.createRaw(header, rawData);

    // Verify that the raw package was created correctly
    expect(packageObj.header.PackageInfo.Route).toBe(header.PackageInfo.Route);
    expect(packageObj.header.PackageInfo.EncodingType).toBe(header.PackageInfo.EncodingType);
    expect(packageObj.header.PackageInfo.Type).toBe(header.PackageInfo.Type);
    expect(packageObj.header.PackageInfo.Id).toBe(header.PackageInfo.Id);
    expect(packageObj.rawData).toEqual(rawData);
  });
});