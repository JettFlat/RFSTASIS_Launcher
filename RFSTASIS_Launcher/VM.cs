using System;
namespace RFSTASIS_Launcher
{
    public class VM : VMBase
    {
        public VM() : base()
        {
            Model.Start();
        }
        public string Text => "Flex";
    }
}
