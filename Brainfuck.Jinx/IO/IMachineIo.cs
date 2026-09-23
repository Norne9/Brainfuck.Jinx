namespace Brainfuck.Jinx.IO;

public interface IMachineIo: IDisposable
{
    void Write(byte value);
    byte Read();
}