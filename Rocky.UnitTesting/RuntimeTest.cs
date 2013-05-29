using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rocky.Data;

namespace Rocky.UnitTesting.Net
{
    [TestClass]
    public class RuntimeTest
    {
        [TestMethod]
        public void Test0x()
        {
            string path = @"E:\ThirdParty\AzureApp\Xine\Sports\Sports.Repository\Resource\template.xlsx";
            var dt = DbUtility.ReadExcel(path, DbUtility.ExcelVersion.Excel2007);


            IPAddress addr = new IPAddress(new byte[] { 192, 1, 1, 1 });
            IPAddress addr2 = new IPAddress(new byte[] { 192, 1, 1, 1 });

            IPEndPoint a = new IPEndPoint(addr, 22);
            IPEndPoint a2 = new IPEndPoint(addr2, 22);
            Assert.AreEqual(addr, addr2);
            Assert.AreEqual(a, a2);
        }

        [TestMethod]
        public void TestLambda()
        {
            var method = typeof(RuntimeTest).GetMethod("add");
            var func = Runtime.Lambda<Func<RuntimeTest, int, int, int>>(method);
            int result = func(new RuntimeTest(), 1, 1);
            Assert.AreEqual(2, result);
        }
        public int add(int a, int b)
        {
            return a + b;
        }
    }
}