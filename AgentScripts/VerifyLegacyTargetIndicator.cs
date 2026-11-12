using System;
using System.Threading.Tasks;
using UnityEngine;
using EverWind.Gameplay;
public static class VerifyLegacyTargetIndicator
{
    public static async Task<string> Run()
    {
        var registry = UnityEngine.Object.FindFirstObjectByType<ServerTransceiverManager>();
        registry.Transport.Port = 14000;
        registry.Transport.Login("controls-preview", "test-only");
        float until = Time.realtimeSinceStartup + 8;
        while (!registry.Transport.Ready)
        {
            if (Time.realtimeSinceStartup > until) throw new TimeoutException(registry.Transport.Status);
            await Task.Delay(25);
        }
        var controller = registry.LocalActor.GetComponent<PlayerActionController>();
        controller.SelectNearest();
        await Task.Delay(150);
        var sprite = registry.LocalActor.GetComponentInChildren<SpriteRenderer>(true);
        if (controller.SelectedTarget == null || sprite == null || sprite.sprite == null || !sprite.gameObject.activeInHierarchy)
            throw new InvalidOperationException("Original target indicator did not become active");
        var camera = Camera.main.GetComponent<GameplayCameraController>();
        for (int i = 0; i < 12; ++i) camera.Orbit(new Vector2(0, -1), 0, true, .016f);
        await Task.Delay(100);
        return "PASS: original target sprite visible for entity " + controller.SelectedTarget.EntityId;
    }
}
