using LightTable.Core;
using Xunit;

namespace LightTable.Tests;
public sealed class OpticalRetryBudgetTests
{
    [Fact] public void ExhaustedInputStopsAndANewInputRestoresTheBudget()
    {
        var budget=new OpticalRetryBudget();var revision=budget.Revision;Assert.NotNull(budget.NextDelay());Assert.Equal(50,budget.NextDelay()!.Value.TotalMilliseconds);Assert.Equal(150,budget.NextDelay()!.Value.TotalMilliseconds);Assert.Null(budget.NextDelay());Assert.Null(budget.NextDelay());
        budget.BeginInput();Assert.NotEqual(revision,budget.Revision);Assert.NotNull(budget.NextDelay());budget.Succeeded();Assert.NotNull(budget.NextDelay());
    }
}
