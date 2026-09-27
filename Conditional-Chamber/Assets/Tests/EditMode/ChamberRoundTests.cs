using System.Reflection;
using ChamberLogic;
using NUnit.Framework;
using UnityEngine;

public class ChamberRoundTests
{
    [Test]
    public void WeaponCarryRotation_TravelsThroughAWeightedArc()
    {
        var evaluate = typeof(ChamberLogicGame).GetMethod(
            "EvaluateCarriedWeaponRotation", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(evaluate, Is.Not.Null);
        var start = Quaternion.Euler(0f, 0f, 90f);
        var destination = Quaternion.Euler(0f, 0f, 30f);
        var turnAngle = Quaternion.Angle(start, destination);

        Quaternion At(float progress) => (Quaternion)evaluate.Invoke(
            null, new object[] { start, destination, progress, 0.43f, turnAngle, -1f });

        var quarter = At(0.25f);
        var middle = At(0.5f);
        var threeQuarter = At(0.75f);
        Assert.That(Quaternion.Angle(At(0f), start), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(At(1f), destination), Is.LessThan(0.01f));
        Assert.That(Quaternion.Angle(start, quarter), Is.GreaterThan(8f));
        Assert.That(Quaternion.Angle(quarter, destination), Is.GreaterThan(Quaternion.Angle(middle, destination)));
        Assert.That(Quaternion.Angle(middle, destination), Is.GreaterThan(Quaternion.Angle(threeQuarter, destination)));
        Assert.That(Quaternion.Angle(middle, Quaternion.Slerp(start, destination, 0.5f)), Is.GreaterThan(5f),
            "The carry path has no natural pitch or bank away from a mechanical fixed-axis turn.");
    }

    [Test]
    public void LiveChance_UpdatesAfterEveryRevealedShell()
    {
        var round = new ChamberRound(2, 4, 17);
        Assert.That(round.LiveChance, Is.EqualTo(2f / 6f));

        var beforeLive = round.RemainingLive;
        var beforeBlank = round.RemainingBlank;
        var shell = round.Fire();

        Assert.That(round.RemainingTotal, Is.EqualTo(5));
        Assert.That(round.RemainingLive, Is.EqualTo(beforeLive - (shell == Shell.Live ? 1 : 0)));
        Assert.That(round.RemainingBlank, Is.EqualTo(beforeBlank - (shell == Shell.Blank ? 1 : 0)));
        Assert.That(round.LiveChance, Is.EqualTo((float)round.RemainingLive / round.RemainingTotal));
    }
}
