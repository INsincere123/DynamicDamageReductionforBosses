using DynamicDamageReductionforBosses.Systems;
using Terraria;
using Terraria.ModLoader;

namespace DynamicDamageReductionforBosses.Players
{
    public class DDRPlayer : ModPlayer
    {
        public override void OnEnterWorld()
        {
            // 只对本地玩家执行，避免联机时重复打印
            if (Player != Main.LocalPlayer) return;
            BossFightTracker.FlushMessages();
        }
    }
}
