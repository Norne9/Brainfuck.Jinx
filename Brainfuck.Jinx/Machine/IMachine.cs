namespace Brainfuck.Jinx.Machine;

public interface IMachine
{
    void Add(int value);
    void Shift(int value);
    void Write();
    void Read();
    void SetZero();
    void Mul(int value, int buffer);
    void MulAndClear(int value, int buffer);
    void MulAndMul(int value, int buffer);
    bool IsZero();
}