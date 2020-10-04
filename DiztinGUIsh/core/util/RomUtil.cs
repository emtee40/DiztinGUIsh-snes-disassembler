﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DiztinGUIsh.core.util
{
    public static class RomUtil
    {
        public static int GetBankSize(Data.ROMMapMode mode)
        {
            // todo
            return mode == Data.ROMMapMode.LoROM ? 0x8000 : 0x10000;
        }

        public static Data.ROMSpeed GetRomSpeed(int offset, IReadOnlyList<byte> romBytes) =>
            offset < romBytes.Count
                ? (romBytes[offset] & 0x10) != 0 ? Data.ROMSpeed.FastROM : Data.ROMSpeed.SlowROM
                : Data.ROMSpeed.Unknown;

        // verify the data in the provided ROM bytes matches the data we expect it to have.
        // returns error message if it's not identical, or null if everything is OK.
        public static string IsThisRomIsIdenticalToUs(byte[] rom,
            Data.ROMMapMode mode, string requiredGameNameMatch, int requiredRomChecksumMatch)
        {
            var romSettingsOffset = GetRomSettingOffset(mode);
            if (rom.Length <= romSettingsOffset + 10)
                return "The linked ROM is too small. It can't be opened.";

            var internalGameNameToVerify = GetRomTitleName(rom, romSettingsOffset);
            var checksumToVerify = ByteUtil.ByteArrayToInteger(rom, romSettingsOffset + 7);

            if (internalGameNameToVerify != requiredGameNameMatch)
                return $"The linked ROM's internal name '{internalGameNameToVerify}' doesn't " +
                       $"match the project's internal name of '{requiredGameNameMatch}'.";

            if (checksumToVerify != requiredRomChecksumMatch)
                return $"The linked ROM's checksums '{checksumToVerify:X8}' " +
                       $"don't match the project's checksums of '{requiredRomChecksumMatch:X8}'.";

            return null;
        }

        public static string GetRomTitleName(byte[] rom, int romSettingOffset)
        {
            var offsetOfGameTitle = romSettingOffset - LengthOfTitleName;
            var internalGameNameToVerify = ReadStringFromByteArray(rom, LengthOfTitleName, offsetOfGameTitle);
            return internalGameNameToVerify;
        }

        public static int ConvertSNESToPC(int address, Data.ROMMapMode mode, int size)
        {
            int _UnmirroredOffset(int offset) => UnmirroredOffset(offset, size);

            // WRAM is N/A to PC addressing
            if ((address & 0xFE0000) == 0x7E0000) return -1;

            // WRAM mirror & PPU regs are N/A to PC addressing
            if (((address & 0x400000) == 0) && ((address & 0x8000) == 0)) return -1;

            switch (mode)
            {
                case Data.ROMMapMode.LoROM:
                {
                    // SRAM is N/A to PC addressing
                    if (((address & 0x700000) == 0x700000) && ((address & 0x8000) == 0)) 
                        return -1;

                    return _UnmirroredOffset(((address & 0x7F0000) >> 1) | (address & 0x7FFF));
                }
                case Data.ROMMapMode.HiROM:
                {
                    return _UnmirroredOffset(address & 0x3FFFFF);
                }
                case Data.ROMMapMode.SuperMMC:
                {
                    return _UnmirroredOffset(address & 0x3FFFFF); // todo, treated as hirom atm
                }
                case Data.ROMMapMode.SA1ROM:
                case Data.ROMMapMode.ExSA1ROM:
                {
                    // BW-RAM is N/A to PC addressing
                    if (address >= 0x400000 && address <= 0x7FFFFF) return -1;

                    if (address >= 0xC00000)
                        return mode == Data.ROMMapMode.ExSA1ROM ? _UnmirroredOffset(address & 0x7FFFFF) : _UnmirroredOffset(address & 0x3FFFFF);

                    if (address >= 0x800000) address -= 0x400000;

                    // SRAM is N/A to PC addressing
                    if (((address & 0x8000) == 0)) return -1;

                    return _UnmirroredOffset(((address & 0x7F0000) >> 1) | (address & 0x7FFF));
                }
                case Data.ROMMapMode.SuperFX:
                {
                    // BW-RAM is N/A to PC addressing
                    if (address >= 0x600000 && address <= 0x7FFFFF) 
                        return -1;

                    if (address < 0x400000)
                        return _UnmirroredOffset(((address & 0x7F0000) >> 1) | (address & 0x7FFF));

                    if (address < 0x600000)
                        return _UnmirroredOffset(address & 0x3FFFFF);

                    if (address < 0xC00000)
                        return 0x200000 + _UnmirroredOffset(((address & 0x7F0000) >> 1) | (address & 0x7FFF));

                    return 0x400000 + _UnmirroredOffset(address & 0x3FFFFF);
                }
                case Data.ROMMapMode.ExHiROM:
                {
                    return _UnmirroredOffset(((~address & 0x800000) >> 1) | (address & 0x3FFFFF));
                }
                case Data.ROMMapMode.ExLoROM:
                {
                    // SRAM is N/A to PC addressing
                    if (((address & 0x700000) == 0x700000) && ((address & 0x8000) == 0)) 
                        return -1;

                    return _UnmirroredOffset((((address ^ 0x800000) & 0xFF0000) >> 1) | (address & 0x7FFF));
                }
                default:
                {
                    return -1;
                }
            }
        }

        public static int ConvertPCtoSNES(int offset, Data.ROMMapMode romMapMode, Data.ROMSpeed romSpeed)
        {
            switch (romMapMode)
            {
                case Data.ROMMapMode.LoROM:
                    offset = ((offset & 0x3F8000) << 1) | 0x8000 | (offset & 0x7FFF);
                    if (romSpeed == Data.ROMSpeed.FastROM || offset >= 0x7E0000) offset |= 0x800000;
                    return offset;
                case Data.ROMMapMode.HiROM:
                    offset |= 0x400000;
                    if (romSpeed == Data.ROMSpeed.FastROM || offset >= 0x7E0000) offset |= 0x800000;
                    return offset;
                case Data.ROMMapMode.ExHiROM when offset < 0x40000:
                    offset |= 0xC00000;
                    return offset;
                case Data.ROMMapMode.ExHiROM:
                    if (offset >= 0x7E0000) offset &= 0x3FFFFF;
                    return offset;
                case Data.ROMMapMode.ExSA1ROM when offset >= 0x400000:
                    offset += 0x800000;
                    return offset;
            }

            offset = ((offset & 0x3F8000) << 1) | 0x8000 | (offset & 0x7FFF);
            if (offset >= 0x400000) offset += 0x400000;

            return offset;
        }

        public static int UnmirroredOffset(int offset, int size)
        {
            // most of the time this is true; for efficiency
            if (offset < size) 
                return offset;

            int repeatSize = 0x8000;
            while (repeatSize < size) repeatSize <<= 1;

            int repeatedOffset = offset % repeatSize;

            // this will then be true for ROM sizes of powers of 2
            if (repeatedOffset < size) return repeatedOffset;

            // for ROM sizes not powers of 2, it's kinda ugly
            int sizeOfSmallerSection = 0x8000;
            while (size % (sizeOfSmallerSection << 1) == 0) sizeOfSmallerSection <<= 1;

            while (repeatedOffset >= size) repeatedOffset -= sizeOfSmallerSection;
            return repeatedOffset;
        }

        // TODO: these can be attributes on the enum itself. like [AsmLabel("UNREACH")]
        public static string TypeToLabel(Data.FlagType flag)
        {
            return flag switch
            {
                Data.FlagType.Unreached => "UNREACH",
                Data.FlagType.Opcode => "CODE",
                Data.FlagType.Operand => "LOOSE_OP",
                Data.FlagType.Data8Bit => "DATA8",
                Data.FlagType.Graphics => "GFX",
                Data.FlagType.Music => "MUSIC",
                Data.FlagType.Empty => "EMPTY",
                Data.FlagType.Data16Bit => "DATA16",
                Data.FlagType.Pointer16Bit => "PTR16",
                Data.FlagType.Data24Bit => "DATA24",
                Data.FlagType.Pointer24Bit => "PTR24",
                Data.FlagType.Data32Bit => "DATA32",
                Data.FlagType.Pointer32Bit => "PTR32",
                Data.FlagType.Text => "TEXT",
                _ => ""
            };
        }

        public static int TypeStepSize(Data.FlagType flag)
        {
            switch (flag)
            {
                case Data.FlagType.Unreached:
                case Data.FlagType.Opcode:
                case Data.FlagType.Operand:
                case Data.FlagType.Data8Bit:
                case Data.FlagType.Graphics:
                case Data.FlagType.Music:
                case Data.FlagType.Empty:
                case Data.FlagType.Text:
                    return 1;
                case Data.FlagType.Data16Bit:
                case Data.FlagType.Pointer16Bit:
                    return 2;
                case Data.FlagType.Data24Bit:
                case Data.FlagType.Pointer24Bit:
                    return 3;
                case Data.FlagType.Data32Bit:
                case Data.FlagType.Pointer32Bit:
                    return 4;
            }
            return 0;
        }

        public static Data.ROMMapMode DetectROMMapMode(IReadOnlyList<byte> romBytes, out bool detectedValidRomMapType)
        {
            detectedValidRomMapType = true;

            if ((romBytes[Data.LOROM_SETTING_OFFSET] & 0xEF) == 0x23)
                return romBytes.Count > 0x400000 ? Data.ROMMapMode.ExSA1ROM : Data.ROMMapMode.SA1ROM;

            if ((romBytes[Data.LOROM_SETTING_OFFSET] & 0xEC) == 0x20)
                return (romBytes[Data.LOROM_SETTING_OFFSET + 1] & 0xF0) == 0x10 ? Data.ROMMapMode.SuperFX : Data.ROMMapMode.LoROM;

            if (romBytes.Count >= 0x10000 && (romBytes[Data.HIROM_SETTING_OFFSET] & 0xEF) == 0x21)
                return Data.ROMMapMode.HiROM;

            if (romBytes.Count >= 0x10000 && (romBytes[Data.HIROM_SETTING_OFFSET] & 0xE7) == 0x22)
                return Data.ROMMapMode.SuperMMC;

            if (romBytes.Count >= 0x410000 && (romBytes[Data.EXHIROM_SETTING_OFFSET] & 0xEF) == 0x25)
                return Data.ROMMapMode.ExHiROM;

            // detection failed. take our best guess.....
            detectedValidRomMapType = false;
            return romBytes.Count > 0x40000 ? Data.ROMMapMode.ExLoROM : Data.ROMMapMode.LoROM;
        }

        public static int GetRomSettingOffset(Data.ROMMapMode mode)
        {
            return mode switch
            {
                Data.ROMMapMode.LoROM => Data.LOROM_SETTING_OFFSET,
                Data.ROMMapMode.HiROM => Data.HIROM_SETTING_OFFSET,
                Data.ROMMapMode.ExHiROM => Data.EXHIROM_SETTING_OFFSET,
                Data.ROMMapMode.ExLoROM => Data.EXLOROM_SETTING_OFFSET,
                _ => Data.LOROM_SETTING_OFFSET
            };
        }

        public static string PointToString(Data.InOutPoint point)
        {
            string result;

            if ((point & Data.InOutPoint.EndPoint) == Data.InOutPoint.EndPoint) result = "X";
            else if ((point & Data.InOutPoint.OutPoint) == Data.InOutPoint.OutPoint) result = "<";
            else result = " ";

            result += ((point & Data.InOutPoint.ReadPoint) == Data.InOutPoint.ReadPoint) ? "*" : " ";
            result += ((point & Data.InOutPoint.InPoint) == Data.InOutPoint.InPoint) ? ">" : " ";

            return result;
        }

        public static string BoolToSize(bool b)
        {
            return b ? "8" : "16";
        }

        // read a fixed length string from an array of bytes. does not check for null termination
        public static string ReadStringFromByteArray(byte[] bytes, int count, int offset)
        {
            var myName = "";
            for (var i = 0; i < count; i++)
                myName += (char)bytes[offset + i];

            return myName;
        }

        public static byte[] ReadAllRomBytesFromFile(string filename)
        {
            var smc = File.ReadAllBytes(filename);
            var rom = new byte[smc.Length & 0x7FFFFC00];

            if ((smc.Length & 0x3FF) == 0x200)
                // skip and dont include the SMC header
                for (int i = 0; i < rom.Length; i++)
                    rom[i] = smc[i + 0x200];
            else if ((smc.Length & 0x3FF) != 0)
                throw new InvalidDataException("This ROM has an unusual size. It can't be opened.");
            else
                rom = smc;

            if (rom.Length < 0x8000)
                throw new InvalidDataException("This ROM is too small. It can't be opened.");

            return rom;
        }

        public static void GenerateHeaderFlags(int romSettingsOffset, IDictionary<int, Data.FlagType> flags, byte[] romBytes)
        {
            for (int i = 0; i < LengthOfTitleName; i++)
                flags.Add(romSettingsOffset - LengthOfTitleName + i, Data.FlagType.Text);
            
            for (int i = 0; i < 7; i++) 
                flags.Add(romSettingsOffset + i, Data.FlagType.Data8Bit);
            
            for (int i = 0; i < 4; i++) 
                flags.Add(romSettingsOffset + 7 + i, Data.FlagType.Data16Bit);
            
            for (int i = 0; i < 0x20; i++) 
                flags.Add(romSettingsOffset + 11 + i, Data.FlagType.Pointer16Bit);

            if (romBytes[romSettingsOffset - 1] == 0)
            {
                flags.Remove(romSettingsOffset - 1);
                flags.Add(romSettingsOffset - 1, Data.FlagType.Data8Bit);
                for (int i = 0; i < 0x10; i++) 
                    flags.Add(romSettingsOffset - 0x25 + i, Data.FlagType.Data8Bit);
            }
            else if (romBytes[romSettingsOffset + 5] == 0x33)
            {
                for (int i = 0; i < 6; i++) 
                    flags.Add(romSettingsOffset - 0x25 + i, Data.FlagType.Text);

                for (int i = 0; i < 10; i++) 
                    flags.Add(romSettingsOffset - 0x1F + i, Data.FlagType.Data8Bit);
            }
        }


        public static Dictionary<int, Label> GenerateVectorLabels(Dictionary<string, bool> vectorNames, int romSettingsOffset, IReadOnlyList<byte> romBytes, Data.ROMMapMode mode)
        {
            // TODO: probably better to just use a data structure for this instead of generating the 
            // offsets with table/entry vars

            var labels = new Dictionary<int, Label>();

            var baseOffset = romSettingsOffset + 15;

            var table = 0; const int tableCount = 2;
            var entry = 0; const int entryCount = 6;
            foreach (var vectorEntry in vectorNames)
            {
                Debug.Assert(table >= 0 && table < tableCount);
                Debug.Assert(entry >= 0 && entry < entryCount);
                // table = 0,1              // which table of Native vs Emulation
                // entry = 0,1,2,3,4,5      // which offset
                //
                // 16*i = 16,32,

                var index = baseOffset + (16 * table) + (2 * entry);
                var offset = romBytes[index] + (romBytes[index + 1] << 8);
                var pc = ConvertSNESToPC(offset, mode, romBytes.Count);
                if (pc >= 0 && pc < romBytes.Count && !labels.ContainsKey(offset))
                    labels.Add(offset, new Label() { name = vectorEntry.Key });

                if (++entry < entryCount)
                    continue;

                entry = 0;
                if (++table >= tableCount)
                    break;
            }

            return labels;
        }

        public const int LengthOfTitleName = 0x15;
    }
}