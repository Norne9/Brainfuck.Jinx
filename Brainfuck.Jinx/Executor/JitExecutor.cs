using System.Reflection;
using System.Reflection.Emit;

using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

public class JitExecutor: IExecutor
{
    private delegate void Executor(IMachine machine);
    
    private static readonly MethodInfo AddMethod = typeof(IMachine).GetMethod(nameof(IMachine.Add))!;
    private static readonly MethodInfo ShiftMethod = typeof(IMachine).GetMethod(nameof(IMachine.Shift))!;
    private static readonly MethodInfo WriteMethod = typeof(IMachine).GetMethod(nameof(IMachine.Write))!;
    private static readonly MethodInfo ReadMethod = typeof(IMachine).GetMethod(nameof(IMachine.Read))!;
    private static readonly MethodInfo SetZeroMethod = typeof(IMachine).GetMethod(nameof(IMachine.SetZero))!;
    private static readonly MethodInfo MulMethod = typeof(IMachine).GetMethod(nameof(IMachine.Mul))!;
    private static readonly MethodInfo MulAndClearMethod = typeof(IMachine).GetMethod(nameof(IMachine.MulAndClear))!;
    private static readonly MethodInfo MulAndMulMethod = typeof(IMachine).GetMethod(nameof(IMachine.MulAndMul))!;
    private static readonly MethodInfo IsZeroMethod = typeof(IMachine).GetMethod(nameof(IMachine.IsZero))!;

    public void Execute(IMachine machine, IReadOnlyList<OpCode> opcodes)
    {
        var executeMethod = new DynamicMethod(
            name: "Execute",
            returnType: typeof(void),
            parameterTypes: [typeof(IMachine)]
        );

        ILGenerator il = executeMethod.GetILGenerator();
        MakeCode(il, opcodes);
        il.Emit(OpCodes.Ret);
        
        Executor compiledExecute = (Executor)executeMethod.CreateDelegate(typeof(Executor));
        compiledExecute(machine);
    }

    private void MakeCode(ILGenerator il, IReadOnlyList<OpCode> opcodes)
    {
        foreach (OpCode opcode in opcodes)
        {
            switch (opcode.Type)
            {
                case OpCodeType.Add:
                    AddOneParamMethod(il, opcode.Value, AddMethod);
                    break;
                case OpCodeType.Shift:
                    AddOneParamMethod(il, opcode.Value, ShiftMethod);
                    break;
                case OpCodeType.Write:
                    AddNoParamMethod(il, WriteMethod);
                    break;
                case OpCodeType.Read:
                    AddNoParamMethod(il, ReadMethod);
                    break;
                case OpCodeType.Loop:
                    if (opcode.OpCodes is null || opcode.OpCodes.Count == 0)
                        break;
                    var loopCheckLabel = il.DefineLabel();
                    var loopBodyLabel = il.DefineLabel();
                    
                    il.Emit(OpCodes.Br, loopCheckLabel);
                    il.MarkLabel(loopBodyLabel);
                    
                    MakeCode(il, opcode.OpCodes);
                    
                    il.MarkLabel(loopCheckLabel);
                    AddNoParamMethod(il, IsZeroMethod);
                    
                    // Brfalse jumps if the value on the stack is 0 (false). 
                    // So, if IsZero is false (0), jump back up to the loop body.
                    il.Emit(OpCodes.Brfalse, loopBodyLabel);
                    break;
                case OpCodeType.SetZero:
                    AddNoParamMethod(il, SetZeroMethod);
                    break;
                case OpCodeType.Mul:
                    AddMulCall(il, opcode, MulMethod);
                    break;
                case OpCodeType.MulAndClear:
                    AddMulCall(il, opcode, MulAndClearMethod);
                    break;
                case OpCodeType.MulAndMul:
                    AddMulCall(il, opcode, MulAndMulMethod);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opcodes), opcode.Type,
                        $"Unknown OpCode: {opcode.Type}");
            }
        }
    }

    private static void AddOneParamMethod(ILGenerator il, int value, MethodInfo method)
    {
        il.Emit(OpCodes.Ldarg_0); // Load 'machine' (Arg 0)
        il.Emit(OpCodes.Ldc_I4, value); // Load integer
        il.Emit(OpCodes.Callvirt, method); // Call method
    }
    private static void AddNoParamMethod(ILGenerator il, MethodInfo method)
    {
        il.Emit(OpCodes.Ldarg_0); // Load 'machine' (Arg 0)
        il.Emit(OpCodes.Callvirt, method); // Call method
    }

    private static void AddMulCall(ILGenerator il, OpCode opcode, MethodInfo method)
    {
        il.Emit(OpCodes.Ldarg_0); // Load 'machine' (Arg 0)
        il.Emit(OpCodes.Ldc_I4, opcode.Value); // Load integer 1
        il.Emit(OpCodes.Ldc_I4, opcode.Buffer); // Load integer 1
        il.Emit(OpCodes.Callvirt, method); // Call method
        if (opcode.Offset != 0)
        {
            AddOneParamMethod(il, opcode.Offset, ShiftMethod);
        }
    }
}