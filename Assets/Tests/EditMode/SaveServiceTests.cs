using System.Collections.Generic;
using System.IO;
using Chris.PachiRogue.Core;
using NUnit.Framework;

namespace Chris.PachiRogue.Tests.EditMode
{
    public class SaveServiceTests
    {
        private sealed class InMemorySaveBackend : ISaveBackend
        {
            private string _payload;

            public bool Exists() => _payload != null;

            public string Read() => _payload;

            public void Write(string payload) => _payload = payload;

            public void Delete() => _payload = null;
        }

        private static SaveModelV1 CreatePopulatedModel()
        {
            var model = new SaveModelV1
            {
                playerName = "Åsa-Britt",
                isRunActive = true,
                stageIndex = 7,
                launchesRemaining = 3,
                hp = 42,
                score = 123456789L,
                acquiredUpgradeIds = new List<string> { "bouncy_boots", "heavy_core", "moon_gravity" },
                metaCurrency = 999L,
                metNpcIds = new List<string> { "pip", "kapp" },
                passiveAbilityIds = new List<string> { "glowheart", "tailwind" },
                passiveAbilityLevels = new List<int> { 3, 1 },
                unlockFlags = new List<string> { "character.puni", "relic.starter_spring" },
                savedAtUnixUtc = 1760000000L
            };
            model.RunSeed = 0xDEADBEEFCAFEBABEUL;
            return model;
        }

        private static void AssertModelsEqual(SaveModelV1 expected, SaveModelV1 actual)
        {
            Assert.AreEqual(expected.version, actual.version);
            Assert.AreEqual(expected.playerName, actual.playerName);
            Assert.AreEqual(expected.runSeedBits, actual.runSeedBits);
            Assert.AreEqual(expected.RunSeed, actual.RunSeed);
            Assert.AreEqual(expected.isRunActive, actual.isRunActive);
            Assert.AreEqual(expected.stageIndex, actual.stageIndex);
            Assert.AreEqual(expected.launchesRemaining, actual.launchesRemaining);
            Assert.AreEqual(expected.hp, actual.hp);
            Assert.AreEqual(expected.score, actual.score);
            CollectionAssert.AreEqual(expected.acquiredUpgradeIds, actual.acquiredUpgradeIds);
            Assert.AreEqual(expected.metaCurrency, actual.metaCurrency);
            CollectionAssert.AreEqual(expected.metNpcIds, actual.metNpcIds);
            CollectionAssert.AreEqual(expected.passiveAbilityIds, actual.passiveAbilityIds);
            CollectionAssert.AreEqual(expected.passiveAbilityLevels, actual.passiveAbilityLevels);
            CollectionAssert.AreEqual(expected.unlockFlags, actual.unlockFlags);
            Assert.AreEqual(expected.savedAtUnixUtc, actual.savedAtUnixUtc);
        }

        [Test]
        public void RoundTrip_PreservesAllFields()
        {
            var service = new JsonSaveService(new InMemorySaveBackend());
            SaveModelV1 original = CreatePopulatedModel();

            service.Save(original);

            Assert.IsTrue(service.TryLoad(out SaveModelV1 loaded));
            AssertModelsEqual(original, loaded);
        }

        [Test]
        public void RoundTrip_PreservesUlongSeedExtremes()
        {
            var service = new JsonSaveService(new InMemorySaveBackend());

            foreach (ulong seed in new[] { 0UL, 1UL, ulong.MaxValue, 0x8000000000000000UL })
            {
                var model = new SaveModelV1 { RunSeed = seed };
                service.Save(model);

                Assert.IsTrue(service.TryLoad(out SaveModelV1 loaded));
                Assert.AreEqual(seed, loaded.RunSeed, $"Seed {seed} did not survive the round trip.");
            }
        }

        [Test]
        public void TryLoad_WithNoSave_ReturnsFalse()
        {
            var service = new JsonSaveService(new InMemorySaveBackend());

            Assert.IsFalse(service.HasSave);
            Assert.IsFalse(service.TryLoad(out SaveModelV1 model));
            Assert.IsNull(model);
        }

        [Test]
        public void TryLoad_WithMalformedJson_ReturnsFalse()
        {
            var backend = new InMemorySaveBackend();
            backend.Write("{ this is not json ]");
            var service = new JsonSaveService(backend);

            Assert.IsFalse(service.TryLoad(out SaveModelV1 model));
            Assert.IsNull(model);
        }

        [Test]
        public void TryLoad_WithUnsupportedVersion_ReturnsFalse()
        {
            var backend = new InMemorySaveBackend();
            var service = new JsonSaveService(backend);

            var future = new SaveModelV1();
            service.Save(future);
            backend.Write(backend.Read().Replace("\"version\":1", "\"version\":999"));

            Assert.IsFalse(service.TryLoad(out SaveModelV1 model));
            Assert.IsNull(model);
        }

        [Test]
        public void DeleteSave_RemovesExistingSave()
        {
            var service = new JsonSaveService(new InMemorySaveBackend());
            service.Save(new SaveModelV1());
            Assert.IsTrue(service.HasSave);

            service.DeleteSave();

            Assert.IsFalse(service.HasSave);
            Assert.IsFalse(service.TryLoad(out _));
        }

        [Test]
        public void Save_OverwritesPreviousSave()
        {
            var service = new JsonSaveService(new InMemorySaveBackend());

            service.Save(new SaveModelV1 { stageIndex = 1 });
            service.Save(new SaveModelV1 { stageIndex = 9 });

            Assert.IsTrue(service.TryLoad(out SaveModelV1 loaded));
            Assert.AreEqual(9, loaded.stageIndex);
        }

        [Test]
        public void FileBackend_RoundTripsThroughDisk()
        {
            string directory = Path.Combine(Path.GetTempPath(), "pachirogue_save_test_" + Path.GetRandomFileName());
            try
            {
                var service = new JsonSaveService(new FileSaveBackend(directory));
                SaveModelV1 original = CreatePopulatedModel();

                service.Save(original);

                // A fresh service instance over the same directory must see the save.
                var reopened = new JsonSaveService(new FileSaveBackend(directory));
                Assert.IsTrue(reopened.TryLoad(out SaveModelV1 loaded));
                AssertModelsEqual(original, loaded);

                reopened.DeleteSave();
                Assert.IsFalse(reopened.HasSave);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }
    }
}
