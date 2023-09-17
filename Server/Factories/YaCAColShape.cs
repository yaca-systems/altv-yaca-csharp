using AltV.Net;
using AltV.Net.Elements.Entities;

namespace Server.Factories
{
    #region Properties
    public class VoiceRangeInfos
    {
        public int MaxRange { get; set; }
    }
    #endregion

    #region Factory handling
    internal class YaCAColShape : ColShape
    {
        public VoiceRangeInfos VoiceRangeInfos { get; set; }

        public YaCAColShape(ICore core, IntPtr nativePointer) : base(core, nativePointer)
        {
            VoiceRangeInfos = new VoiceRangeInfos();
        }
    }

    public class YaCAColShapeFactory : IBaseObjectFactory<IColShape>
    {
        public IColShape Create(ICore core, IntPtr nativePointer)
        {
            return new YaCAColShape(core, nativePointer);
        }
    }
    #endregion
}
