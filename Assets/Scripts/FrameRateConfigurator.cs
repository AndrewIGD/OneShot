using UnityEngine;
using Unity.MLAgents;

public static class FrameRateConfigurator
{
    private const int TargetFrameRate = 60;
    private const float TargetFixedDeltaTime = 1f / 60f;
    private const float TrainingMaximumDeltaTime = 0.5f;
    private const int PhysicsVelocityIterations = 4;
    private const int PhysicsPositionIterations = 2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ConfigureFrameRate()
    {
        ApplyFrameRateSettings();
    }

    public static void ApplyFrameRateSettings()
    {
        Time.fixedDeltaTime = TargetFixedDeltaTime;
        QualitySettings.vSyncCount = 0;

        Physics2D.reuseCollisionCallbacks = true;
        Physics2D.autoSyncTransforms = false;
        Physics2D.velocityIterations = PhysicsVelocityIterations;
        Physics2D.positionIterations = PhysicsPositionIterations;

        var playerNoHitLayer = LayerMask.NameToLayer("PlayerNoHit");
        if (playerNoHitLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerNoHitLayer, playerNoHitLayer, true);
        }

        var isTraining = TrainingModeChecker.IsTrainingMode;
        if (isTraining)
        {
            Application.targetFrameRate = -1;
            Time.maximumDeltaTime = TrainingMaximumDeltaTime;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }
        else
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = TargetFrameRate;
            Time.maximumDeltaTime = 0.05f;
            AudioListener.volume = 1f;
            AudioListener.pause = false;
        }
    }
}
