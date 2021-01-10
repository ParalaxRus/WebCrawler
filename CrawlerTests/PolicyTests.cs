using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebCrawler
{
    [TestClass]
    public class PolicyTests
    {
        [TestMethod]
        public void DefaultConstructorShouldCreateEmptyPolicy()
        {
            var policy = new Policy();

            Assert.IsTrue(policy.IsEmpty);
        }

        [TestMethod]
        public void NotMentionedInPolicyLocalPathShouldBeAllowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/about");

            Assert.IsTrue(policy.IsAllowed("/search"));
        }

        [TestMethod]
        public void LocalPathWithDissalowMatchShoulBeDisallowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/about");
            policy.AddDisallowed("/search");

            Assert.IsFalse(policy.IsAllowed("/search"));
        }

        [TestMethod]
        public void LocalPathWithAllowAndDissalowMatchOfTheSameLengthShouldBeDissallowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/home/s");
            policy.AddAllowed("*search");

            Assert.IsFalse(policy.IsAllowed("/home/search/shirts"));
        }

        [TestMethod]
        public void LocalPathWithAllowAndDissalowMatchButAllowIsLongerShouldBeAllowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/home");
            policy.AddAllowed("/home/about");

            Assert.IsTrue(policy.IsAllowed("/home/about/index.html"));
        }

        [TestMethod]
        public void LocalPathWithAllowAndDissalowMatchButDisallowIsLongerShouldBeDisallowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/home/about");
            policy.AddAllowed("/home");

            Assert.IsFalse(policy.IsAllowed("/home/about/index.html"));
        }

        [TestMethod]
        public void LocalPathMatchingDisallowWithWildcardShouldBeDisallowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/home/*/about");

            Assert.IsFalse(policy.IsAllowed("/home/test1/test2/about"));
        }

        [TestMethod]
        public void LocalPathMatchingDisallowWithSingleCharacterShouldBeDisallowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("/h");

            Assert.IsFalse(policy.IsAllowed("/home/test1/test2/about"));
        }

        [TestMethod]
        public void LocalPathMatchingDisallowExtensionShouldBeDisallowed()
        {
            var policy = new Policy(new Uri("https://www.example.com"), "*");
            policy.AddDisallowed("*.pdf$");

            Assert.IsFalse(policy.IsAllowed("/home/test1/test2/about.pdf"));
        }
    }
}