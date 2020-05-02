using System;
using System.Collections.Generic;
using System.Text;

namespace Emux.GameBoy.Cpu 
{
	public enum InterruptVector : ushort 
	{
		Custom1=	0x08,
		Custom2 =	0x10,
		Custom3=	0x18,
		Custom4=	0x20,
		Custom5=	0x28,
		Custom6=	0x30,
		Custom7=	0x38,
		VBlank =	GameBoyCpu.InterruptStartAddr,
		LcdStat =	GameBoyCpu.InterruptStartAddr + 8,	// 0x48
		Timer =		GameBoyCpu.InterruptStartAddr + 16, // 0x50
		Serial =	GameBoyCpu.InterruptStartAddr + 24, // 0x58
		Joypad =	GameBoyCpu.InterruptStartAddr + 32	// 0x60
	}
}
