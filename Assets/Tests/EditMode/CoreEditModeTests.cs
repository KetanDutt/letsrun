using System.Collections;
using NUnit.Framework;

public sealed class CoreEditModeTests
{
    public static IEnumerable Contracts()
    {
        foreach (CoreContractCase contract in CoreContractCases.Create(UnityProfileCodec.Serialize, UnityProfileCodec.Deserialize))
            yield return new TestCaseData(contract).SetName(contract.Name);
    }

    [TestCaseSource(nameof(Contracts))]
    public void CoreContract(CoreContractCase contract) { contract.Test(); }
}
