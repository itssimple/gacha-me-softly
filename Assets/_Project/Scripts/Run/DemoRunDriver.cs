using System;
using System.Collections.Generic;
using Chris.PachiRogue.Core;
using UnityEngine;

namespace Chris.PachiRogue.Run
{
    /// <summary>
    /// PLACEHOLDER stage flow until Phases 2–4 (launch physics, procgen, run
    /// loop) exist: advances the "run" by publishing the same
    /// <see cref="StageCleared"/> events real gameplay will publish, so the
    /// interlude/meta systems are exercised end-to-end today. Delete this
    /// component from the Boot scene when the real run loop lands.
    /// </summary>
    public sealed class DemoRunDriver : MonoBehaviour, IServiceConsumer
    {
        private const int BaseShardsPerStage = 20;
        private const int ShardsPerStageIndex = 5;

        private IEventBus _eventBus;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public void InitServices(ServiceRegistry services)
        {
            _eventBus = services.Resolve<IEventBus>();

            _subscriptions.Add(_eventBus.Subscribe<StoryIntroCompleted>(_ => PublishStageCleared(1)));
            _subscriptions.Add(_eventBus.Subscribe<InterludeCompleted>(OnInterludeCompleted));
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }

        private void OnInterludeCompleted(InterludeCompleted evt)
        {
            if (evt.StageIndex < InterludePlanner.FinalStage)
            {
                PublishStageCleared(evt.StageIndex + 1);
            }
        }

        private void PublishStageCleared(int stageIndex)
        {
            _eventBus.Publish(new StageCleared
            {
                StageIndex = stageIndex,
                ShardsAwarded = BaseShardsPerStage + ShardsPerStageIndex * stageIndex
            });
        }
    }
}
