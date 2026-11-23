namespace SaintKnight.Day02
{
    // Priority: Disabled > Dead > Held > Hurt > active attack phase > Recovery > Ready.
    // Read from authoritative health/hitbox/timers rather than keeping duplicate flags.
    // Held is appended so existing numeric values stay unchanged.
    public enum SKCombatState { Disabled, Ready, Windup, Attacking, Recovery, Hurt, Dead, Held }
}
