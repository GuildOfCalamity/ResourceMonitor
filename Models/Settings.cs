using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monitor;

public class Settings
{
    public string Theme { get; set; } = "Dark";
    public double Frequency { get; set; } = 3.0;
	public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int WindowHeight { get; set; }
    public int WindowWidth { get; set; }
	public bool CenterScreen { get; set; } // RFU
    public Settings() { }
    public override string ToString()
    {
        return $"[CenterScreen: {CenterScreen}] [Width:{WindowWidth}] [Height:{WindowHeight}] [X:{PositionX}] [Y:{PositionY}]";
    }
}
