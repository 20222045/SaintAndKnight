using System;
using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

/// <summary>
/// Simulated playtest of the Day 5 scene with human-like input timing. Not a regression gate:
/// it only logs "PLAYTEST|" measurements. Run explicitly with -testFilter PlaytestProbe.
/// </summary>
[Explicit("Measurement probe, run on demand")]
[Category("Playtest")]
public sealed class PlaytestProbe
{
    private Keyboard keyboard;
    private SKDay01Player player;
    private SKPlayerCombat combat;
    private SKHealth health;
    private SKGrabState grab;
    private SKEnemyAgent a;
    private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
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

    private static void Log(string line) => Debug.Log("PLAYTEST|" + line);
    private static void Set(object obj, string name, object value) => obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(obj, value);
    private static T Get<T>(object obj, string name) => (T)obj.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);

    private IEnumerator Load()
    {
        SKHitStop.CancelActive();
        Time.timeScale = 1f;
        Keys();
        yield return SceneManager.LoadSceneAsync("Day05_EnemyPatterns");
        yield return null;
        var enemies = Object.FindObjectsByType<SKEnemyAgent>(FindObjectsSortMode.None).OrderBy(e => e.DisplayName).ToArray();
        a = enemies[0];
        enemies[1].gameObject.SetActive(false);
        a.AttacksEnabled = false;
        player = Object.FindFirstObjectByType<SKDay01Player>();
        combat = player.GetComponent<SKPlayerCombat>();
        health = player.GetComponent<SKHealth>();
        grab = combat.Grab;
        Place(new Vector2(-5.5f, -2.2f));
        yield return new WaitForSeconds(.3f);
    }

    private void Place(Vector2 at)
    {
        player.transform.position = at;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }

    private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private IEnumerator WaitFor(Func<bool> condition, float timeout)
    {
        float end = Time.time + timeout;
        while (!condition() && Time.time < end) yield return null;
    }

    // ---------------------------------------------------------------- jump
    [UnityTest]
    public IEnumerator JumpHeightsByHoldTime()
    {
        foreach (float scale in new[] { .45f, 1f })
        foreach (float hold in new[] { .05f, .08f, .12f, .18f, .25f, .4f })
        {
            yield return Load();
            Set(player, "jumpReleaseVelocityScale", scale);
            yield return WaitFor(() => player.IsGrounded, 2f);
            float start = player.transform.position.y, peak = start, t0 = Time.time, x0 = player.transform.position.x;
            Keys(Key.Space, Key.D);
            bool released = false, left = false;
            float landed = -1f;
            while (Time.time < t0 + 2f)
            {
                yield return null;
                if (!released && Time.time >= t0 + hold) { Keys(Key.D); released = true; }
                peak = Mathf.Max(peak, player.transform.position.y);
                if (!player.IsGrounded) left = true;
                if (left && player.IsGrounded && player.GetComponent<Rigidbody2D>().linearVelocity.y <= 0f)
                { landed = Time.time - t0; break; }
            }
            float dist = player.transform.position.x - x0;
            Keys();
            Log($"jump|scale={scale:0.00}|hold={hold:0.00}|height={peak - start:0.00}|air={landed:0.00}|runDist={dist:0.00}");
        }
    }

    // ------------------------------------------------- reacting to the grab tell
    [UnityTest]
    public IEnumerator GrabTellReaction()
    {
        foreach (float windup in new[] { .3f, .45f })
        foreach (bool armored in new[] { false, true })
        foreach (string response in new[] { "walk", "jump", "attack" })
        foreach (float reaction in new[] { .15f, .2f, .25f, .3f, .35f })
        {
            if (armored && windup < .4f) continue;
            yield return Load();
            Set(a, "grabWindupSeconds", windup);
            Set(a, "grabWindupArmored", armored);
            a.AttacksEnabled = true;
            Place((Vector2)a.transform.position + new Vector2(-.35f, -.05f));
            Set(a, "readyAt", 0f);
            Set(a, "phase", SKEnemyState.Patrol);
            yield return WaitFor(() => a.State == SKEnemyState.GrabWindup, 2f);
            bool started = a.State == SKEnemyState.GrabWindup;
            yield return new WaitForSeconds(reaction);
            if (response == "walk") Keys(Key.A);
            else if (response == "jump") Keys(Key.Space);
            else Keys(Key.J);
            bool held = false;
            float end = Time.time + .6f;
            while (Time.time < end) { yield return null; held |= grab.IsHeld; if (response == "attack") Keys(); }
            Keys();
            Log($"tell|windup={windup:0.00}|armored={armored}|response={response}|reaction={reaction:0.00}|tellSeen={started}|held={held}|enemyHp={a.Health.CurrentHp:0}");
        }
    }

    // ------------------------------------------------------------ mash escape
    [UnityTest]
    public IEnumerator MashRates()
    {
        foreach (float rate in new[] { 3f, 4f, 5f, 6f, 7f, 8f, 10f, 12f })
        {
            yield return Load();
            a.AttacksEnabled = true;
            Place((Vector2)a.transform.position + new Vector2(-.35f, -.05f));
            Set(a, "readyAt", 0f);
            Set(a, "phase", SKEnemyState.Patrol);
            yield return WaitFor(() => grab.IsHeld, 2f);
            if (!grab.IsHeld) { Log($"mash|rate={rate}|noGrab"); continue; }
            float t0 = Time.time, hp0 = health.CurrentHp, next = t0;
            int presses = 0;
            bool down = false;
            while (grab.IsHeld && Time.time < t0 + 6f)
            {
                if (Time.time >= next)
                {
                    Keys(presses % 2 == 0 ? Key.J : Key.Space);
                    down = true;
                    presses++;
                    next += 1f / rate;
                }
                else if (down && Time.time >= next - .5f / rate) { Keys(); down = false; }
                yield return null;
            }
            Keys();
            float took = Time.time - t0;
            bool thrown = took >= 3.95f;
            Log($"mash|rate={rate:0}|outcome={(thrown ? "THROWN" : "ESCAPED")}|time={took:0.00}|presses={presses}|hpLost={hp0 - health.CurrentHp:0}");
        }
    }

    // ------------------------------------------------------------- fight bots
    [UnityTest, Timeout(600000)] // 24 fights of up to 15 s each: longer than the default 180 s
    public IEnumerator FightBots()
    {
        foreach (string variant in new[] { "current", "tuned" })
        foreach (string enemy in new[] { "A", "B" })
        foreach (int character in new[] { 0, 1 })
        foreach (string style in new[] { "kite_free", "kite_rooted", "masher" })
        {
            yield return Load();
            var all = Resources.FindObjectsOfTypeAll<SKEnemyAgent>().Where(e => e.gameObject.scene.IsValid())
                .OrderBy(e => e.DisplayName).ToArray();
            if (enemy == "B")
            {
                a.gameObject.SetActive(false);
                a = all[1];
                a.gameObject.SetActive(true);
                yield return null;
            }
            if (variant == "tuned")
            {
                if (enemy == "A")
                {
                    Set(a, "hurtRecoverySeconds", .3f);
                    Set(a, "grabWindupSeconds", .45f);
                    Set(a, "grabWindupArmored", true);
                }
                else
                {
                    Set(a, "hurtRecoverySeconds", .5f);
                    Set(a, "strikeArmored", true);
                }
            }
            if (character == 1) { player.SwitchCharacter(); yield return null; }
            a.AttacksEnabled = true;
            Place((Vector2)a.transform.position + new Vector2(-2.6f, -.05f));
            Set(a, "readyAt", 0f);
            yield return Fight(character, style, variant + "|" + enemy);
        }
    }

    private IEnumerator Fight(int character, string style, string tag)
    {
        float reach = character == 0 ? 1.9f : 1.6f; // hitbox front edge + enemy hurtbox half width
        // Enemy strike front edge + player hurtbox half width + a small safety margin.
        float danger = Get<float>(a, "attackOffset") + Get<Vector2>(a, "attackSize").x * .5f + .35f;
        float t0 = Time.time;
        float hpStart = health.CurrentHp;
        int strikes = 0, grabs = 0, hits = 0, deaths = 0;
        float lastEnemyHp = a.Health.CurrentHp;
        var lastState = a.State;
        bool lastJ = false, lastMash = false;
        float minDist = 99f;
        var hitTimes = new List<float>();
        while (Time.time < t0 + 15f && !a.Health.IsDead)
        {
            float dx = a.transform.position.x - player.transform.position.x;
            float dist = Mathf.Abs(dx);
            minDist = Mathf.Min(minDist, dist);
            Key toward = dx >= 0f ? Key.D : Key.A, away = dx >= 0f ? Key.A : Key.D;
            var keys = new List<Key>();
            var s = a.State;
            if (s != lastState)
            {
                if (s == SKEnemyState.Windup) strikes++;
                if (s == SKEnemyState.Holding) grabs++;
                lastState = s;
            }
            if (a.Health.CurrentHp < lastEnemyHp) { hits++; hitTimes.Add(Time.time - t0); }
            lastEnemyHp = a.Health.CurrentHp;
            if (health.IsDead) { deaths++; break; }

            bool pressJ = false;
            if (grab.IsHeld)
            {
                if (!lastMash) keys.Add(Key.J);
                lastMash = !lastMash;
            }
            else if (style == "masher")
            {
                // Careless play: walk in and hammer attack.
                if (dist > .2f) keys.Add(toward);
                pressJ = combat.State == SKCombatState.Ready;
            }
            else if (combat.LocksFacing)
            {
                if (style == "kite_free") keys.Add(away); // retreat during windup/strike
            }
            else if ((s == SKEnemyState.Windup || s == SKEnemyState.GrabWindup) && dist < danger) keys.Add(away);
            else if (combat.State == SKCombatState.Ready && dist <= reach - .15f && dist >= .6f)
            {
                keys.Add(toward);
                pressJ = true;
            }
            else if (dist > reach - .3f) keys.Add(toward);
            else if (dist < .9f) keys.Add(away);

            if (pressJ && !lastJ) keys.Add(Key.J);
            lastJ = pressJ && !lastJ;
            Keys(keys.ToArray());
            yield return null;
        }
        Keys();
        string label = character == 0 ? "Cecilia" : "Adela";
        string gaps = hitTimes.Count > 1
            ? string.Join(",", hitTimes.Zip(hitTimes.Skip(1), (p, q) => (q - p).ToString("0.00"))) : "-";
        Log($"fight|{tag}|{label}|{style}|killed={a.Health.IsDead}|time={Time.time - t0:0.00}|playerDmg={hpStart - health.CurrentHp:0}" +
            $"|died={deaths > 0}|enemyStrikes={strikes}|grabs={grabs}|hits={hits}|hitGaps={gaps}|minDist={minDist:0.00}");
    }
}
