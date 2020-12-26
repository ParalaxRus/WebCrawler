using System;
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
            var db = new DataBase();

            Assert.AreEqual(db.GetHostCount(), 0);
        }

        [TestMethod]
        public void GetNonExistingHostShouldReturnNull()
        {
            var db = new DataBase();

            Assert.IsNull(db.GetHostRecord("invalid"));            
        }

        [TestMethod]
        public void AddingNewHostShouldAddIt()
        {
            var db = new DataBase();

            var values = new object[] { 0, "host1", true, true };
            db.AddHost((string)values[1], (bool)values[2], (bool)values[3]);
            Assert.AreEqual(db.GetHostCount(), 1);

            var expectedValues = db.GetHostRecord((string)values[1]);
            Assert.IsTrue(values.SequenceEqual(expectedValues));
        }

        [TestMethod]
        public void AddingExistingHostShouldUpdateIt()
        {
            var db = new DataBase();

            db.AddHost("host1", false, false);

            var values = new object[] { 0, "host1", true, true };
            db.AddHost((string)values[1], (bool)values[2], (bool)values[3]);
            Assert.AreEqual(db.GetHostCount(), 1);

            var expectedValues = db.GetHostRecord((string)values[1]);
            Assert.IsTrue(values.SequenceEqual(expectedValues));
        }

        [TestMethod]
        public void AddingConnectionWithNonExistingParentShouldThrowArgumentException()
        {
            var db = new DataBase();

            db.AddHost("host1", false, false);
            
            Action action = () =>
            {
                db.AddConnection("invalid", "host1");
            };
            

            Assert.ThrowsException<ArgumentException>(action);
        }

        [TestMethod]
        public void AddingConnectionWithNonExistingChildShouldThrowArgumentException()
        {
            var db = new DataBase();

            db.AddHost("host1", false, false);
            
            Action action = () =>
            {
                db.AddConnection("host1", "invalid");
            };
            
            Assert.ThrowsException<ArgumentException>(action);
        }

        [TestMethod]
        public void GetNonExistingParentShouldThrowArgumentException()
        {
            var db = new DataBase();

            Action action = () =>
            {
                db.GetChildren("invalid");
            };
            
            Assert.ThrowsException<ArgumentException>(action);
        }

        [TestMethod]
        public void AddingValidConnectionShouldAddIt()
        {
            var db = new DataBase();

            db.AddHost("parent", false, false);
            db.AddHost("child", false, false);

            db.AddConnection("parent", "child");
            
            var children = db.GetChildren("parent");
            Assert.AreEqual(children.Count, 1);
            Assert.AreEqual(children[0], "child");
        }

        [TestMethod]
        public void AddingExistingConnectionShouldUpdateIt()
        {
            var db = new DataBase();

            db.AddHost("parent", false, false);
            db.AddHost("child1", false, false);

            db.AddConnection("parent", "child1");
            
            // Nothing actually gets modified since connections 
            // table has only two columns for now
            var children = db.GetChildren("parent");
            Assert.AreEqual(children.Count, 1);
            Assert.AreEqual(children[0], "child1");
        }

        [TestMethod]
        public void GetChildrenForParentWithMultipleChildrenShouldReturnAllChildren()
        {
            var db = new DataBase();

            var children = new string[3]
            {
                "child1",
                "child2",
                "child3"
            };
            db.AddHost("parent", false, false);
            foreach (var child in children)
            {
                db.AddHost(child, false, false);
                db.AddConnection("parent", child);
            }
            
            // Nothing actually gets modified since connections 
            // table has only two columns for now
            var actualChildren = db.GetChildren("parent");
            Assert.IsTrue(children.SequenceEqual(actualChildren));
        }
    }
}
