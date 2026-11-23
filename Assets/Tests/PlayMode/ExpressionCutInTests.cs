using System.Collections;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Rest;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>2026-11-17 face close-up; since 2026-11-19 not added by the run (user), the component itself still checked.</summary>
public sealed class ExpressionCutInTests
{
    private SKDay01HUD hud;
    private SKHealth health;
    private SKExpressionCutIn cutIn;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("Day09_Stage1");
        yield return null;
        hud = Object.FindFirstObjectByType<SKDay01HUD>();
        health = Object.FindFirstObjectByType<SKDay01Player>().GetComponent<SKHealth>();
        yield return null;
        Assert.IsNull(hud.GetComponent<SKExpressionCutIn>(), "2026-11-19: the run no longer adds the square face close-up");
        cutIn = SKExpressionCutIn.Attach(hud);   // the component is kept; checked here on demand
    }

    [UnityTest]
    public IEnumerator AWorseStageShowsTheNewFaceOnceAndHealingDoesNot()
    {
        Assert.IsNotNull(cutIn);
        Assume.That(hud.Portrait.sprite != null && hud.Portrait.sprite.name.StartsWith("cecilia_") && hud.Portrait.sprite.name.Contains("stage"),
            "Needs the local (git-ignored) stage illustrations");
        Assert.IsFalse(cutIn.IsShowing);
        health.AdjustForTraining(-30f);                       // stage 0 -> 1
        yield return null;
        yield return null;
        Assert.IsTrue(cutIn.IsShowing, "Worse stage: the face pops up");
        Assert.AreEqual(hud.Portrait.sprite.texture, cutIn.Face.texture, "It is the face of the illustration now on the HUD");
        StringAssert.Contains("stage1", cutIn.Face.name);
        yield return new WaitForSecondsRealtime(SKExpressionCutIn.Show + .1f);
        Assert.IsFalse(cutIn.IsShowing, "Then it gets out of the way");
        health.AdjustForTraining(30f);                        // healing back to stage 0
        yield return null;
        yield return null;
        Assert.IsFalse(cutIn.IsShowing, "Getting better does not pop up");
    }

    [UnityTest, Explicit("Needs a graphics device; writes screenshots")]
    public IEnumerator CaptureCloseUps()
    {
        System.IO.Directory.CreateDirectory("Logs/RestInjury/Screens");
        for (int stage = 1; stage <= 3; stage++)
        {
            health.AdjustForTraining(-25f);
            yield return new WaitForSecondsRealtime(.35f);
            Day17To30Tests.Shoot("closeup_stage" + health.DamageStage, "Logs/RestInjury/Screens/");
            yield return new WaitForSecondsRealtime(SKExpressionCutIn.Show);
        }
    }
}
