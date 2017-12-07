using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace SigninTest
{
    [TestClass]
    public class PersonTest
    {
        [TestMethod]
        public void CreatePersonFromScanDataTest()
        {
            var person = SignInLibrary.Person.Create(null);
            Assert.IsNull(person);

            person = SignInLibrary.Person.Create(string.Empty);
            Assert.IsNull(person);

            person = SignInLibrary.Person.Create("Last,First");
            Assert.IsNotNull(person);
            Assert.AreEqual(person.FullName, "Last, First");
            Assert.AreEqual(person.Role, SignInLibrary.Person.RoleType.Student);

            person = SignInLibrary.Person.Create("mentor Last,First");
            Assert.IsNotNull(person);
            Assert.AreEqual(person.FullName, "Last, First");
            Assert.AreEqual(person.Role, SignInLibrary.Person.RoleType.Mentor);
        }

        [TestMethod]
        public void CreatePersonWithNoScanFromXmlTest()
        {
            var xml = new XElement("Person",
                        new XAttribute("firstName", "First"),
                        new XAttribute("lastName", "Last"),
                        new XAttribute("role", "Student"));

            var person = new SignInLibrary.Person(xml);
            Assert.IsNotNull(person);
            Assert.AreEqual(person.FullName, "Last, First");
            Assert.AreEqual(person.Role, SignInLibrary.Person.RoleType.Student);
        }

        //[TestMethod]
        //public void CreatePersonWithScansFromXmlTest()
        //{
        //var xml = new XElement("Person",
        //            new XAttribute("firstName", "First"),
        //            new XAttribute("lastName", "Last"),
        //            new XAttribute("role", "Student"),
        //            new XElement("Scans",
        //                new XAttribute
        //            );

        //var person = new SignInLibrary.Person(xml);
        //Assert.IsNotNull(person);
        //Assert.AreEqual(person.FullName, "Last, First");
        //Assert.AreEqual(person.Role, SignInLibrary.Person.RoleType.Student);
        //}
    }
}
