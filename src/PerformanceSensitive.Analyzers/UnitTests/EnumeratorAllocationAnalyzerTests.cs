﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = PerformanceSensitive.Analyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    PerformanceSensitive.CSharp.Analyzers.EnumeratorAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace PerformanceSensitive.Analyzers.UnitTests
{
    public class EnumeratorAllocationAnalyzerTests
    {
        [Fact]
        public async Task EnumeratorAllocation_Basic()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System;
using System.Linq;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        int[] intData = new[] { 123, 32, 4 };
        IList<int> iListData = new[] { 123, 32, 4 };
        List<int> listData = new[] { 123, 32, 4 }.ToList();

        foreach (var i in intData)
        {
            Console.WriteLine(i);
        }

        foreach (var i in listData)
        {
            Console.WriteLine(i);
        }

        foreach (var i in iListData) // Allocations (line 19)
        {
            Console.WriteLine(i);
        }

        foreach (var i in (IEnumerable<int>)intData) // Allocations (line 24)
        {
            Console.WriteLine(i);
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(25,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
                VerifyCS.Diagnostic().WithLocation(25, 24),
                // Test0.cs(30,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
                VerifyCS.Diagnostic().WithLocation(30, 24));
        }

        [Fact]
        public async Task EnumeratorAllocation_Advanced()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        // These next 3 are from the YouTube video 
        foreach (object a in new[] { 1, 2, 3}) // Allocations 'new [] { 1. 2, 3}'
        {
            Console.WriteLine(a.ToString());
        }

        IEnumerable<string> fx1 = default(IEnumerable<string>);
        foreach (var f in fx1) // Allocations 'in'
        {
        }

        List<string> fx2 = default(List<string>);
        foreach (var f in fx2) // NO Allocations
        {
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(17,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
                VerifyCS.Diagnostic().WithLocation(17, 24));
        }

        [Fact]
        public async Task EnumeratorAllocation_Via_InvocationExpressionSyntax()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System.Collections;
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        var enumeratorRaw = GetIEnumerableRaw();
        while (enumeratorRaw.MoveNext())
        {
            Console.WriteLine(enumeratorRaw.Current.ToString());
        }

        var enumeratorRawViaIEnumerable = GetIEnumeratorViaIEnumerable();
        while (enumeratorRawViaIEnumerable.MoveNext())
        {
            Console.WriteLine(enumeratorRawViaIEnumerable.Current.ToString());
        }
    }

    private IEnumerator GetIEnumerableRaw()
    {
        return new[] { 123, 32, 4 }.GetEnumerator();
    }

    private IEnumerator<int> GetIEnumeratorViaIEnumerable()
    {
        int[] intData = new[] { 123, 32, 4 };
        return (IEnumerator<int>)intData.GetEnumerator();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(17,43): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
                VerifyCS.Diagnostic().WithLocation(17, 43));
        }

        [Fact]
        public async Task EnumeratorAllocation_IterateOverString_NoWarning()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        foreach (char c in ""foo"") { };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }
    }
}
