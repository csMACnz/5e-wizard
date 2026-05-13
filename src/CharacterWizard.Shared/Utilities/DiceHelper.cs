namespace CharacterWizard.Shared.Utilities;

/// <summary>
/// Stateless helpers for common dice-rule calculations.
/// </summary>
public static class DiceHelper
{
    /// <summary>
    /// Rolls 4d6 and drops the lowest die.
    /// </summary>
    public static int RollAbilityScore(IRng rng)
    {
        int r1 = rng.Next(1, 7);
        int r2 = rng.Next(1, 7);
        int r3 = rng.Next(1, 7);
        int r4 = rng.Next(1, 7);
        return r1 + r2 + r3 + r4 - Math.Min(Math.Min(r1, r2), Math.Min(r3, r4));
    }

    /// <summary>
    /// Rolls a dice expression in the form <c>XdY</c> or <c>XdY * M</c>.
    /// Returns false when expression cannot be parsed.
    /// </summary>
    public static bool TryRollExpression(IRng rng, string expression, out int result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        int multiplier = 1;
        var parts = expression.Split('*');
        var dicePart = parts[0].Trim();
        if (parts.Length == 2)
        {
            if (!int.TryParse(parts[1].Trim(), out int m) || m < 0)
                return false;
            multiplier = m;
        }
        else if (parts.Length > 2)
        {
            return false;
        }

        var diceParts = dicePart.Split('d');
        if (diceParts.Length != 2)
            return false;
        if (!int.TryParse(diceParts[0].Trim(), out int count) || count < 1)
            return false;
        if (!int.TryParse(diceParts[1].Trim(), out int sides) || sides < 1)
            return false;

        int total = 0;
        for (int i = 0; i < count; i++)
            total += rng.Next(1, sides + 1);

        result = total * multiplier;
        return true;
    }
}
