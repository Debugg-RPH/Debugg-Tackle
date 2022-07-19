using System;
using System.Windows.Forms;
using Rage;
using Rage.Attributes;
using Rage.Native;

[assembly: Plugin("Debugg Tackle", Description = "Allows you to tackle peds | Made by Debugg#8770.", Author = "Debugg")]
namespace DebuggTackle
{
    public static class EntryPoint
    {
        //Simple + base
        private static bool TimerEnabled = false;
        private static float tackleMaxDistance = 1.0f;
        private static string tackleAnimDict = "swimming@first_person@diving";
        private static string tackleAnimName = "dive_run_fwd_-45_loop";

        //Config
        private static Keys tackleKey = Keys.G;
        private static bool advTackleEnb = true;

        //Advanced
        private static string tackleLib = "missmic2ig_11";
        private static string tackleAnim = "mic_2_ig_11_intro_goon";
        private static string tackleVictimAnim = "mic_2_ig_11_intro_p_one";

        public static InitializationFile initialiseFile()
        {
            InitializationFile ini = new InitializationFile("Plugins/DebuggTackle.ini");
            ini.Create();
            return ini;
        }

        public static void RegisterKeyMapping()
        {
            InitializationFile ini = initialiseFile();
            KeysConverter kc = new KeysConverter();
            advTackleEnb = ini.ReadBoolean("Keybindings", "advTackleEnb", true);
            tackleKey = (Keys)kc.ConvertFromString(ini.ReadString("Keybindings", "tackleKey", "G"));
        }

        private static void PlayAnim(string dict, string anim, int dur, int flags)
        {
            Ped myChar = Game.LocalPlayer.Character;
            myChar.Tasks.PlayAnimation(dict, anim, 8.0f, (AnimationFlags)flags);
            GameFiber.Wait(dur);
            myChar.Tasks.ClearSecondary();
        }

        private static void ClearPedTasksNow(Ped ped)
        {
            ped.Tasks.ClearImmediately();
            ped.Tasks.ClearSecondary();
            //ped.Tasks.Clear();
        }

        private static void PlayTackleOnPed(Ped ped, string dict, string anim, int dur, int flags, Ped player)
        {
            while (true)
            {
                TimerEnabled = true;
                Ped myChar = Game.LocalPlayer.Character;
                NativeFunction.Natives.TaskSetBlockingOfNonTemporaryEvents(ped, true);
                NativeFunction.Natives.SetPedFleeAttributes(ped, 0, false);
                ClearPedTasksNow(ped);
                NativeFunction.Natives.AttachEntityToEntity(ped, player, 11816, 0.25f, 0.5f, 0.0f, 0.5f, 0.5f, 180.0f, false, false, false, false, 2, false);
                myChar.Tasks.PlayAnimation(tackleLib, tackleAnim, 8.0f, (AnimationFlags)flags);
                GameFiber.Wait(5);
                ped.Tasks.PlayAnimation(dict, anim, 8.0f, AnimationFlags.SecondaryTask);
                GameFiber.Wait(dur);
                NativeFunction.Natives.TaskSetBlockingOfNonTemporaryEvents(ped, false);
                ClearPedTasksNow(myChar);
                NativeFunction.Natives.DetachEntity(ped, true, false);
                NativeFunction.Natives.SetPedToRagdoll(ped, randomInt(5000, 10000), randomInt(5000, 10000), 0);
                NativeFunction.Natives.SetEntityAsMissionEntity(ped, false, false);
                break;
            }
            
        }

        public static Ped GetClosestPed(Vector3 coords, float Radius)
        {
            if (coords != null) 
            { 
                if (Radius != 0)
                {
                    foreach (Ped ped in World.EnumeratePeds())
                    {
                        if (ped.IsValid() && ped != Game.LocalPlayer.Character)
                        {
                            Vector3 PedPosition = ped.Position;
                            if (PedPosition.DistanceTo(coords) <= Radius)
                            {
                                if (CanPedBeUsed(ped))
                                {
                                    NativeFunction.Natives.SetEntityAsMissionEntity(ped, true, true);
                                    return ped;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public static bool CanPedBeUsed(Ped ped)
        {
            if (ped == null) { return false; }
            if (!ped.IsValid()) { return false; }
            if (!ped.IsOnFoot) { return false; }
            if (ped.IsDead) { return false; }
            if (!ped.IsHuman) { return false; }
            if (ped == Game.LocalPlayer.Character) { return false; }
            return true;
        }

        public static void Main()
        {
            RegisterKeyMapping();
            GameFiber.StartNew(ProcessTackling);
        }

        public static void ProcessTackling()
        {
            while (true)
            {
                GameFiber.Yield();
                GameFiber.Wait(0);

                if (Game.GetKeyboardState().IsShiftDown)
                {
                    if (Game.IsKeyDown(tackleKey))
                    {
                        Vector3 myPos = Game.LocalPlayer.Character.Position;
                        Ped ped = GetClosestPed(myPos, tackleMaxDistance);
                        if (ped != null)
                        {
                            OnTackled(ped, Game.LocalPlayer.Character);
                            if (!advTackleEnb)
                            {
                                GameFiber.Wait(5);
                                OnTackling();
                            }
                        }
                    }
                }
            }
        }

        public static void OnTackling()
        {
            Ped myChar = Game.LocalPlayer.Character;
            PlayAnim(tackleAnimDict, tackleAnimName, 250, 49);
            NativeFunction.Natives.SetPedToRagdoll(myChar, 200, 200, 0);
            TimerEnabled = true;
            GameFiber.Wait(6000);
            TimerEnabled = false;
        }

        public static int randomInt(int min, int max)
        {
            Random r = new Random();
            int rInt = r.Next(min, max);
            return rInt;
        }

        public static void OnTackled(Ped tacklePed, Ped startPed)
        {
            if (advTackleEnb)
            {
                PlayTackleOnPed(tacklePed, tackleLib, tackleVictimAnim, 3000, 0, startPed);
                GameFiber.Wait(3000);
                TimerEnabled = false;
            } 
            else
            {
                NativeFunction.Natives.SetPedToRagdoll(tacklePed, randomInt(5000, 10000), randomInt(5000, 10000), 0);
            }
        }
    }
}
