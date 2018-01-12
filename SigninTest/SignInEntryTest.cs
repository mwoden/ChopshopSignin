using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SigninTest
{
    [TestClass]
    class SignInEntryTest
    {
        static readonly DateTime entryTime = new DateTime(2017, 12, 1, 18, 0, 0);
        static readonly DateTime exitTime = new DateTime(2017, 12, 1, 21, 0, 0);

        [TestMethod]
        public void CreateCompleteSignInEntryTest()
        {
            var signIn = new SignInLibrary.SignInEntry(entryTime, exitTime);

            Assert.AreEqual(signIn.IsOut, true);
            Assert.AreEqual(signIn.IsIn, false);

            Assert.AreEqual(false, true);
        }
    }
}
