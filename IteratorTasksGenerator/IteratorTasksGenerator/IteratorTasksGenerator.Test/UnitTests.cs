using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using IteratorTasksGenerator;

namespace IteratorTasksGenerator.Test
{
    [TestClass]
    public class UnitTest : ConventionCodeFixVerifier
    {
        [TestMethod]
        public void SimpleTask() => VerifyCSharpByConvention();

        [TestMethod]
        public void HasResultTask() => VerifyCSharpByConvention();

        [TestMethod]
        public void ConflictAll() => VerifyCSharpByConvention();

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new YieldReturnCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new YieldReturnAnalyzer();
        }
    }
}