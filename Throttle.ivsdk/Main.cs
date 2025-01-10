using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using CCL.GTAIV;
using IVSDKDotNet;
using IVSDKDotNet.Enums;
using IVSDKDotNet.Native;
using System.Drawing;
using CCL.GTAIV.Extensions;
using static IVSDKDotNet.Native.Natives;

namespace Throttle
{
    public class Main : Script
    {
        public static bool enable;
        public static int throttleInc;
        public static Vector2 BarOffset;
        public static bool FreeWheelNoGas;
        public static bool dontCrash;
        public static bool allowDrawing;
        public static IVPed PlayerPed { get; private set; }
        private static IVVehicle playerVehicle;
        public static int throttleAmt;
        private static int msWhl;

        public Main()
        {
            Initialized += Main_Initialized;
            Tick += new EventHandler(this.MainTick);
            ProcessAutomobile += Main_ProcessAutomobile;
            OnImGuiRendering += Main_OnImGuiRendering;
        }

        private void Main_Initialized(object sender, EventArgs e)
        {
            LoadSettings(Settings);
            allowDrawing = false;
            throttleAmt = 100;
        }
        private void LoadSettings(SettingsFile settings)
        {
            enable = settings.GetBoolean("Throttle", "Enable", true);
            BarOffset = settings.GetVector2("Throttle", "Offset", new Vector2(100f, 90f));
            throttleInc = settings.GetInteger("Throttle", "Sensitivity", 5);
            FreeWheelNoGas = settings.GetBoolean("Throttle", "FreeWheelNoGas", true);
        }
        private void MainTick(object sender, EventArgs e)
        {
            if (!enable)
                return;

            PlayerPed = IVPed.FromUIntPtr(IVPlayerInfo.FindThePlayerPed());
            if (PlayerPed.IsInVehicle())
                dontCrash = true;

            if (dontCrash)
            {
                playerVehicle = IVVehicle.FromUIntPtr(PlayerPed.GetVehicle());

                if (playerVehicle != null && PlayerPed.IsInVehicle())
                {
                    if (!IS_CHAR_DEAD(PlayerPed.GetHandle()))
                    {
                        allowDrawing = true;
                        GET_MOUSE_WHEEL(out msWhl);
                        if (msWhl < 0)
                            throttleAmt += throttleInc;
                        else if (msWhl > 0)
                            throttleAmt -= throttleInc;

                        if (throttleAmt > 100)
                            throttleAmt = 100;

                        else if (throttleAmt < 0)
                            throttleAmt = 0;
                    }
                }
                else
                    allowDrawing = false;
            }
        }
        private void Main_ProcessAutomobile(UIntPtr vehPtr)
        {
            if (!enable)
                return;

            if (!dontCrash)
                return;

            if (playerVehicle != null && PlayerPed.IsInVehicle())
            {
                if (NativeControls.IsGameKeyPressed(0, GameKey.MoveForward) && playerVehicle.BrakePedal <= 0)
                    playerVehicle.GasPedal = ((float)throttleAmt / 100);
                else if (NativeControls.IsGameKeyPressed(0, GameKey.MoveForward) && playerVehicle.BrakePedal > 0)
                    playerVehicle.BrakePedal = ((float)throttleAmt / 100);
                else if (NativeControls.IsGameKeyPressed(0, GameKey.MoveBackward) && playerVehicle.BrakePedal <= 0)
                    playerVehicle.GasPedal = -((float)throttleAmt / 100);
                else if (NativeControls.IsGameKeyPressed(0, GameKey.MoveBackward) && playerVehicle.BrakePedal > 0)
                    playerVehicle.BrakePedal = ((float)throttleAmt / 100);
                else if (FreeWheelNoGas && !NativeControls.IsUsingController() && !NativeControls.IsGameKeyPressed(0, GameKey.MoveForward) && !NativeControls.IsGameKeyPressed(0, GameKey.MoveBackward))
                {
                    playerVehicle.GasPedal = (float)(0.005 / playerVehicle.Handling.DriveForce);
                    if (playerVehicle.GasPedal < 0.01f)
                        playerVehicle.GasPedal = 0.01f;
                    playerVehicle.BrakePedal = (float)(0.01 / playerVehicle.Handling.BrakeForce);
                }
                else if (FreeWheelNoGas && NativeControls.IsUsingController() && !NativeControls.IsGameKeyPressed(0, GameKey.Attack) && !NativeControls.IsGameKeyPressed(0, GameKey.Aim))
                {
                    playerVehicle.GasPedal = (float)(0.005 / playerVehicle.Handling.DriveForce);
                    if (playerVehicle.GasPedal < 0.01f)
                        playerVehicle.GasPedal = 0.01f;
                    playerVehicle.BrakePedal = (float)(0.01 / playerVehicle.Handling.BrakeForce);
                }
            }
        }
        private void Main_OnImGuiRendering(IntPtr devicePtr, ImGuiIV_DrawingContext ctx)
        {
            if (!allowDrawing)
                return;
            if (IVMenuManager.RadarMode == 0)
                return;
            if (IVCutsceneMgr.IsRunning())
                return;
            if (!(IS_PAUSE_MENU_ACTIVE() || IS_SCREEN_FADING_OUT() || IS_SCREEN_FADED_OUT()))
            {
                ImGuiIV.PushStyleVar(eImGuiStyleVar.WindowBorderSize, -15f);
                ImGuiIV.PushStyleColor(eImGuiCol.FrameBg, Color.FromArgb(100, Color.Black));

                ImGuiIV.SetNextWindowBgAlpha(0.0f);
                if (ImGuiIV.Begin("##ThrottleBar", eImGuiWindowFlags.NoTitleBar | eImGuiWindowFlags.AlwaysAutoResize | eImGuiWindowFlags.NoMove, eImGuiWindowFlagsEx.NoMouseEnable))
                {
                    ImGuiIV.ProgressBar(throttleAmt / 100f, new Vector2(100f, 20f), " ");
                    RectangleF rect = IVGame.GetRadarRectangle();
                    ImGuiIV.SetWindowPos(new Vector2(rect.Right, rect.Y) + BarOffset);
                }
                ImGuiIV.End();

                ImGuiIV.PopStyleVar(2);
                ImGuiIV.PopStyleColor();
            }

        }
    }
}

