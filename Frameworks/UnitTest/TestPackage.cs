using System.Linq;
using System.Text;
using GoPlay.Core;
using NUnit.Framework;
using GoPlay.Core.Protocols;

namespace UnitTest
{
    public class TestPackage
    {
        [Test]
        public void TestSplit()
        {
            var str = new StringBuilder();
            for (int i = 0; i < Consts.Package.MAX_CHUNK_SIZE + 100; i++)
            {
                str.Append((byte)(i % byte.MaxValue));
            }
            var pack = Package.Create(1, new PbString
            {
                Value = str.ToString()
            }, PackageType.Request, EncodingType.Protobuf);
            pack.Header.ClientId = 10;

            var arr = pack.Split().ToArray();
            Assert.AreEqual(3, arr.Length);
            
            Assert.AreEqual(pack.Header.ClientId, arr[0].Header.ClientId);
            Assert.AreEqual(Consts.Package.MAX_CHUNK_SIZE, arr[0].RawData.Length);
            Assert.AreEqual(Consts.Package.MAX_CHUNK_SIZE, arr[0].Header.PackageInfo.ContentSize);
            Assert.AreEqual(0, arr[0].Header.PackageInfo.ChunkIndex);
            Assert.AreEqual(3, arr[0].Header.PackageInfo.ChunkCount);
            
            Assert.AreEqual(pack.Header.ClientId, arr[1].Header.ClientId);
            Assert.AreEqual(Consts.Package.MAX_CHUNK_SIZE, arr[1].RawData.Length);
            Assert.AreEqual(Consts.Package.MAX_CHUNK_SIZE, arr[1].Header.PackageInfo.ContentSize);
            Assert.AreEqual(1, arr[1].Header.PackageInfo.ChunkIndex);
            Assert.AreEqual(3, arr[1].Header.PackageInfo.ChunkCount);
            
            Assert.AreEqual(pack.Header.ClientId, arr[2].Header.ClientId);
            Assert.AreEqual(pack.RawData.Length % Consts.Package.MAX_CHUNK_SIZE, arr[2].RawData.Length);
            Assert.AreEqual(pack.RawData.Length % Consts.Package.MAX_CHUNK_SIZE, arr[2].Header.PackageInfo.ContentSize);
            Assert.AreEqual(2, arr[2].Header.PackageInfo.ChunkIndex);
            Assert.AreEqual(3, arr[2].Header.PackageInfo.ChunkCount);
            
            var pack2 = Package.Join(arr);
            Assert.AreEqual(pack.RawData.Length, pack2.RawData.Length);
            Assert.AreEqual(pack.RawData, pack2.RawData);
            Assert.AreEqual(pack.Header.ClientId, pack2.Header.ClientId);
        }
    }
}