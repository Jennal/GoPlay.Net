using NUnit.Framework;
using UnitTest.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;

namespace UnitTest
{
    public class TestExpression
    {
        [Test]
        public void TestCallMethod()
        {
            var testProcessor = new TestProcessor
            {
                Prefix = "TestCallMethod"
            };
            var method = typeof(TestProcessor).GetMethod("Echo");

            var d = ExpressionUtil.CreateMethod(testProcessor, method);
            var result = (PbString)d(new Header(), new PbString
            {
                Value = "Hello"
            });
            
            Assert.AreEqual("[TestCallMethod] Server reply: Hello", result.Value);
            
            result = (PbString)d(new Header(), new PbString
            {
                Value = "Hi"
            });
            
            Assert.AreEqual("[TestCallMethod] Server reply: Hi", result.Value);
        }

        [Test]
        public void TestMethodInfo()
        {
            var testProcessor = new TestProcessor
            {
                Prefix = "TestCallMethod"
            };
            var method = typeof(TestProcessor).GetMethod("Echo");
            var result = (PbString)method.Invoke(testProcessor, new object[]
            {
                new Header(), new PbString
                {
                    Value = "Hello"
                }
            });
            Assert.AreEqual("[TestCallMethod] Server reply: Hello", result.Value);
        }
    }
}