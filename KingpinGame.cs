using System;
using System.Text;
using LiveSplit.ComponentUtil;

namespace LiveSplit.Kingpin
{
    using ComponentAutosplitter;

    class KingpinGame : Game
    {
        private static readonly Type[] eventTypes = new Type[] { typeof(LoadedMapEvent),
                                                                 typeof(FinishedMapEvent) };
        public override Type[] EventTypes => eventTypes;

        public override string Name => "Kingpin";
        public override string[] ProcessNames => new string[] { "kingpin" };
        public override bool GameTimeExists => false;
        public override bool LoadRemovalExists => true;

        public KingpinGame() : base(new CustomSettingBool[]{})
        {
        }
    }

    abstract class KingpinMapEvent : MapEvent
    {
        public KingpinMapEvent() : base()
        {
        }

        public KingpinMapEvent(string map)
        {
            if (map.EndsWith(".bsp"))
            {
                this.map = map;
            }
            else
            {
                this.map = map + ".bsp";
            }

            attributeValues = new string[] { this.map };
        }
    }

    class LoadedMapEvent : KingpinMapEvent
    {
        public override string Description => "A certain map was loaded.";

        public LoadedMapEvent() : base()
        {
        }

        public LoadedMapEvent(string map) : base(map)
        {
        }

        public override bool HasOccured(GameInfo info)
        {
            return (info.PreviousGameState != KingpinState.InGame) &&
                   info.InGame && (info.CurrentMap == map);
        }

        public override string ToString()
        {
            return "Map '" + map + "' was loaded";
        }
    }

    class FinishedMapEvent : KingpinMapEvent
    {
        public override string Description => "A certain map was finished.";

        public FinishedMapEvent() : base()
        {
        }

        public FinishedMapEvent(string map) : base(map)
        {
        }

        public override bool HasOccured(GameInfo info)
        {
            return info.MapChanged && (info.CurrentMap != map) && (info.PreviousMap == map);
        }

        public override string ToString()
        {
            return "Map '" + map + "' was finished";
        }
    }

    public enum KingpinState
    {
        MenuOrLoading = 3, InGame = 4
    }
}

namespace LiveSplit.ComponentAutosplitter
{
    using Kingpin;

    partial class GameInfo
    {
        public KingpinState PreviousGameState { get; private set; }
        public KingpinState CurrentGameState { get; private set; }
        public string PreviousMap { get; private set; }
        public string CurrentMap { get; private set; }
        public bool MapChanged { get; private set; }

        private Int32 mapAddress = 0x90A705;
        private Int32 gameStateAddress = 0xE7A180;

        partial void UpdateInfo()
        {
            int gameState;
            if (gameProcess.ReadValue(baseAddress + gameStateAddress, out gameState))
            {
                PreviousGameState = CurrentGameState;
                CurrentGameState = (KingpinState)gameState;
            }

            if (PreviousGameState != CurrentGameState)
            {
                UpdateMap();
                InGame = (CurrentGameState == KingpinState.InGame);
            }
            else
            {
                MapChanged = false;
            }
        }

        public void UpdateMap()
        {
            StringBuilder mapStringBuilder = new StringBuilder(16);
            if (gameProcess.ReadString(baseAddress + mapAddress, mapStringBuilder) &&
                mapStringBuilder.ToString() != CurrentMap)
            {
                PreviousMap = CurrentMap;
                CurrentMap = mapStringBuilder.ToString();
                MapChanged = true;
            }
        }
    }
}
