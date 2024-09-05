using System;

namespace MoSpeedUI;

public class WrongJavaVersion : Exception
{
    WrongJavaVersion(){}
    public WrongJavaVersion(string message) : base(message){}
    public WrongJavaVersion(string message, Exception inner) : base(message, inner){}
}