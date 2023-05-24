using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Flee.PublicTypes;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Linq.Dynamic.Core;
using TestRoslynCompilation;

var summary = BenchmarkRunner.Run<ExpressionEvaluatorsBenchmark>();

[MemoryDiagnoser]
public class ExpressionEvaluatorsBenchmark
{

    const int w = 768, h = 1024;
    const double dpi = 260.0;
    const string csharpCode = "w >= 768 && h <= 1024 && dpi > 260";

    [GlobalSetup]
    public void Setup() {
        // setup FLEE
        fleeContext = new ExpressionContext();
        fleeContext.Variables["w"] = w;
        fleeContext.Variables["h"] = h;
        fleeContext.Variables["dpi"] = dpi;
        fleeExpr = fleeContext.CompileGeneric<bool>("w >= 768 AND h <= 1024 AND Dpi > 260");
        // setup EXPRESSIVE
        DataDict = new Dictionary<string, object>() {
            ["w"] = w,
            ["h"] = h,
            ["dpi"] = dpi,
        };
        expressiveExpr = new ("[w] >= 768 && [h] <= 1024 && [dpi] > 260");
        // setup NCALC
        ncalcExpr = new("[w] >= 768 and [h] <= 1024 and [dpi] > 260");
        ncalcExpr.Parameters["w"] = w;
        ncalcExpr.Parameters["h"] = h;
        ncalcExpr.Parameters["dpi"] = dpi;
        // setup CSharpScript 
        csharpScript = CSharpScript.Create<bool>(csharpCode, globalsType: typeof(ExpressionParserData));
        csharpScript.Compile();
        // ... delegate
        csharpRunner = csharpScript.CreateDelegate();
        // ... dynamic, interface and evaluator
        ParserData = new ExpressionParserData {
            w = 768,
            h = 1024,
            dpi = 260.50,
        };
        var assembly = Compiler.Compile(csharpCode, typeof(EvaluatorData));
        checker = Compiler.GetChecker(assembly);
        dynaChecker = checker;
        // ... "evaluator" uses a global static class to hold the expression variables
        EvaluatorData.w = w;
        EvaluatorData.h = h;
        EvaluatorData.dpi = dpi;
        evaluator = Compiler.GetEvaluator(assembly);
        // setup Dynamic Linq Parsed Lambda Expression
        var dynExpr = DynamicExpressionParser.ParseLambda<ExpressionParserData, bool>(new ParsingConfig(), true, csharpCode);
        func = dynExpr.Compile();
    }
    Dictionary<string, object> DataDict;
    ExpressionParserData ParserData;
    ExpressionContext fleeContext;
    IGenericExpression<bool> fleeExpr;
    Expressive.Expression expressiveExpr;
    NCalc.Expression ncalcExpr;
    Script<bool> csharpScript;
    ScriptRunner<bool> csharpRunner;
    IChecker checker;
    dynamic dynaChecker;
    IEvaluator evaluator;
    Func<ExpressionParserData, bool> func;

    // this class is used to simulate the same "dynamic scenario" but statically compiled
    public class CSharpMethodCall
    {
        public bool Check(int w, int h, double dpi) => w >= 768 && h <= 1024 && dpi > 260;
    }
    readonly CSharpMethodCall caller = new();
    [Benchmark]
    public bool DirectCSharpSameValues() => caller.Check(w, h, dpi);

    [Benchmark]
    public bool DynamicLinqParseLambdaSameValues() => func(ParserData);

    [Benchmark]
    public bool RoslynCompiledInterfaceSameValues() => checker.Check(w, h, dpi);

    [Benchmark]
    public bool RoslynCompiledDynamicCallSameValues() => dynaChecker.Check(w, h, dpi);

    [Benchmark]
    public bool RoslynCompiledEvaluatorSameValues() => evaluator.Evaluate();

    [Benchmark]
    public Task<ScriptState<bool>> CSharpScriptCompiledSameValues() => csharpScript.RunAsync(ParserData);

    [Benchmark]
    public Task<bool> CSharpScriptDelegateSameValues() => csharpRunner(ParserData);

    [Benchmark]
    public bool FleeSameValues() => fleeExpr.Evaluate();

    [Benchmark]
    public bool ExpressiveSameValues() => expressiveExpr.Evaluate<bool>(DataDict);

    [Benchmark]
    public object NCalcSameValues() => ncalcExpr.Evaluate();  

    // this class is used to hold data for methods that accept an object instance as its parameters
    public class ExpressionParserData {
        public int w { get; set; } 
        public int h { get; set; }  
        public double dpi { get; set; }
    }
}