using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class Day04EnemyTests
{
    private SKEnemyAgent a;
    private SKEnemyAgent b;
    private SKDay01Player player;
    private SKHealth health;
    private Keyboard keyboard;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Day05_EnemyPatterns");
        yield return null;
        var enemies = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).OrderBy(e => e.DisplayName).ToArray();
        Assert.AreEqual(2, enemies.Length);
        a = enemies[0]; b = enemies[1];
        b.gameObject.SetActive(false);
        a.AttacksEnabled = false;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        health = player.GetComponent<SKHealth>();
        PlacePlayer(new Vector2(-5.5f, -2.2f));
        yield return new WaitForSeconds(.25f);
        Set(a, "readyAt", 0f);
        Set(a, "phase", SKEnemyState.Patrol);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        yield return null;
    }

    private static void Set(object obj, string name, object value) => obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private static T Get<T>(object obj, string name) => (T)obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);

    private static IEnumerator Until(Func<bool> condition, float timeout = 3f)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        Assert.IsTrue(condition(), "Expected enemy transition before timeout");
    }

    private void PlacePlayer(Vector2 position)
    {
        player.transform.position = position;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private void PrepareAttack(SKEnemyAgent enemy, float distance)
    {
        Set(enemy, "patrolSpeed", 0f);
        Set(enemy, "chaseSpeed", 0f);
        Set(enemy, "readyAt", 0f);
        Set(enemy, "phase", SKEnemyState.Patrol);
        enemy.AttacksEnabled = true;
        PlacePlayer((Vector2)enemy.transform.position + Vector2.left * distance);
    }

    [UnityTest]
    public IEnumerator PatrolTurnsAtBothEndsWithoutLeavingItsRoute()
    {
        float home = a.Home.x;
        bool right = false, left = false;
        float minimum = home, maximum = home;
        // Rigidbody contact friction affects travel time; verify endpoint visits with a bounded wait.
        float until = Time.time + 12f;
        while (!(right && left) && Time.time < until)
        {
            float x = a.transform.position.x;
            minimum = Mathf.Min(minimum, x);
            maximum = Mathf.Max(maximum, x);
            Assert.LessOrEqual(Mathf.Abs(x - home), 1.25f);
            right |= x > home + 1f;
            left |= x < home - 1f;
            Assert.AreEqual(SKEnemyState.Patrol, a.State);
            yield return null;
        }
        Assert.IsTrue(right && left, $"The enemy must patrol both sides of spawn {home}; observed {minimum}..{maximum}");
    }

    [UnityTest]
    public IEnumerator DetectionChaseAndLostTargetReturnToPatrol()
    {
        PlacePlayer(new Vector2(-2.25f, -2.2f));
        float before = Vector2.Distance(player.transform.position, a.transform.position);
        yield return Until(() => a.State == SKEnemyState.Chase);
        yield return new WaitForSeconds(.5f);
        Assert.Less(Vector2.Distance(player.transform.position, a.transform.position), before - .4f);
        PlacePlayer(new Vector2(-5.8f, -2.2f));
        yield return Until(() => a.State == SKEnemyState.Return);
        yield return Until(() => a.State == SKEnemyState.Patrol);
        Assert.Less(Mathf.Abs(a.transform.position.x - a.Home.x), .1f);
    }

    [UnityTest]
    public IEnumerator WallsAndVerticalDistancePreventDetection()
    {
        Set(a, "patrolSpeed", 0f);
        PlacePlayer(new Vector2(-2f, -2.2f));
        var wall = new GameObject("Test_Sight_Blocker");
        wall.transform.position = new Vector3(-1f, 0f, 0f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 8f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.15f);
        Assert.AreEqual(SKEnemyState.Patrol, a.State);
        Object.Destroy(wall);
        PlacePlayer(new Vector2(a.transform.position.x, 2f));
        player.GetComponent<Rigidbody2D>().simulated = false;
        yield return new WaitForSeconds(.15f);
        Assert.AreEqual(SKEnemyState.Patrol, a.State);
    }

    [UnityTest]
    public IEnumerator PatrolDoesNotWalkOffAnEdgeOrThroughAWall()
    {
        var floor = new GameObject("Test_Short_Ledge");
        floor.transform.position = new Vector3(0f, 6f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(2f, .5f);
        a.transform.position = new Vector3(0f, 6.85f, 0f);
        a.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Set(a, "home", a.transform.position);
        Set(a, "patrolRadius", 3f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(2.5f);
        Assert.Greater(a.transform.position.y, 6.7f, "Ground probe must prevent a fall");
        Assert.Less(Mathf.Abs(a.transform.position.x), .7f);
        var wall = new GameObject("Test_Solid_Wall");
        wall.transform.position = new Vector3(.55f, 7f, 0f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 2f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(2f);
        Assert.Less(a.transform.position.x, .2f, "Solid wall blocks movement");
    }

    [UnityTest]
    public IEnumerator HomeLeashForcesReturnEvenWithTargetStillInSight()
    {
        Set(a, "leashRadius", .65f);
        PlacePlayer(new Vector2(-2.5f, -2.2f));
        yield return Until(() => a.State == SKEnemyState.Chase);
        yield return Until(() => a.State == SKEnemyState.Return);
        Assert.Less(Mathf.Abs(a.transform.position.x - a.Home.x), .75f);
        yield return Until(() => a.State == SKEnemyState.Patrol);
        Assert.Less(Mathf.Abs(a.transform.position.x - a.Home.x), .1f);
    }

    [UnityTest]
    public IEnumerator ACompletesTelegraphedAttackAndRecoveryOnce()
    {
        PrepareAttack(a, .95f);
        yield return Until(() => a.State == SKEnemyState.Windup);
        float committedX = a.transform.position.x;
        var warning = a.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>();
        Assert.IsTrue(warning.enabled);
        Assert.AreEqual(new Vector2(1f, .85f), warning.size);
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(100f, health.CurrentHp, "Windup gives time to evade");
        Assert.AreEqual(committedX, a.transform.position.x, .01f);
        yield return Until(() => a.State == SKEnemyState.Attacking);
        yield return Until(() => a.State == SKEnemyState.Recovery);
        Assert.AreEqual(85f, health.CurrentHp, "One strike deals 15, not repeated overlap damage");
        Assert.AreEqual(85f, Object.FindFirstObjectByType<SKDay01HUD>().DisplayedHp);
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(SKEnemyState.Recovery, a.State);
        Assert.AreEqual(85f, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator HeavyBHasLongerWarningWiderRangeAndStrongerSingleHit()
    {
        a.gameObject.SetActive(false);
        b.gameObject.SetActive(true);
        yield return new WaitForSeconds(.15f);
        PrepareAttack(b, 1.45f);
        yield return Until(() => b.State == SKEnemyState.Windup);
        var warning = b.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>();
        // 2026-11-10: B's wide swing is high (a crouch passes under it); the tell shows that height.
        Assert.AreEqual(new Vector2(1.6f, .5f), warning.size);
        Assert.AreEqual(-1f, warning.transform.localPosition.x);
        Assert.AreEqual(.475f, warning.transform.localPosition.y, .001f);
        yield return new WaitForSeconds(.55f);
        Assert.AreEqual(SKEnemyState.Windup, b.State);
        Assert.AreEqual(100f, health.CurrentHp);
        yield return Until(() => b.State == SKEnemyState.Recovery);
        Assert.AreEqual(75f, health.CurrentHp, "The wide charged strike deals 25 once");
        Assert.IsFalse(warning.enabled);
    }

    [UnityTest]
    public IEnumerator LeavingCommittedHeavyStrikeRangeAvoidsDamage()
    {
        a.gameObject.SetActive(false);
        b.gameObject.SetActive(true);
        yield return new WaitForSeconds(.15f);
        PrepareAttack(b, 1.45f);
        yield return Until(() => b.State == SKEnemyState.Windup);
        PlacePlayer((Vector2)b.transform.position + Vector2.left * 2.6f);
        yield return Until(() => b.State == SKEnemyState.Recovery);
        Assert.AreEqual(100f, health.CurrentHp);
        Assert.IsFalse(SKHitStop.IsActive, "A missed charged strike must not cause hit stop");
    }

    [UnityTest]
    public IEnumerator PlayerAttackInterruptsWindupAndDeathStopsMovement()
    {
        PrepareAttack(a, .85f);
        yield return Until(() => a.State == SKEnemyState.Windup);
        // The enemy is on the player's right; default player facing is right.
        Assert.IsTrue(player.GetComponent<SKPlayerCombat>().TryAttack());
        yield return Until(() => a.State == SKEnemyState.Hurt);
        Assert.AreEqual(40f, a.Health.CurrentHp);
        Assert.IsFalse(a.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>().enabled);
        Assert.IsFalse(a.GetComponentInChildren<SKHitbox>().IsActive);
        yield return new WaitForSeconds(.3f);
        Assert.AreEqual(100f, health.CurrentHp);
        Assert.IsTrue(a.Health.TryTakeDamage(1000f, player.transform.position));
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(SKEnemyState.Dead, a.State);
        Assert.AreEqual(0f, a.GetComponent<Rigidbody2D>().linearVelocity.x);
        yield return new WaitForSeconds(.8f);
        Assert.AreEqual(100f, health.CurrentHp);
    }

    [UnityTest]
    public IEnumerator PlayerDeathAndDisableCancelEnemyWindup()
    {
        PrepareAttack(a, .95f);
        yield return Until(() => a.State == SKEnemyState.Windup);
        a.enabled = false;
        Assert.AreEqual(SKEnemyState.Disabled, a.State);
        Assert.IsFalse(a.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>().enabled);
        Assert.IsFalse(a.GetComponentInChildren<SKHitbox>().IsActive);
        a.enabled = true;
        Set(a, "readyAt", 0f);
        yield return Until(() => a.State == SKEnemyState.Windup);
        health.TryTakeDamage(1000f, a.transform.position);
        yield return new WaitForSeconds(.7f);
        Assert.IsFalse(a.transform.Find("Attack_Warning").GetComponent<SpriteRenderer>().enabled);
        Assert.IsFalse(a.GetComponentInChildren<SKHitbox>().IsActive);
        Assert.AreEqual(0f, a.GetComponent<Rigidbody2D>().linearVelocity.x);
    }

    [UnityTest]
    public IEnumerator ResetKeyDuringHitStopRestoresAllEnemyHealthAndSpawns()
    {
        b.gameObject.SetActive(true);
        yield return null;
        a.Health.TryTakeDamage(1000f, player.transform.position);
        b.Health.TryTakeDamage(25f, player.transform.position);
        health.TryTakeDamage(25f, a.transform.position);
        a.transform.position += Vector3.left;
        b.transform.position += Vector3.left;
        SKHitStop.Request(.1f, player.gameObject.scene);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
        yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        Assert.IsFalse(SKHitStop.IsActive);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.AreEqual(100f, health.CurrentHp);
        Assert.AreEqual(60f, a.Health.CurrentHp);
        Assert.AreEqual(80f, b.Health.CurrentHp);
        Assert.AreEqual(a.Home.x, a.transform.position.x, .01f);
        Assert.AreEqual(b.Home.x, b.transform.position.x, .01f);
        Assert.AreEqual(SKEnemyState.Recovery, a.State);
        Assert.AreEqual(SKEnemyState.Recovery, b.State);
        Assert.AreEqual(-3f, player.transform.position.x, .01f);
    }

    [UnityTest]
    public IEnumerator Day04SceneKeepsAOnlyAndReusesExistingTargetArt()
    {
        yield return SceneManager.LoadSceneAsync("Day04_EnemyPatrol");
        yield return null;
        var enemies = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None);
        Assert.AreEqual(1, enemies.Length);
        Assert.AreEqual("ENEMY A - PATROL", enemies[0].DisplayName);
        Assert.AreEqual("training_dummy", enemies[0].GetComponent<SpriteRenderer>().sprite.name);
        Assert.IsNotNull(Object.FindFirstObjectByType<SKEnemyTrainingSession>());
        Assert.IsNull(Object.FindFirstObjectByType<SKTrainingTarget>());
        Assert.IsNull(Object.FindFirstObjectByType<SKTrainingSession>());
    }
}
