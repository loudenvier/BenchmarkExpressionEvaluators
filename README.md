# Benchmark Expression Evaluators

Evaluating C# expressions from strings/text has always been a challenge. Many official technologies and third-party libraries emerged that allowed us to acomplish just that with varying degrees of success:

* Microsoft's "ugly" and outdated `CodeDom`
* Microsoft's incredibly fast `DynamicMethod` along with all the complexity of `IL.Emmit`
* Microsoft's elegant (and performant) `ExpressionTree`'s 
* "Hacks" that use `DataTable.Compute`, `DataColumn` `"Eval"`, SQL etc.
* Microsoft's amazing (and overpowered) "compiler as a service" Roslyn
* ... And it's scripting offsprings
* Good'n'old `Dynamic Linq` (now ported to .NET Core)
* And third-party expression evaluators such as:
  * NCalc
  * Flee
  * Expressive

This repository aims to benchmark current sensible choices of performant and easy to use expression evaluators to help me (and hopefully us) find which one is the best.

