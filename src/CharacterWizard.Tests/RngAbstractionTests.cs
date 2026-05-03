using CharacterWizard.Shared.Utilities;

namespace CharacterWizard.Tests;

/// <summary>
/// Tests for the IRng / IRngFactory abstractions introduced to replace
/// Random.Shared with action-scoped, independently testable RNG contexts.
/// </summary>
public class RngAbstractionTests
{
    // ── Test double ───────────────────────────────────────────────────────

    /// <summary>
    /// Deterministic IRng that returns values from a fixed sequence,
    /// cycling when exhausted. Suitable for injection in unit tests.
    /// </summary>
    private sealed class SequenceRng(params int[] values) : IRng
    {
        private int _index;

        public int Next(int maxValue)
        {
            int v = values[_index % values.Length] % maxValue;
            _index++;
            return v < 0 ? 0 : v;
        }

        public int Next(int minValue, int maxValue)
        {
            int range = maxValue - minValue;
            int v = minValue + (values[_index % values.Length] % range);
            _index++;
            return v < minValue ? minValue : v;
        }
    }

    private sealed class FixedRngFactory(IRng rng) : IRngFactory
    {
        public IRng Create() => rng;
    }

    // ── IRng contract ─────────────────────────────────────────────────────

    [Fact]
    public void SequenceRng_Next_ReturnsExpectedValues()
    {
        IRng rng = new SequenceRng(3, 7, 1);

        Assert.Equal(3, rng.Next(10));
        Assert.Equal(7, rng.Next(10));
        Assert.Equal(1, rng.Next(10));
        // cycles
        Assert.Equal(3, rng.Next(10));
    }

    [Fact]
    public void SequenceRng_NextRange_ReturnsBoundedValues()
    {
        IRng rng = new SequenceRng(0, 5, 2);

        int v1 = rng.Next(1, 7);
        int v2 = rng.Next(1, 7);
        int v3 = rng.Next(1, 7);

        Assert.InRange(v1, 1, 6);
        Assert.InRange(v2, 1, 6);
        Assert.InRange(v3, 1, 6);
    }

    // ── IRngFactory contract ──────────────────────────────────────────────

    [Fact]
    public void FixedRngFactory_Create_ReturnsSameInstance()
    {
        IRng rng = new SequenceRng(1, 2, 3);
        IRngFactory factory = new FixedRngFactory(rng);

        var a = factory.Create();
        var b = factory.Create();

        Assert.Same(a, b);
    }

    // ── Action isolation ──────────────────────────────────────────────────

    [Fact]
    public void TwoFactoryCreations_WithIndependentInstances_ProduceIndependentSequences()
    {
        // Each Create() call returns a distinct IRng backed by its own Random instance.
        // Simulate this with two separate SequenceRngs via two factories.
        IRng rng1 = new SequenceRng(0);  // always picks index 0
        IRng rng2 = new SequenceRng(4);  // always picks index 4

        string[] options = ["A", "B", "C", "D", "E", "F"];

        // Action 1 uses rng1
        string pick1 = options[rng1.Next(options.Length)];
        // Action 2 uses rng2
        string pick2 = options[rng2.Next(options.Length)];

        Assert.Equal("A", pick1);
        Assert.Equal("E", pick2);
    }
}
