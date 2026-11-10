using System.IO;
using Arsenal.Core.Data;
using Arsenal.Core.Profile;
using Arsenal.Core.Save;
using Arsenal.Core.Simulation;
using Arsenal.Core.Services;
using Xunit;

namespace Arsenal.Core.Tests
{
    public class SaveTests
    {
        [Fact]
        public void SaveLoad_RoundTrip_File()
        {
            var dir = Path.Combine(Path.GetTempPath(), "arsenal-test-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var (profile, arsenal, economy) = TestData.NewGame();
                economy.GrantReward(1234, 50);
                arsenal.CreateBuild("vk9_sentinel", "Ma config", out _);
                profile.settings.gyroEnabled = true;

                var save = new SaveService(new FileSaveStorage(dir), new TestJson());
                save.Save(profile);
                save.Save(profile); // seconde écriture => crée la copie .bak

                var loaded = save.Load(() => null, out var status);
                Assert.Equal(LoadStatus.Loaded, status);
                Assert.Equal(profile.money, loaded.money);
                Assert.Equal(2, loaded.builds.Count);
                Assert.True(loaded.settings.gyroEnabled);
                Assert.True(File.Exists(Path.Combine(dir, "profile.json.bak")));
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        [Fact]
        public void CorruptedSave_FallsBackToBackup()
        {
            var storage = new MemorySaveStorage();
            var save = new SaveService(storage, new TestJson());
            var p = new PlayerProfile { money = 10 };
            save.Save(p);
            p.money = 20;
            save.Save(p);
            storage.Data["profile"] = storage.Data["profile"].Replace("20", "99999"); // altère le contenu => somme invalide

            var loaded = save.Load(() => new PlayerProfile { money = -1 }, out var status);
            Assert.Equal(LoadStatus.RecoveredFromBackup, status);
            Assert.Equal(10, loaded.money);
        }

        [Fact]
        public void NoSave_CreatesNewProfile()
        {
            var save = new SaveService(new MemorySaveStorage(), new TestJson());
            var loaded = save.Load(() => new PlayerProfile { money = 77 }, out var status);
            Assert.Equal(LoadStatus.NewProfile, status);
            Assert.Equal(77, loaded.money);
        }

        [Fact]
        public void GarbageSave_ResetsCleanly()
        {
            var storage = new MemorySaveStorage();
            storage.Data["profile"] = "{ ceci n'est pas du json";
            var loaded = new SaveService(storage, new TestJson()).Load(() => new PlayerProfile { money = 5 }, out var status);
            Assert.Equal(LoadStatus.CorruptedReset, status);
            Assert.Equal(5, loaded.money);
        }
    }

    public class SimulationTests
    {
        private static StatBlock Stats(float rpm = 600, int mag = 3, float reload = 1f, float acc = 50, float recoil = 50, float handling = 50)
            => new StatBlock { damage = 30, accuracy = acc, recoil = recoil, fireRate = rpm, magazineSize = mag, handling = handling, reloadTime = reload };

        [Fact]
        public void SemiAuto_RequiresTriggerReset_AndRespectsFireRate()
        {
            var sim = new WeaponSimulation(Stats(rpm: 600), "semi");
            Assert.Equal(FireResult.Fired, sim.TriggerHeld());
            Assert.Equal(FireResult.TriggerNotReset, sim.TriggerHeld());
            sim.TriggerReleased();
            Assert.Equal(FireResult.Cooldown, sim.TriggerHeld());
            sim.TriggerReleased();
            sim.Tick(0.11f);
            Assert.Equal(FireResult.Fired, sim.TriggerHeld());
        }

        [Fact]
        public void Auto_EmptiesMagazine_ThenReloads()
        {
            var sim = new WeaponSimulation(Stats(rpm: 600, mag: 3, reload: 1f), "auto");
            int fired = 0;
            for (int i = 0; i < 100; i++) { if (sim.TriggerHeld() == FireResult.Fired) fired++; sim.Tick(0.02f); }
            Assert.Equal(3, fired);
            Assert.Equal(0, sim.Ammo);
            Assert.True(sim.StartReload());
            sim.Tick(0.5f);
            Assert.Equal(FireResult.Reloading, sim.TriggerHeld());
            sim.Tick(0.6f);
            Assert.False(sim.IsReloading);
            Assert.Equal(3, sim.Ammo);
        }

        [Fact]
        public void BetterStats_GiveTighterSpreadAndLessKick()
        {
            var good = new WeaponSimulation(Stats(acc: 90, recoil: 10));
            var bad = new WeaponSimulation(Stats(acc: 30, recoil: 80));
            Assert.True(good.CurrentSpreadDegrees < bad.CurrentSpreadDegrees);
            Assert.True(good.RecoilKickDegrees < bad.RecoilKickDegrees);
        }

        [Fact]
        public void Aiming_ReducesSpread_FasterWithHandling()
        {
            var agile = new WeaponSimulation(Stats(handling: 100)) { IsAiming = true };
            var heavy = new WeaponSimulation(Stats(handling: 0)) { IsAiming = true };
            float hip = agile.CurrentSpreadDegrees;
            agile.Tick(0.15f); heavy.Tick(0.15f);
            Assert.Equal(1f, agile.AimBlend, 3);
            Assert.True(heavy.AimBlend < 0.5f);
            Assert.True(agile.CurrentSpreadDegrees < hip);
        }
    }
}
