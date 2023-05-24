using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Text;

namespace TestRoslynCompilation
{

    public interface IChecker
    {
        bool Check(int w, int h, double dpi);
    }

    public interface IEvaluator
    {
        bool Evaluate();
    }

    public static class Compiler
    {
        const string template = """
        using System;
        using TestRoslynCompilation;
        using static #globals#;
        
        namespace Compiled {

        public class Checker : IChecker {
            public bool Check(int w, int h, double dpi) => #expr#;
        }

        public class Evaluator : IEvaluator {
            public bool Evaluate() => #expr#;
        }

        }
        """;

        public static IChecker GetChecker(Assembly assembly) {
            if (assembly.CreateInstance("Compiled.Checker") is not IChecker checker)
                throw new Exception("Could not create checker!");
            return checker;
        }
        public static IChecker GetChecker(string expression, Type globalType) {
            var assembly = Compile(expression, globalType);
            return GetChecker(assembly);
        }

        public static IEvaluator GetEvaluator(Assembly assembly) {
            if (assembly.CreateInstance("Compiled.Evaluator") is not IEvaluator evaluator)
                throw new Exception("Could not create evaluator!");
            return evaluator;
        }
        public static IEvaluator GetEvaluator(string expression, Type globalType) {
            var assembly = Compile(expression, globalType);
            return GetEvaluator(assembly);
        }

        public static Assembly Compile(string expression, Type globalType) {
            var code = template.Replace("#expr#", expression).Replace("#globals#", $"{globalType.Namespace}.{globalType.Name}");
            var tree = SyntaxFactory.ParseSyntaxTree(code.Trim());
            var selfRef = MetadataReference.CreateFromFile(typeof(IChecker).Assembly.Location);
            var refs = new List<MetadataReference> { selfRef };
            if (globalType.Assembly != typeof(IChecker).Assembly)
                refs.Add(MetadataReference.CreateFromFile(globalType.Assembly.Location));
            var compilation = CSharpCompilation.Create("Checker.cs")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release))
                .WithReferences(Basic.Reference.Assemblies.Net70.References.All)
                .AddReferences(refs)
                .AddSyntaxTrees(tree);
            using var codeStream = new MemoryStream();
            EmitResult compilationResult = compilation.Emit(codeStream);
            if (!compilationResult.Success) {
                var sb = new StringBuilder();
                foreach (var diag in compilationResult.Diagnostics)
                    sb.AppendLine(diag.ToString());
                throw new Exception(sb.ToString());
            }
            return Assembly.Load(codeStream.ToArray());
        }
    }

    public static class EvaluatorData
    {
        public static int w { get; set; }
        public static int h { get; set; }
        public static double dpi { get; set; }

    }

}
