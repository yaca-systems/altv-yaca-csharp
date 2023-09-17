using AltV.Net;
using AltV.Net.Elements.Entities;
using Console = Server.Helpers.Console;
using Server.Factories;

namespace Server
{
    internal class Index : Resource
    {
        #region Resource handling
        public override void OnStart()
        {
            Console.Information("--> Started resource!");
        }

        public override void OnStop()
        {
            Console.Information("--> Stopped resource!");
        }
        #endregion

        #region Factory handling
        public override IEntityFactory<IPlayer> GetPlayerFactory()
        {
            return new YaCAPlayerFactory();
        }

        public override IBaseObjectFactory<IColShape> GetColShapeFactory()
        {
            return new YaCAColShapeFactory();
        }
        #endregion
    }
}