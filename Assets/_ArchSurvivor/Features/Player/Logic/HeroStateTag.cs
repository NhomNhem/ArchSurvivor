namespace _ArchSurvivor.Features.Player.Logic {
    /// <summary>
    /// Represents the high-level states of Alric the Knight.
    /// This allows different systems (Movement, Combat, Animation) to sync seamlessly.
    /// </summary>
    public enum HeroStateTag {
        Locomotion, // Idle and Running
        Attacking,  // Stationary attack state
        Dashing,    // High speed movement with invincibility
        Stunned,    // Unable to act (for future enemies)
        Dead        // Game Over state
    }
}
