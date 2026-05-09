using System.Linq;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Wires;
using Content.Shared._Horizon._Fractions.AnCo.WeaponCaseHack;
using Content.Shared.Lock;
using Content.Shared.Popups;
using Content.Shared.Wires;
using Robust.Shared.Log;

namespace Content.Server._Horizon._Fractions.AnCo.WeaponCaseHack;

public sealed partial class AnCoWeaponCaseHackWireAction : ComponentWireAction<AnCoWeaponCaseHackComponent>
{
    public override Color Color { get; set; } = Color.Red;
    public override string Name { get; set; } = "wire-name-anco-weapon-case-hack";

    private bool _isFirstWire;
    public override object? StatusKey => _isFirstWire ? AnCoWeaponCaseHackWireActionKey.Status : null;

    public override bool AddWire(Wire wire, int count)
    {
        _isFirstWire = count == 1;
        return true;
    }

    public override StatusLightState? GetLightState(Wire wire, AnCoWeaponCaseHackComponent comp)
    {
        if (!_isFirstWire)
            return null;

        if (!EntityManager.TryGetComponent<LockComponent>(wire.Owner, out var lockComp))
            return StatusLightState.Off;

        return lockComp.Locked ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, AnCoWeaponCaseHackComponent comp)
    {
        if (!EntityManager.TryGetComponent<LockComponent>(wire.Owner, out var lockComp) || !lockComp.Locked)
            return true;

        if (!EntityManager.TryGetComponent<WiresComponent>(wire.Owner, out var wiresComp))
            return true;

        var serialNumber = wiresComp.SerialNumber;
        if (string.IsNullOrEmpty(serialNumber))
            return true;

        var (cutColor, pulseColor, pulseFirst) = GetCorrectWireColors(serialNumber);

        Logger.Info($"[AnCoWeaponCaseHack] Serial: {serialNumber}, Cut: {cutColor}, Pulse: {pulseColor}, PulseFirst: {pulseFirst}, Action: Cut {wire.Color}");

        if (wire.Color == cutColor)
        {
            // Если нужно сначала пульсировать, а пульс ещё не сделан - взрыв
            if (pulseFirst && !comp.PulseCompleted)
            {
                ExplodeAndDestroy(wire.Owner, user, comp);
                return true;
            }

            comp.CutCompleted = true;

            if (comp.PulseCompleted)
            {
                EntityManager.System<LockSystem>().Unlock(wire.Owner, user);
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("anco-weapon-case-hack-success"), wire.Owner, user);
                comp.CutCompleted = false;
                comp.PulseCompleted = false;
            }
            else
            {
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("anco-weapon-case-hack-cut-success"), wire.Owner, user);
            }
        }
        else
        {
            // Взрыв и удаление ящика при неправильном проводе
            ExplodeAndDestroy(wire.Owner, user, comp);
            return true;
        }

        return true;
    }

    private void ExplodeAndDestroy(EntityUid uid, EntityUid user, AnCoWeaponCaseHackComponent comp)
    {
        EntityManager.System<ExplosionSystem>().QueueExplosion(
            uid,
            comp.ExplosionPrototype,
            comp.ExplosionIntensity,
            comp.ExplosionSlope,
            comp.ExplosionMaxTileIntensity,
            canCreateVacuum: false);
        EntityManager.QueueDeleteEntity(uid);
    }

    public override bool Mend(EntityUid user, Wire wire, AnCoWeaponCaseHackComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, AnCoWeaponCaseHackComponent comp)
    {
        if (!EntityManager.TryGetComponent<LockComponent>(wire.Owner, out var lockComp) || !lockComp.Locked)
            return;

        if (!EntityManager.TryGetComponent<WiresComponent>(wire.Owner, out var wiresComp))
            return;

        var serialNumber = wiresComp.SerialNumber;
        if (string.IsNullOrEmpty(serialNumber))
            return;

        var (cutColor, pulseColor, pulseFirst) = GetCorrectWireColors(serialNumber);

        Logger.Info($"[AnCoWeaponCaseHack] Serial: {serialNumber}, Cut: {cutColor}, Pulse: {pulseColor}, PulseFirst: {pulseFirst}, Action: Pulse {wire.Color}");

        if (wire.Color == pulseColor)
        {
            // Если нужно сначала резать, а резка ещё не сделана - взрыв
            if (!pulseFirst && !comp.CutCompleted)
            {
                ExplodeAndDestroy(wire.Owner, user, comp);
                return;
            }

            comp.PulseCompleted = true;

            if (comp.CutCompleted)
            {
                EntityManager.System<LockSystem>().Unlock(wire.Owner, user);
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("anco-weapon-case-hack-success"), wire.Owner, user);
                comp.CutCompleted = false;
                comp.PulseCompleted = false;
            }
            else
            {
                EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("anco-weapon-case-hack-pulse-success"), wire.Owner, user);
            }
        }
        else
        {
            // Взрыв и удаление ящика при неправильном проводе
            ExplodeAndDestroy(wire.Owner, user, comp);
        }
    }

    private (WireColor cut, WireColor pulse, bool pulseFirst) GetCorrectWireColors(string serial)
    {
        var letters = serial.Length >= 4 ? serial[..4].ToUpperInvariant() : "";
        var digits = serial.Length >= 9 ? serial[5..9] : "";

        var digitValues = new List<int>();
        foreach (var c in digits)
        {
            if (char.IsDigit(c))
                digitValues.Add(c - '0');
        }

        var digitSum = digitValues.Sum();
        var vowels = new HashSet<char> { 'A', 'E', 'I', 'O', 'U' };
        var cleanSerial = serial.Replace("-", "");
        var vowelCount = letters.Count(c => vowels.Contains(c));

        // Rule1: Содержит буквы AnCo в любом порядке (pulseFirst)
        if (letters.Contains('A') && letters.Contains('N') && letters.Contains('C') && letters.Contains('O'))
            return (WireColor.Gray, WireColor.Purple, true);

        // Rule2: Содержит SS (cutFirst)
        if (serial.Contains("SS"))
            return (WireColor.Blue, WireColor.Red, false);

        // Rule3: Содержит NT (pulseFirst)
        if (serial.Contains("NT"))
            return (WireColor.Red, WireColor.Blue, true);

        // Rule4: Содержит 3026 (cutFirst)
        if (digits.Contains("3026") || serial.Contains("3026"))
            return (WireColor.Pink, WireColor.Gold, false);

        // Rule5: Первая и последняя буквы одинаковые (pulseFirst)
        if (letters.Length >= 4 && letters[0] == letters[3])
            return (WireColor.Purple, WireColor.Orange, true);

        // Rule6: X, Z и Y одновременно (cutFirst)
        if (letters.Contains('X') && letters.Contains('Z') && letters.Contains('Y'))
            return (WireColor.Fuchsia, WireColor.Cyan, false);

        // Rule7: Содержит буквы L,U,M,A в любом порядке (pulseFirst)
        if (letters.Contains('L') && letters.Contains('U') && letters.Contains('M') && letters.Contains('A'))
            return (WireColor.Brown, WireColor.Gold, true);

        // Rule8: Содержит буквы A,S,T,R + цифра 0 в любом порядке (cutFirst)
        if (letters.Contains('A') && letters.Contains('S') && letters.Contains('T') && letters.Contains('R') && digits.Contains('0'))
            return (WireColor.Gray, WireColor.Cyan, false);

        // Rule9: Серийник симметричен (cutFirst)
        if (IsPalindrome(cleanSerial))
            return (WireColor.Purple, WireColor.Navy, false);

        // Rule10: Все цифры одинаковые (pulseFirst)
        if (digitValues.Count >= 4 && digitValues.All(d => d == digitValues[0]))
            return (WireColor.Purple, WireColor.Green, true);

        // Rule11: Все буквы одинаковые (cutFirst)
        if (letters.Length >= 4 && letters.All(c => c == letters[0]))
            return (WireColor.Purple, WireColor.Brown, false);

        // Rule12: Все цифры простые (2,3,5,7) (pulseFirst)
        var primes = new HashSet<int> { 2, 3, 5, 7 };
        if (digitValues.Count >= 4 && digitValues.All(d => primes.Contains(d)))
            return (WireColor.Blue, WireColor.Pink, true);

        // Rule13: Произведение цифр < 300 (pulseFirst)
        if (digitValues.Count > 0)
        {
            var product = digitValues.Aggregate(1, (a, b) => a * b);
            if (product < 300)
                return (WireColor.Brown, WireColor.Green, true);
        }

        // Rule14: Сумма первых двух цифр = сумме последних двух (cutFirst)
        if (digitValues.Count >= 4 && digitValues[0] + digitValues[1] == digitValues[2] + digitValues[3])
            return (WireColor.Cyan, WireColor.Pink, false);

        // Rule15: Все цифры чётные (0,2,4,6,8) (pulseFirst)
        var evens = new HashSet<int> { 0, 2, 4, 6, 8 };
        if (digitValues.Count >= 4 && digitValues.All(d => evens.Contains(d)))
            return (WireColor.Navy, WireColor.Pink, true);

        // Rule16: Все цифры нечётные (1,3,5,7,9) (cutFirst)
        var odds = new HashSet<int> { 1, 3, 5, 7, 9 };
        if (digitValues.Count >= 4 && digitValues.All(d => odds.Contains(d)))
            return (WireColor.Pink, WireColor.Navy, false);

        // Rule17: Первая + последняя цифра = 10 (pulseFirst)
        if (digitValues.Count >= 2 && digitValues[0] + digitValues[^1] == 10)
            return (WireColor.Gold, WireColor.Purple, true);

        // Rule18: Разность первой и последней > 5 (cutFirst)
        if (digitValues.Count >= 2 && Math.Abs(digitValues[0] - digitValues[^1]) > 5)
            return (WireColor.Red, WireColor.Cyan, false);

        // Rule19: Цифры образуют возрастающую последовательность (pulseFirst)
        if (IsAscendingSequence(digitValues))
            return (WireColor.Cyan, WireColor.Orange, true);

        // Rule20: Более двух одинаковых символов (3+) (pulseFirst)
        if (HasMoreThanTwoSameSymbols(cleanSerial))
            return (WireColor.Gold, WireColor.Navy, true);

        // Rule21: Есть повтор буквы (pulseFirst)
        if (letters.GroupBy(c => c).Any(g => g.Count() > 1))
            return (WireColor.Orange, WireColor.Red, true);

        // Rule22: Есть буква Z (pulseFirst)
        if (letters.Contains('Z'))
            return (WireColor.Brown, WireColor.Fuchsia, true);

        // Rule23: Есть буква A (cutFirst)
        if (letters.Contains('A'))
            return (WireColor.Red, WireColor.Navy, false);

        // Rule24: Первая цифра больше последней (cutFirst)
        if (digitValues.Count >= 2 && digitValues[0] > digitValues[^1])
            return (WireColor.Blue, WireColor.Orange, false);

        // Rule25: Цифры в обратном порядке (pulseFirst)
        if (IsReverseOrdered(digitValues))
            return (WireColor.Fuchsia, WireColor.Red, true);

        // Rule26: Все цифры разные (cutFirst)
        if (digitValues.Distinct().Count() == digitValues.Count && digitValues.Count >= 4)
            return (WireColor.Cyan, WireColor.Brown, false);

        // Rule27: Две одинаковые цифры подряд (pulseFirst)
        for (int i = 0; i < digitValues.Count - 1; i++)
        {
            if (digitValues[i] == digitValues[i + 1])
                return (WireColor.Navy, WireColor.Gold, true);
        }

        // Rule28: Есть цифра 9 (cutFirst)
        if (digitValues.Contains(9))
            return (WireColor.Green, WireColor.Purple, false);

        // Rule29: Есть цифра 0 (pulseFirst)
        if (digitValues.Contains(0))
            return (WireColor.Orange, WireColor.Navy, true);

        // Rule30: Сумма цифр делится на 3 (pulseFirst)
        if (digitSum % 3 == 0)
            return (WireColor.Gray, WireColor.Red, true);

        // Rule31: Сумма цифр < 10 (pulseFirst)
        if (digitSum < 10)
            return (WireColor.Gold, WireColor.Fuchsia, true);

        // Rule32: Сумма цифр > 25 (cutFirst)
        if (digitSum > 25)
            return (WireColor.Brown, WireColor.Pink, false);

        // Rule33: Содержит кириллицу (редкое ~1%) (pulseFirst)
        if (serial.Any(c => c >= 'А' && c <= 'я' || c == 'Ё' || c == 'ё'))
            return (WireColor.Purple, WireColor.Gold, true);

        // Rule34: Содержит "ERP" или буквы E,R,P в любом порядке (cutFirst)
        if (serial.Contains("ERP") || (letters.Contains('E') && letters.Contains('R') && letters.Contains('P')))
            return (WireColor.Pink, WireColor.Brown, false);

        // Rule35: Содержит буквы E,V,I,L в любом порядке (pulseFirst)
        if (letters.Contains('E') && letters.Contains('V') && letters.Contains('I') && letters.Contains('L'))
            return (WireColor.Fuchsia, WireColor.Gray, true);

        // Rule36: Содержит буквы B,U,G в любом порядке (cutFirst)
        if (letters.Contains('B') && letters.Contains('U') && letters.Contains('G'))
            return (WireColor.Orange, WireColor.Green, false);

        // Rule37: Сумма цифр нечётная (pulseFirst)
        if (digitSum % 2 == 1)
            return (WireColor.Red, WireColor.Gold, true);

        // Rule38: Сумма цифр чётная (cutFirst)
        if (digitSum % 2 == 0)
            return (WireColor.Blue, WireColor.Green, false);

        return (WireColor.Navy, WireColor.Gray, false);
    }

    private static bool IsPalindrome(string s)
    {
        for (int i = 0; i < s.Length / 2; i++)
        {
            if (s[i] != s[s.Length - 1 - i])
                return false;
        }
        return true;
    }

    private static bool HasMoreThanTwoSameSymbols(string s)
    {
        return s.GroupBy(c => c).Any(g => g.Count() > 2);
    }

    private static bool IsEvenLetter(char c)
    {
        var pos = c - 'A' + 1;
        return pos % 2 == 0;
    }

    private static bool IsAlphabetical(string s)
    {
        if (s.Length < 2)
            return false;
        for (int i = 0; i < s.Length - 1; i++)
        {
            if (s[i] >= s[i + 1])
                return false;
        }
        return true;
    }

    private static bool IsReverseAlphabetical(string s)
    {
        if (s.Length < 2)
            return false;
        for (int i = 0; i < s.Length - 1; i++)
        {
            if (s[i] <= s[i + 1])
                return false;
        }
        return true;
    }

    private static bool IsAscendingSequence(List<int> digits)
    {
        if (digits.Count < 4)
            return false;
        for (int i = 0; i < digits.Count - 1; i++)
        {
            if (digits[i] + 1 != digits[i + 1])
                return false;
        }
        return true;
    }

    private static bool IsReverseOrdered(List<int> digits)
    {
        if (digits.Count < 2)
            return false;
        for (int i = 0; i < digits.Count - 1; i++)
        {
            if (digits[i] <= digits[i + 1])
                return false;
        }
        return true;
    }
}
