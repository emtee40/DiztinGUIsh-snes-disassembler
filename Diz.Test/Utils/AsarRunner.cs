﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Diz.Test.Utils
{
    public static class AsarRunner
    {
        private const string pathToAsar = @"D:\projects\cthack\src\rom\bin\asar-domfix.exe";

        //  --no-title-check
        
        public static IReadOnlyList<byte> AssembleToRom(string assemblyCode)
        {
            var tmpOutputAsm = Path.GetTempPath() + Guid.NewGuid().ToString() + ".asm";
            var tmpOutputRom = Path.GetTempPath() + Guid.NewGuid().ToString() + ".sfc";
            
            using var swAsm = new StreamWriter(
                new FileStream(tmpOutputAsm, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 4096));
            
            swAsm.Write(assemblyCode);

            swAsm.Close(); swAsm.Dispose();

            var psi = new ProcessStartInfo(pathToAsar)
            {
                WorkingDirectory = Path.GetDirectoryName(pathToAsar),
                Arguments = $"\"{tmpOutputAsm}\" \"{tmpOutputRom}\""
            };
            Process.Start(psi).WaitForExit(20000);

            return File.ReadAllBytes(tmpOutputRom);
        }
    }
}