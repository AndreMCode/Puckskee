using System;
using UnityEngine;

public static class GameEvents
{
    // ==========================================
    // GAMEPLAY EVENTS
    // ==========================================
    public static Action<int> OnTurnChanged;            // Broadcasts new turn count
    public static Action<int, float> OnDistanceUpdated; // Broadcasts player ID and puck distance covered
    public static Action<int, float> OnMassUpdated;     // Broadcasts player ID and current mass
    public static Action<int, float> OnFrictionUpdated; // Broadcasts player ID and current linear damping
    public static Action<int> OnPlayerSwapped;          // Broadcasts active player ID (1 or 2)
    public static Action<bool> OnPauseToggled;          // Broadcasts Pause status
    public static Action OnPuckLaunched;                // Fired when the puck is shot
    public static Action OnPuckStopped;                 // Fired when physics come to a rest

    // ==========================================
    // PRESENTATION & FEEDBACK EVENTS
    // ==========================================
    public static Action OnBumperHit;                  // Audio/VFX directors listen to this
    public static Action OnGoalScored;                 // Audio/VFX directors listen to this
    public static Action<AudioClip> OnPlayCustomSFX;   // Generic audio trigger

    // ==========================================
    // LIFECYCLE EVENTS
    // ==========================================
    public static Action<string, string> OnGameOver;           // Broadcasts win/loss result message
}