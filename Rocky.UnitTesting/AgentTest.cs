using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rocky.UnitTesting
{
    [TestClass]
    public class AgentTest
    {
        [TestMethod]
        public void TestDriveChar()
        {
            var disk = from t in DriveInfo.GetDrives()
                       where t.DriveType == DriveType.Fixed || t.DriveType == DriveType.Removable
                       orderby t.Name ascending
                       select t;
            Assert.IsTrue(disk.Any());
            Assert.IsTrue(disk.Last().Name.Length > 0);
        }
    }
}