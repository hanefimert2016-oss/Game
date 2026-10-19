namespace ArcSquad
{
    // Free of Unity dependencies so rules can be exercised with .NET on CI.
    public static class SquadRules
    {
        public const float MaxEnergy = 100f;
        public const float MissileCost = 20f;
        public const float MaxDelta = .05f;
        public static float ClampEnergy(float energy) => Clamp(energy, 0f, MaxEnergy);
        public static bool CanBoost(float energy) => energy > .001f;
        public static bool CanMissile(float energy, float cooldown) => energy >= MissileCost && cooldown <= 0f;
        public static float ApplyDamage(float current, float damage) => Clamp(current - (damage > 0f ? damage : 0f), 0f, 100f);
        public static int ScoreForKill(int wave) => 100 + (wave < 1 ? 1 : wave) * 20;
        public static bool ValidPort(int port) => port >= 1024 && port <= 65535;
        public static float LimitMovement(float dt) => Clamp(dt, 0f, MaxDelta);
        static float Clamp(float n, float lo, float hi) => n < lo ? lo : n > hi ? hi : n;
    }
}
