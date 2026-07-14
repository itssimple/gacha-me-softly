using System;
using UnityEngine;

namespace Chris.PachiRogue.Core
{
    /// <summary>
    /// The one and only bootstrap MonoBehaviour (Boot scene). Builds the
    /// <see cref="ServiceRegistry"/> and registers Core services; everything
    /// else receives its dependencies via injection from here. This is the
    /// only class allowed to touch scene-global concerns like
    /// DontDestroyOnLoad (see CLAUDE.md "No singletons").
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private const string SaveKey = "chris.pachirogue.save.v1";

        [Tooltip("Session seed override for reproducible runs. 0 = derive a fresh seed at boot.")]
        [SerializeField]
        private long seedOverride;

        public ServiceRegistry Services { get; private set; }

        private void Awake()
        {
            // Mobile target (PLAN.md Phase 5); harmless elsewhere.
            Application.targetFrameRate = 60;

            Services = new ServiceRegistry();

            // Seed entry point: the only place a non-IRngService source may
            // feed randomness, and only to mint the session seed itself.
            ulong seed = seedOverride != 0
                ? unchecked((ulong)seedOverride)
                : unchecked((ulong)DateTime.UtcNow.Ticks ^ ((ulong)Environment.TickCount << 32));

            Services.Register<IRngService>(new RngService(seed));
            Services.Register<IEventBus>(new EventBus());
            Services.Register<ISaveService>(new JsonSaveService(CreateSaveBackend()));

            InjectSceneConsumers();

            DontDestroyOnLoad(gameObject);
        }

        private void InjectSceneConsumers()
        {
            // Scene-wide object lookup is permitted in bootstrap only — this
            // is the composition root wiring the Boot scene (CLAUDE.md).
            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IServiceConsumer consumer)
                {
                    consumer.InitServices(Services);
                }
            }
        }

        private static ISaveBackend CreateSaveBackend()
        {
            // Runtime platform check instead of #if so both backends stay
            // compiled and testable on every platform (PLAN.md Phase 1).
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return new PlayerPrefsSaveBackend(SaveKey);
            }

            return new FileSaveBackend(Application.persistentDataPath);
        }
    }
}
