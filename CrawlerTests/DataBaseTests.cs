using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class SiteDataBaseTests
    {
        [TestMethod]
        public void ThereShouldBeZeroHostsByDefault()
        {
            var db = new SiteDataBase();

            Assert.AreEqual(db.GetHostCount(), 0);
        }

        [TestMethod]
        public void GetNonExistingHostShouldReturnNull()
        {
            var db = new SiteDataBase();

            Assert.IsNull(db.GetHostRecord("invalid"));            
        }

        [TestMethod]
        public void AddingNewHostShouldAddIt()
        {
            var db = new SiteDataBase();

            var values = new object[] { 0, "host1", true, true };
            db.AddHost((string)values[1], (bool)values[2], (bool)values[3]);
            Assert.AreEqual(db.GetHostCount(), 1);

            var expectedValues = db.GetHostRecord((string)values[1]);
            Assert.IsTrue(values.SequenceEqual(expectedValues));
        }

        [TestMethod]
        public void AddingExistingHostShouldUpdateIt()
        {
            var db = new SiteDataBase();

            db.AddHost("host1", false, false);

            var values = new object[] { 0, "host1", true, true };
            db.AddHost((string)values[1], (bool)values[2], (bool)values[3]);
            Assert.AreEqual(db.GetHostCount(), 1);

            var expectedValues = db.GetHostRecord((string)values[1]);
            Assert.IsTrue(values.SequenceEqual(expectedValues));
        }

        /*public void AddingParentHostShouldAddIt()
        {
            Action action = () =>
            {
                var db = new SiteDataBase();

                db.AddParent("noparent", true, true);
            };
            

            Assert.ThrowsException<ArgumentException>(action);
        }*/
    }
}
