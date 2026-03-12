using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Cycloside.Plugins.BuiltIn
{
    internal sealed class TileWorldImportedPack
    {
        public string DisplayName { get; init; } = string.Empty;

        public string DataFilePath { get; init; } = string.Empty;

        public string? ConfigFilePath { get; init; }

        public string Ruleset { get; init; } = "ms";

        public int LastLevel { get; init; }

        public int NativePlayableCount { get; init; }

        public IReadOnlyList<TileWorldImportedLevel> Levels { get; init; } = Array.Empty<TileWorldImportedLevel>();

        public override string ToString()
        {
            return $"{DisplayName} [{Ruleset}] ({NativePlayableCount}/{Levels.Count} native)";
        }
    }

    internal sealed class TileWorldImportedLevel
    {
        public int Number { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;

        public int TimeLimitSeconds { get; init; }

        public int ChipsRequired { get; init; }

        public string Hint { get; init; } = string.Empty;

        public int[] TopLayerCodes { get; init; } = Array.Empty<int>();

        public int[] BottomLayerCodes { get; init; } = Array.Empty<int>();

        public bool CanPlayNatively { get; init; }

        public IReadOnlyList<string> UnsupportedTiles { get; init; } = Array.Empty<string>();

        public TileWorldLevel? NativeLevel { get; init; }

        public override string ToString()
        {
            var label = string.IsNullOrWhiteSpace(Name) ? $"Level {Number}" : $"{Number}: {Name}";
            return CanPlayNatively ? label : $"{label} [preview]";
        }
    }

    internal static class TileWorldPackLibrary
    {
        private const ushort DatSignature = 0xAAAC;
        private const ushort MsRulesetSignature = 0x0002;
        private const ushort LynxRulesetSignature = 0x0102;
        private const int GridWidth = 32;
        private const int GridHeight = 32;
        private static readonly Encoding LevelEncoding = Encoding.Latin1;

        public static IReadOnlyList<TileWorldImportedPack> ScanLibrary(string rootPath, out string statusMessage)
        {
            var packs = new List<TileWorldImportedPack>();
            var issues = new List<string>();

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                statusMessage = "No Tile World library path configured.";
                return packs;
            }

            if (!Directory.Exists(rootPath))
            {
                statusMessage = $"Library path not found: {rootPath}";
                return packs;
            }

            var dataDirectory = Path.Combine(rootPath, "data");
            var setsDirectory = Path.Combine(rootPath, "sets");

            var referencedDataFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (Directory.Exists(setsDirectory))
            {
                foreach (var configPath in Directory.GetFiles(setsDirectory, "*.dac"))
                {
                    try
                    {
                        var config = ReadConfig(configPath);
                        if (string.IsNullOrWhiteSpace(config.DataFileName))
                        {
                            issues.Add($"{Path.GetFileName(configPath)} has no data file entry.");
                            continue;
                        }

                        var dataFilePath = ResolveDataFilePath(rootPath, dataDirectory, config.DataFileName);
                        if (!File.Exists(dataFilePath))
                        {
                            issues.Add($"{Path.GetFileName(configPath)} points to missing file {config.DataFileName}.");
                            continue;
                        }

                        referencedDataFiles.Add(Path.GetFullPath(dataFilePath));

                        var pack = ReadPack(dataFilePath, config.DisplayName, configPath, config.Ruleset, config.LastLevel);
                        packs.Add(pack);
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"{Path.GetFileName(configPath)} failed: {ex.Message}");
                    }
                }
            }

            if (Directory.Exists(dataDirectory))
            {
                foreach (var dataFilePath in Directory.GetFiles(dataDirectory, "*.dat"))
                {
                    try
                    {
                        if (referencedDataFiles.Contains(Path.GetFullPath(dataFilePath)))
                        {
                            continue;
                        }

                        var displayName = Path.GetFileNameWithoutExtension(dataFilePath) ?? Path.GetFileName(dataFilePath);
                        packs.Add(ReadPack(dataFilePath, displayName, null, string.Empty, 0));
                    }
                    catch (Exception ex)
                    {
                        issues.Add($"{Path.GetFileName(dataFilePath)} failed: {ex.Message}");
                    }
                }
            }

            packs.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));

            statusMessage = packs.Count == 0
                ? (issues.Count == 0 ? "No packs found." : string.Join(" | ", issues))
                : $"Loaded {packs.Count} pack(s)." + (issues.Count == 0 ? string.Empty : $" Issues: {string.Join(" | ", issues)}");

            return packs;
        }

        private static TileWorldImportedPack ReadPack(string dataFilePath, string displayName, string? configPath, string rulesetOverride, int lastLevel)
        {
            using var stream = File.OpenRead(dataFilePath);
            using var reader = new BinaryReader(stream, Encoding.UTF8, false);

            var signature = reader.ReadUInt16();
            if (signature != DatSignature)
            {
                throw new InvalidDataException("Not a valid .dat file.");
            }

            var ruleset = reader.ReadUInt16();
            var levelCount = reader.ReadUInt16();
            if (levelCount <= 0)
            {
                throw new InvalidDataException("Pack contains no levels.");
            }

            var packRuleset = NormalizeRuleset(rulesetOverride);
            if (string.IsNullOrWhiteSpace(packRuleset))
            {
                packRuleset = ruleset switch
                {
                    MsRulesetSignature => "ms",
                    LynxRulesetSignature => "lynx",
                    _ => "unknown"
                };
            }

            var levels = new List<TileWorldImportedLevel>(levelCount);
            for (var index = 0; index < levelCount; index++)
            {
                var levelSize = reader.ReadUInt16();
                var levelBytes = reader.ReadBytes(levelSize);
                if (levelBytes.Length != levelSize)
                {
                    throw new InvalidDataException("Level data ended unexpectedly.");
                }

                levels.Add(ParseLevel(levelBytes));
            }

            var nativePlayableCount = 0;
            foreach (var level in levels)
            {
                if (level.CanPlayNatively)
                {
                    nativePlayableCount++;
                }
            }

            return new TileWorldImportedPack
            {
                DisplayName = displayName,
                DataFilePath = dataFilePath,
                ConfigFilePath = configPath,
                Ruleset = packRuleset,
                LastLevel = lastLevel,
                NativePlayableCount = nativePlayableCount,
                Levels = levels
            };
        }

        private static TileWorldImportedLevel ParseLevel(byte[] bytes)
        {
            if (bytes.Length < 10)
            {
                throw new InvalidDataException("Level data is too small.");
            }

            var levelNumber = ReadUInt16(bytes, 0);
            var timeLimit = ReadUInt16(bytes, 2);
            var chipsRequired = ReadUInt16(bytes, 4);
            var topLayerSize = ReadUInt16(bytes, 8);
            var offset = 10;

            var topLayer = DecodeLayer(bytes, ref offset, topLayerSize);
            var bottomLayerSize = ReadUInt16(bytes, offset);
            offset += 2;
            var bottomLayer = DecodeLayer(bytes, ref offset, bottomLayerSize);

            var name = $"Level {levelNumber}";
            var password = string.Empty;
            var hint = string.Empty;

            if (offset + 2 <= bytes.Length)
            {
                var metadataSize = ReadUInt16(bytes, offset);
                offset += 2;
                var metadataEnd = Math.Min(bytes.Length, offset + metadataSize);
                while (offset + 2 <= metadataEnd)
                {
                    var fieldId = bytes[offset];
                    var fieldSize = bytes[offset + 1];
                    offset += 2;

                    var actualSize = (int)fieldSize;
                    if (offset + actualSize > metadataEnd)
                    {
                        actualSize = metadataEnd - offset;
                    }

                    if (actualSize < 0)
                    {
                        actualSize = 0;
                    }

                    switch (fieldId)
                    {
                        case 2:
                            if (actualSize >= 2)
                            {
                                chipsRequired = ReadUInt16(bytes, offset);
                            }
                            break;
                        case 3:
                            if (actualSize > 0)
                            {
                                name = ReadText(bytes, offset, actualSize);
                            }
                            break;
                        case 6:
                            if (actualSize > 0)
                            {
                                password = ReadPassword(bytes, offset, actualSize);
                            }
                            break;
                        case 7:
                            if (actualSize > 0)
                            {
                                hint = ReadText(bytes, offset, actualSize);
                            }
                            break;
                    }

                    offset += actualSize;
                }
            }

            var unsupportedTiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nativeLevel = TryCreateNativeLevel(name, hint, topLayer, bottomLayer, unsupportedTiles);

            return new TileWorldImportedLevel
            {
                Number = levelNumber,
                Name = name,
                Password = password,
                TimeLimitSeconds = timeLimit,
                ChipsRequired = chipsRequired,
                Hint = hint,
                TopLayerCodes = topLayer,
                BottomLayerCodes = bottomLayer,
                CanPlayNatively = nativeLevel != null,
                UnsupportedTiles = unsupportedTiles.OrderBy(tile => tile, StringComparer.OrdinalIgnoreCase).ToArray(),
                NativeLevel = nativeLevel
            };
        }

        private static int[] DecodeLayer(byte[] bytes, ref int offset, int encodedSize)
        {
            if (offset + encodedSize > bytes.Length)
            {
                throw new InvalidDataException("Encoded layer is truncated.");
            }

            var cells = new int[GridWidth * GridHeight];
            var encodedEnd = offset + encodedSize;
            var position = 0;

            while (offset < encodedEnd && position < cells.Length)
            {
                var current = bytes[offset++];
                if (current == 0xFF)
                {
                    if (offset + 2 > encodedEnd)
                    {
                        break;
                    }

                    var repeat = bytes[offset++];
                    var repeatedCode = bytes[offset++];
                    for (var count = 0; count < repeat && position < cells.Length; count++)
                    {
                        cells[position++] = repeatedCode;
                    }
                }
                else
                {
                    cells[position++] = current;
                }
            }

            offset = encodedEnd;
            return cells;
        }

        private static TileWorldLevel? TryCreateNativeLevel(string name, string hint, int[] topLayer, int[] bottomLayer, HashSet<string> unsupportedTiles)
        {
            var tiles = new TileWorldTile[GridWidth, GridHeight];
            TileWorldPoint? playerStart = null;

            for (var y = 0; y < GridHeight; y++)
            {
                for (var x = 0; x < GridWidth; x++)
                {
                    var index = y * GridWidth + x;
                    if (!TryResolveCell(topLayer[index], bottomLayer[index], ref playerStart, new TileWorldPoint(x, y), out var tile, unsupportedTiles))
                    {
                        return null;
                    }

                    tiles[x, y] = tile;
                }
            }

            if (playerStart == null)
            {
                unsupportedTiles.Add("Missing player start");
                return null;
            }

            return new TileWorldLevel(name, string.IsNullOrWhiteSpace(hint) ? "Imported from a local Tile World pack." : hint, tiles, playerStart.Value);
        }

        private static bool TryResolveCell(int topCode, int bottomCode, ref TileWorldPoint? playerStart, TileWorldPoint position, out TileWorldTile tile, HashSet<string> unsupportedTiles)
        {
            if (!TryMapRawCode(bottomCode, out var bottomTile, out var bottomBehavior))
            {
                unsupportedTiles.Add(GetTileName(bottomCode));
                tile = TileWorldTile.Floor;
                return false;
            }

            if (IsPlayerCode(bottomCode))
            {
                playerStart = position;
                bottomTile = TileWorldTile.Floor;
                bottomBehavior = TilePlacementBehavior.Base;
            }

            tile = bottomTile;
            if (topCode == 0)
            {
                return true;
            }

            if (IsPlayerCode(topCode))
            {
                playerStart = position;
                return true;
            }

            if (!TryMapRawCode(topCode, out var topTile, out var topBehavior))
            {
                unsupportedTiles.Add(GetTileName(topCode));
                return false;
            }

            if (topBehavior == TilePlacementBehavior.Base && bottomBehavior == TilePlacementBehavior.Base && bottomCode != 0)
            {
                unsupportedTiles.Add($"{GetTileName(bottomCode)} + {GetTileName(topCode)}");
                return false;
            }

            if (topBehavior == TilePlacementBehavior.Overlay && bottomBehavior != TilePlacementBehavior.BaseFloor)
            {
                unsupportedTiles.Add($"{GetTileName(topCode)} on {GetTileName(bottomCode)}");
                return false;
            }

            tile = topTile;
            return true;
        }

        private static bool TryMapRawCode(int rawCode, out TileWorldTile tile, out TilePlacementBehavior behavior)
        {
            switch (rawCode)
            {
                case 0x00:
                    tile = TileWorldTile.Floor;
                    behavior = TilePlacementBehavior.BaseFloor;
                    return true;
                case 0x01:
                    tile = TileWorldTile.Wall;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x02:
                    tile = TileWorldTile.Chip;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x03:
                    tile = TileWorldTile.Water;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x04:
                    tile = TileWorldTile.Fire;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x0A:
                case 0x0E:
                case 0x0F:
                case 0x10:
                case 0x11:
                    tile = TileWorldTile.Block;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x0B:
                case 0x2D:
                    tile = TileWorldTile.Floor;
                    behavior = TilePlacementBehavior.BaseFloor;
                    return true;
                case 0x15:
                    tile = TileWorldTile.Exit;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x16:
                    tile = TileWorldTile.DoorBlue;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x17:
                    tile = TileWorldTile.DoorRed;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x18:
                    tile = TileWorldTile.DoorGreen;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x19:
                    tile = TileWorldTile.DoorYellow;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x22:
                    tile = TileWorldTile.Socket;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x2F:
                    tile = TileWorldTile.Hint;
                    behavior = TilePlacementBehavior.Base;
                    return true;
                case 0x64:
                    tile = TileWorldTile.KeyBlue;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x65:
                    tile = TileWorldTile.KeyRed;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x66:
                    tile = TileWorldTile.KeyGreen;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x67:
                    tile = TileWorldTile.KeyYellow;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x68:
                    tile = TileWorldTile.BootsWater;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                case 0x69:
                    tile = TileWorldTile.BootsFire;
                    behavior = TilePlacementBehavior.Overlay;
                    return true;
                default:
                    tile = TileWorldTile.Floor;
                    behavior = TilePlacementBehavior.Base;
                    return false;
            }
        }

        private static bool IsPlayerCode(int rawCode)
        {
            return rawCode >= 0x6C && rawCode <= 0x6F;
        }

        private static string GetTileName(int rawCode)
        {
            return rawCode switch
            {
                0x00 => "Empty",
                0x01 => "Wall",
                0x02 => "Chip",
                0x03 => "Water",
                0x04 => "Fire",
                0x0A => "Block",
                0x0B => "Dirt",
                0x0C => "Ice",
                0x0D => "Force South",
                0x0E => "Cloning Block North",
                0x0F => "Cloning Block West",
                0x10 => "Cloning Block South",
                0x11 => "Cloning Block East",
                0x12 => "Force North",
                0x13 => "Force East",
                0x14 => "Force West",
                0x15 => "Exit",
                0x16 => "Blue Door",
                0x17 => "Red Door",
                0x18 => "Green Door",
                0x19 => "Yellow Door",
                0x1A => "Ice Corner SE",
                0x1B => "Ice Corner SW",
                0x1C => "Ice Corner NW",
                0x1D => "Ice Corner NE",
                0x21 => "Thief",
                0x22 => "Socket",
                0x23 => "Green Button",
                0x24 => "Red Button",
                0x27 => "Brown Button",
                0x28 => "Blue Button",
                0x29 => "Teleport",
                0x2A => "Bomb",
                0x2B => "Trap",
                0x2D => "Gravel",
                0x2F => "Hint",
                0x31 => "Clone Machine",
                0x32 => "Random Force Floor",
                0x40 or 0x41 or 0x42 or 0x43 => "Bug",
                0x44 or 0x45 or 0x46 or 0x47 => "Fireball",
                0x48 or 0x49 or 0x4A or 0x4B => "Ball",
                0x4C or 0x4D or 0x4E or 0x4F => "Tank",
                0x50 or 0x51 or 0x52 or 0x53 => "Glider",
                0x54 or 0x55 or 0x56 or 0x57 => "Teeth",
                0x58 or 0x59 or 0x5A or 0x5B => "Walker",
                0x5C or 0x5D or 0x5E or 0x5F => "Blob",
                0x60 or 0x61 or 0x62 or 0x63 => "Paramecium",
                0x64 => "Blue Key",
                0x65 => "Red Key",
                0x66 => "Green Key",
                0x67 => "Yellow Key",
                0x68 => "Flippers",
                0x69 => "Fire Boots",
                0x6A => "Ice Skates",
                0x6B => "Suction Boots",
                0x6C or 0x6D or 0x6E or 0x6F => "Chip",
                _ => $"Tile 0x{rawCode:X2}"
            };
        }

        private static string ResolveDataFilePath(string rootPath, string dataDirectory, string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return configuredPath;
            }

            var direct = Path.Combine(dataDirectory, configuredPath);
            if (File.Exists(direct))
            {
                return direct;
            }

            return Path.Combine(rootPath, configuredPath);
        }

        private static TileWorldConfig ReadConfig(string configPath)
        {
            var config = new TileWorldConfig
            {
                DisplayName = Path.GetFileNameWithoutExtension(configPath) ?? Path.GetFileName(configPath)
            };

            foreach (var rawLine in File.ReadAllLines(configPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim().ToLowerInvariant();
                var value = line[(separatorIndex + 1)..].Trim();

                switch (key)
                {
                    case "file":
                        config.DataFileName = value;
                        break;
                    case "ruleset":
                        config.Ruleset = NormalizeRuleset(value);
                        break;
                    case "lastlevel":
                        if (int.TryParse(value, out var lastLevel))
                        {
                            config.LastLevel = lastLevel;
                        }
                        break;
                }
            }

            return config;
        }

        private static ushort ReadUInt16(byte[] bytes, int offset)
        {
            if (offset + 1 >= bytes.Length)
            {
                return 0;
            }

            return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
        }

        private static string ReadText(byte[] bytes, int offset, int length)
        {
            var text = LevelEncoding.GetString(bytes, offset, length);
            return text.Replace('\0', ' ').Trim();
        }

        private static string ReadPassword(byte[] bytes, int offset, int length)
        {
            var buffer = new char[length];
            var count = 0;

            for (var index = 0; index < length; index++)
            {
                var decoded = (char)(bytes[offset + index] ^ 0x99);
                if (decoded == '\0')
                {
                    break;
                }

                buffer[count++] = decoded;
            }

            return new string(buffer, 0, count).Trim();
        }

        private static string NormalizeRuleset(string ruleset)
        {
            if (string.IsNullOrWhiteSpace(ruleset))
            {
                return string.Empty;
            }

            var value = ruleset.Trim().ToLowerInvariant();
            return value switch
            {
                "microsoft" => "ms",
                "windows" => "ms",
                _ => value
            };
        }

        private sealed class TileWorldConfig
        {
            public string DisplayName { get; init; } = string.Empty;

            public string DataFileName { get; set; } = string.Empty;

            public string Ruleset { get; set; } = string.Empty;

            public int LastLevel { get; set; }
        }

        private enum TilePlacementBehavior
        {
            Base,
            BaseFloor,
            Overlay
        }
    }
}
