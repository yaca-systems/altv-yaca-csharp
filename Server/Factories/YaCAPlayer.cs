using AltV.Net;
using AltV.Net.Elements.Entities;

namespace Server.Factories
{
    #region Properties
    public class VoiceSettings
    {
        public int VoiceRange { get; set; }
        public bool VoiceFirstConnect { get; set; }
        public int MaxVoceRangeInMeter { get; set; }
        public bool Muted { get; set; }
        public string IngameName { get; set; }
    }

    public class VoicePlugin
    {
        public int ClientId { get; set; }
        public bool ForceMuted { get; set; }
        public int Range { get; set; }
        public int PlayerId { get; set; }
    }

    public class RadioSettings
    {
        public bool Activated { get; set; }
        public int CurrentChannel { get; set; }
        public bool HasLong { get; set; }
        public Dictionary<int, string> Frequencies { get; set; }
    }
    #endregion

    #region Factory handling
    internal class YaCAPlayer : Player
    {
        public VoiceSettings VoiceSettings { get; set; }
        public VoicePlugin VoicePlugin { get; set; }
        public RadioSettings RadioSettings { get; set; }

        public YaCAPlayer(ICore core, IntPtr nativePointer, ushort id) : base(core, nativePointer, id)
        {
            VoiceSettings = new VoiceSettings();
            VoicePlugin = new VoicePlugin();
            RadioSettings = new RadioSettings();
        }
    }

    public class YaCAPlayerFactory : IEntityFactory<IPlayer>
    {
        public IPlayer Create(ICore core, IntPtr nativePointer, ushort id) 
        {
            return new YaCAPlayer(core, nativePointer, id);
        }
    }
    #endregion
}
