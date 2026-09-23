namespace Brainfuck.Jinx.IO;

public class SimpleIo: IMachineIo
{
    public void Write(byte value) => Console.Write((char)value);
    
    public byte Read() => (byte)Console.Read();

    public void Dispose()
    {
        
    }
}