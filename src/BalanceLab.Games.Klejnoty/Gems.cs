using System.Globalization;
using System.Text;

namespace BalanceLab.Games.Klejnoty;

/// <summary>Piec kolorow klejnotow i zloto, ktore zastepuje dowolny kolor.</summary>
public enum Gem
{
    White,
    Blue,
    Green,
    Red,
    Black,
    Gold,
}

/// <summary>
/// Liczba zetonow kazdego rodzaju — po bajcie na rodzaj w jednej liczbie 64-bitowej. Dodawanie
/// i odejmowanie calych zestawow to wtedy jedna operacja, a rownosc i hash sa za darmo.
/// </summary>
/// <remarks>Kazda liczba miesci sie w 0..255; w grze zadna nie przekracza kilkudziesieciu.</remarks>
public readonly record struct Gems
{
    /// <summary>Kolory bez zlota.</summary>
    public const int Colors = 5;

    private Gems(ulong packed) => Packed = packed;

    internal ulong Packed { get; }

    public static Gems None => default;

    public int this[Gem gem] => (int)((Packed >> Shift(gem)) & 0xFF);

    public int Total
    {
        get
        {
            var total = 0;
            for (var gem = Gem.White; gem <= Gem.Gold; gem++)
            {
                total += this[gem];
            }

            return total;
        }
    }

    public static Gems Of(int white = 0, int blue = 0, int green = 0, int red = 0, int black = 0, int gold = 0) =>
        None.With(Gem.White, white).With(Gem.Blue, blue).With(Gem.Green, green)
            .With(Gem.Red, red).With(Gem.Black, black).With(Gem.Gold, gold);

    public static Gems One(Gem gem) => None.With(gem, 1);

    /// <summary>Zestaw z wartosci kolorow po kolei (bialy, niebieski, zielony, czerwony, czarny).</summary>
    public static Gems FromColors(ReadOnlySpan<int> counts)
    {
        var gems = None;
        for (var i = 0; i < counts.Length; i++)
        {
            gems = gems.With((Gem)i, counts[i]);
        }

        return gems;
    }

    public Gems With(Gem gem, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 255);
        return new Gems((Packed & ~(0xFFUL << Shift(gem))) | ((ulong)count << Shift(gem)));
    }

    /// <summary>Czy w kazdym rodzaju jest co najmniej tyle, co w <paramref name="other"/>.</summary>
    public bool Covers(Gems other)
    {
        for (var gem = Gem.White; gem <= Gem.Gold; gem++)
        {
            if (this[gem] < other[gem])
            {
                return false;
            }
        }

        return true;
    }

    // Bajty sie nie przenosza, dopoki zadna liczba nie wyjdzie poza 0..255 — pilnuje tego wolajacy.
    public static Gems operator +(Gems a, Gems b) => new(a.Packed + b.Packed);

    public static Gems operator -(Gems a, Gems b) =>
        a.Covers(b) ? new(a.Packed - b.Packed) : throw new InvalidOperationException($"Nie mozna odjac {b} od {a}.");

    public static Gems Add(Gems a, Gems b) => a + b;

    public static Gems Subtract(Gems a, Gems b) => a - b;

    /// <summary>Na przyklad "W1 U2 Z1" (W bialy, U niebieski, G zielony, R czerwony, K czarny, Z zloto); pusty zestaw to "-".</summary>
    public override string ToString()
    {
        var text = new StringBuilder();
        for (var gem = Gem.White; gem <= Gem.Gold; gem++)
        {
            if (this[gem] > 0)
            {
                text.Append(text.Length > 0 ? " " : "").Append("WUGRKZ"[(int)gem]).Append(this[gem].ToString(CultureInfo.InvariantCulture));
            }
        }

        return text.Length > 0 ? text.ToString() : "-";
    }

    private static int Shift(Gem gem) => (int)gem * 8;
}
