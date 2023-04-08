using System;
using System.Text;
using LiveSplit.ComponentUtil;

namespace LiveSplit.Kingpin
{
    using ComponentAutosplitter;

    class KingpinGame : Game
    {
        private static readonly Type[] eventTypes = new Type[] { typeof(LoadedMapEvent),
                                                                 typeof(FinishedMapEvent),
                                                                 typeof(MapChangedEvent),
                                                                 typeof(FinalCutsceneEvent) };
        public override Type[] EventTypes => eventTypes;

        public override string Name => "Kingpin";
        public override string[] ProcessNames => new string[] { "kingpin" };

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

    class MapChangedEvent : NoAttributeEvent
    {
        public override string Description => "A different map was loaded.";

        public override bool HasOccured(GameInfo info)
        {
            return info.MapChanged;
        }

        public override string ToString()
        {
            return "Map has changed";
        }
    }

    class FinalCutsceneEvent : NoAttributeEvent
    {
        public override string Description => "Final cutscene on rcboss2 started.";

        public override bool HasOccured(GameInfo info)
        {
            return info.CurrentMap == "rcboss2.bsp" && info.InFinalCutscene;
        }

        public override string ToString()
        {
            return "Final cutscene started";
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
        public bool GameTimeExists => false;
        public bool LoadRemovalExists => true;

        public KingpinState PreviousGameState { get; private set; }
        public KingpinState CurrentGameState { get; private set; }
        public string PreviousMap { get; private set; }
        public string CurrentMap { get; private set; }
        public bool MapChanged { get; private set; }
        public bool InFinalCutscene { get; private set; }

        private Int32 mapAddress = 0x90A705;
        private Int32 gameStateAddress = 0xE7A180;
        private Int32 finalCutsceneAddress = 0xAA48DD;

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

            InFinalCutscene = false;
            if (gameProcess.ReadValue(baseAddress + finalCutsceneAddress, out byte finalCutscene))
            {
                InFinalCutscene = (finalCutscene != 0);
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
