using System;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace Rocky.UnitTesting.Net
{
    [TestClass]
    public class HubTest
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
            var method = typeof(HubTest).GetMethod("add");
            var func = Hub.Lambda<Func<HubTest, int, int, int>>(method);
            int result = func(new HubTest(), 1, 1);
            Assert.AreEqual(2, result);
        }
        public int add(int a, int b)
        {
            return a + b;
        }
    }
}