﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Nmkoder.Data;
using Nmkoder.Extensions;
using Nmkoder.Forms;
using Nmkoder.IO;
using Nmkoder.Media;
using static Nmkoder.UI.Tasks.Av1anUi;

namespace Nmkoder.UI.Tasks
{
    class Av1an
    {
        public enum QualityMode { Crf, TargetVmaf }
        public enum ChunkMethod { Hybrid, Lsmash, Ffms2, Segment, Select }

        public static void Init()
        {
            Av1anUi.Init();
        }

        public static async Task Run()
        {
            Program.mainForm.SetWorking(true);
            string args = "";

            try
            {
                CodecUtils.Av1anCodec vCodec = GetCurrentCodecV();
                CodecUtils.AudioCodec aCodec = GetCurrentCodecA();
                bool vmaf = IsUsingVmaf();
                string inPath = TrackList.current.File.TruePath;
                string outPath = GetOutPath();
                string cust = Program.mainForm.av1anCustomArgsBox.Text.Trim();
                string custEnc = Program.mainForm.av1anCustomEncArgsBox.Text.Trim();
                CodecArgs codecArgs = CodecUtils.GetCodec(vCodec).GetArgs(GetVideoArgsFromUi(), TrackList.current.File, Data.Codecs.Pass.OneOfOne);
                string v = codecArgs.Arguments;
                string vf = await GetVideoFilterArgs(codecArgs);
                string a = CodecUtils.GetCodec(aCodec).GetArgs(GetAudioArgsFromUi()).Arguments;
                string w = Program.mainForm.av1anOptsWorkerCountUpDown.Value.ToString();
                string s = GetSplittingMethod();
                string m = GetChunkGenMethod();
                string c = GetConcatMethod();
                IoUtils.TryDeleteIfExists(outPath);

                args = $"-i {inPath.Wrap()} --verbose --keep --split-method {s} -m {m} -c {c} {cust} {v} -f \" {vf} \" -a \" {a} \" -w {w} -o {outPath.Wrap()}";

                if (vmaf)
                {
                    int q = (int)Program.mainForm.av1anQualityUpDown.Value;
                    string filters = vf.Length > 3 ? $"--vmaf-filter \" {vf.Split("-vf ").LastOrDefault()} \"" : "";
                    args += $" --target-quality {q} --vmaf-path {Paths.GetVmafPath(false).Wrap()} {filters} --vmaf-threads 2";
                }
               
                Logger.Log("av1an " + args);
            }
            catch (Exception e)
            {
                Logger.Log($"Error creating av1an command: {e.Message}\n{e.StackTrace}");
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Shift) // Allow reviewing and editing command if shift is held
            {
                EditCommandForm form = new EditCommandForm("av1an", args);
                form.ShowDialog();

                if (string.IsNullOrWhiteSpace(form.Args))
                {
                    Program.mainForm.SetWorking(false);
                    return;
                }

                args = form.Args;
            }

            Logger.Log($"Running:\nav1an {args}", true, false, "av1an");

            await AvProcess.RunAv1an(args, AvProcess.LogMode.OnlyLastLine, true);

            Program.mainForm.SetWorking(false);
        }

        public static int GetDefaultWorkerCount ()
        {
            return (int)Math.Ceiling((double)Environment.ProcessorCount * 0.4f);
        }
    }
}
