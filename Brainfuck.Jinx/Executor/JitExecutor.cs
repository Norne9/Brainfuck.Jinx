using System.Reflection;
using System.Reflection.Emit;

using Brainfuck.Jinx.IO;
using Brainfuck.Jinx.Machine;

namespace Brainfuck.Jinx.Executor;

/// <summary>
/// An executor that compiles a parsed program into a
/// <see cref="DynamicMethod"/> and runs the generated IL, instead of walking the
/// op-code tree.
/// </summary>
/// <remarks>
/// <para>
/// There are two compilation paths, mirroring the two
/// <see cref="IExecutor"/> overloads:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="Execute(IMachine, IReadOnlyList{OpCode})"/> emits one
///     <see cref="IMachine"/> call per op-code, so the tape stays owned by the
///     caller and survives between runs.
///   </description></item>
///   <item><description>
///     <see cref="Execute(IMachineIo, IReadOnlyList{OpCode})"/> inlines the tape
///     as a local <see cref="byte"/>[] and the pointer as a local
///     <see cref="int"/>, eliminating the per-cell interface dispatch. This is
///     the fast path used by the command-line shell.
///   </description></item>
/// </list>
/// <para>
/// Both paths must match <see cref="InterpreterExecutor"/> exactly; the JIT is
/// only a faster encoding of the same semantics. The generated method is
/// discarded after each call, so a program is recompiled on every execution.
/// </para>
/// </remarks>
public class JitExecutor: IExecutor
{
    /// <summary>The fixed tape length, in bytes, shared by every executor.</summary>
    private const int MemorySize = 30_000;

    /// <summary>Signature of the generated method for the machine-based path.</summary>
    private delegate void Executor(IMachine machine);

    /// <summary>Signature of the generated method for the inlined-tape path.</summary>
    private delegate void InlineExecutor(IMachineIo io);

    private static readonly MethodInfo AddMethod = typeof(IMachine).GetMethod(nameof(IMachine.Add))!;
    private static readonly MethodInfo ShiftMethod = typeof(IMachine).GetMethod(nameof(IMachine.Shift))!;
    private static readonly MethodInfo WriteMethod = typeof(IMachine).GetMethod(nameof(IMachine.Write))!;
    private static readonly MethodInfo ReadMethod = typeof(IMachine).GetMethod(nameof(IMachine.Read))!;
    private static readonly MethodInfo SetMethod = typeof(IMachine).GetMethod(nameof(IMachine.Set))!;
    private static readonly MethodInfo MulMethod = typeof(IMachine).GetMethod(nameof(IMachine.Mul))!;
    private static readonly MethodInfo MulAndClearMethod = typeof(IMachine).GetMethod(nameof(IMachine.MulAndClear))!;
    private static readonly MethodInfo PointerScanMethod = typeof(IMachine).GetMethod(nameof(IMachine.PointerScan))!;
    private static readonly MethodInfo IsZeroMethod = typeof(IMachine).GetMethod(nameof(IMachine.IsZero))!;
    private static readonly MethodInfo IoWriteMethod = typeof(IMachineIo).GetMethod(nameof(IMachineIo.Write))!;
    private static readonly MethodInfo IoReadMethod = typeof(IMachineIo).GetMethod(nameof(IMachineIo.Read))!;

    /// <summary>
    /// Compiles <paramref name="opcodes"/> to IL that calls
    /// <paramref name="machine"/>, then immediately invokes it.
    /// </summary>
    /// <param name="machine">The machine the generated code drives.</param>
    /// <param name="opcodes">The parsed program to compile and run.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown while compiling if an op-code has an unknown
    /// <see cref="OpCodeType"/>.
    /// </exception>
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
    
    /// <summary>
    /// Executes <paramref name="opcodes"/> directly against a freshly allocated
    /// 30,000 byte tape, without materialising an <see cref="IMachine"/>.
    /// </summary>
    /// <remarks>
    /// The generated method keeps the tape in a local <see cref="byte"/>[] and the
    /// pointer in a local <see cref="int"/>, so every cell operation is a plain IL
    /// array access instead of an <see cref="IMachine"/> interface dispatch.
    /// </remarks>
    public void Execute(IMachineIo io, IReadOnlyList<OpCode> opcodes)
    {
        var executeMethod = new DynamicMethod(
            name: "Execute",
            returnType: typeof(void),
            parameterTypes: [typeof(IMachineIo)]
        );

        ILGenerator il = executeMethod.GetILGenerator();
        LocalBuilder memory = il.DeclareLocal(typeof(byte[]));
        LocalBuilder position = il.DeclareLocal(typeof(int));
        LocalBuilder destination = il.DeclareLocal(typeof(int));

        // byte[] memory = new byte[MemorySize];
        il.Emit(OpCodes.Ldc_I4, MemorySize);
        il.Emit(OpCodes.Newarr, typeof(byte));
        il.Emit(OpCodes.Stloc, memory);

        MakeInlineCode(il, memory, position, destination, opcodes);
        il.Emit(OpCodes.Ret);

        InlineExecutor compiledExecute = (InlineExecutor)executeMethod.CreateDelegate(typeof(InlineExecutor));
        compiledExecute(io);
    }

    /// <summary>
    /// Emits the machine-based body of the generated method: each op-code
    /// becomes a call to the matching <see cref="IMachine"/> method.
    /// </summary>
    /// <param name="il">The generator receiving the op-code instructions.</param>
    /// <param name="opcodes">The program (or loop body) to translate.</param>
    /// <remarks>
    /// Loops are emitted as a backwards branch guarded by
    /// <see cref="IMachine.IsZero"/>, matching the interpreter's
    /// <c>while (!IsZero())</c>. Loops with an empty body emit nothing.
    /// </remarks>
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
                case OpCodeType.Set:
                    AddOneParamMethod(il, opcode.Value, SetMethod);
                    break;
                case OpCodeType.Mul:
                    AddMulCall(il, opcode, MulMethod);
                    break;
                case OpCodeType.MulAndClear:
                    AddMulCall(il, opcode, MulAndClearMethod);
                    break;
                case OpCodeType.PointerScan:
                    AddOneParamMethod(il, opcode.Value, PointerScanMethod);
                    break;
                case OpCodeType.Halt:
                    var checkLabel = il.DefineLabel();
                    var retLabel = il.DefineLabel();
                    
                    il.Emit(OpCodes.Br, checkLabel);
                    il.MarkLabel(retLabel);
                    il.Emit(OpCodes.Ret);
                    
                    il.MarkLabel(checkLabel);
                    AddNoParamMethod(il, IsZeroMethod);
                    il.Emit(OpCodes.Brfalse, retLabel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opcodes), opcode.Type,
                        $"Unknown OpCode: {opcode.Type}");
            }
        }
    }

    /// <summary>
    /// Emits the body of the inlined executor. Every op-code is translated into
    /// direct IL against the local tape and pointer instead of a virtual call.
    /// </summary>
    private static void MakeInlineCode(
        ILGenerator il,
        LocalBuilder memory,
        LocalBuilder position,
        LocalBuilder destination,
        IReadOnlyList<OpCode> opcodes)
    {
        foreach (OpCode opcode in opcodes)
        {
            switch (opcode.Type)
            {
                case OpCodeType.Add:
                    EmitAdd(il, memory, position, opcode.Value);
                    break;
                case OpCodeType.Shift:
                    EmitShift(il, position, opcode.Value);
                    break;
                case OpCodeType.Write:
                    EmitWrite(il, memory, position);
                    break;
                case OpCodeType.Read:
                    EmitRead(il, memory, position);
                    break;
                case OpCodeType.Loop:
                    if (opcode.OpCodes is null || opcode.OpCodes.Count == 0)
                        break;
                    var loopHeadLabel = il.DefineLabel();
                    var loopDoneLabel = il.DefineLabel();

                    // while (memory[position] != 0) { ... }
                    il.MarkLabel(loopHeadLabel);
                    EmitLoadCell(il, memory, position);
                    il.Emit(OpCodes.Brfalse, loopDoneLabel);

                    MakeInlineCode(il, memory, position, destination, opcode.OpCodes);

                    il.Emit(OpCodes.Br, loopHeadLabel);
                    il.MarkLabel(loopDoneLabel);
                    break;
                case OpCodeType.Set:
                    EmitSet(il, memory, position, opcode.Value);
                    break;
                case OpCodeType.Mul:
                    EmitMul(il, memory, position, destination, opcode.Buffer, opcode.Value);
                    EmitShift(il, position, opcode.Offset);
                    break;
                case OpCodeType.MulAndClear:
                    EmitMul(il, memory, position, destination, opcode.Buffer, opcode.Value);
                    EmitSet(il, memory, position, 0);
                    EmitShift(il, position, opcode.Offset);
                    break;
                case OpCodeType.PointerScan:
                    EmitPointerScan(il, memory, position, opcode.Value);
                    break;
                case OpCodeType.Halt:
                    var haltLabel = il.DefineLabel();
                    EmitLoadCell(il, memory, position);
                    il.Emit(OpCodes.Brfalse, haltLabel);
                    il.Emit(OpCodes.Ret);
                    il.MarkLabel(haltLabel);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(opcodes), opcode.Type,
                        $"Unknown OpCode: {opcode.Type}");
            }
        }
    }

    /// <summary>Pushes <c>memory[position]</c> (zero-extended to an <see cref="int"/>).</summary>
    private static void EmitLoadCell(ILGenerator il, LocalBuilder memory, LocalBuilder position)
    {
        il.Emit(OpCodes.Ldloc, memory);
        il.Emit(OpCodes.Ldloc, position);
        il.Emit(OpCodes.Ldelem_U1);
    }

    /// <summary>Emits <c>memory[position] = (byte)(memory[position] + value)</c>.</summary>
    private static void EmitAdd(ILGenerator il, LocalBuilder memory, LocalBuilder position, int value)
    {
        // A multiple of 256 leaves the cell unchanged.
        if ((value & 0xFF) == 0)
        {
            return;
        }

        il.Emit(OpCodes.Ldloc, memory);
        il.Emit(OpCodes.Ldloc, position);
        EmitLoadCell(il, memory, position);
        il.Emit(OpCodes.Ldc_I4, value);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Conv_U1);
        il.Emit(OpCodes.Stelem_I1);
    }

    /// <summary>Emits <c>memory[position] = (byte)value</c>.</summary>
    private static void EmitSet(ILGenerator il, LocalBuilder memory, LocalBuilder position, int value)
    {
        il.Emit(OpCodes.Ldloc, memory);
        il.Emit(OpCodes.Ldloc, position);
        if ((value & 0xFF) == 0)
        {
            il.Emit(OpCodes.Ldc_I4_0);
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4, value);
            il.Emit(OpCodes.Conv_U1);
        }
        il.Emit(OpCodes.Stelem_I1);
    }

    /// <summary>
    /// Emits <c>position = (position + value) mod MemorySize</c>.
    /// </summary>
    /// <remarks>
    /// <paramref name="value"/> is reduced modulo the tape size at generation
    /// time. Because the pointer is always in <c>[0, MemorySize)</c> and the
    /// reduced offset is in <c>(-MemorySize, MemorySize)</c>, at most one
    /// conditional correction is required -- cheaper than two <c>rem</c>s.
    /// </remarks>
    private static void EmitShift(ILGenerator il, LocalBuilder position, int value)
    {
        var offset = value % MemorySize;
        if (offset == 0)
        {
            return;
        }

        il.Emit(OpCodes.Ldloc, position);
        il.Emit(OpCodes.Ldc_I4, offset);
        il.Emit(OpCodes.Add);

        var doneLabel = il.DefineLabel();
        il.Emit(OpCodes.Dup);
        if (offset > 0)
        {
            il.Emit(OpCodes.Ldc_I4, MemorySize);
            il.Emit(OpCodes.Blt, doneLabel);
            il.Emit(OpCodes.Ldc_I4, MemorySize);
            il.Emit(OpCodes.Sub);
        }
        else
        {
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Bge, doneLabel);
            il.Emit(OpCodes.Ldc_I4, MemorySize);
            il.Emit(OpCodes.Add);
        }

        il.MarkLabel(doneLabel);
        il.Emit(OpCodes.Stloc, position);
    }

    /// <summary>
    /// Emits the wrapped destination index for a multiply: 
    /// <c>destination = (position + buffer) mod MemorySize</c>.
    /// </summary>
    private static void EmitDestination(
        ILGenerator il,
        LocalBuilder position,
        LocalBuilder destination,
        int buffer)
    {
        var offset = ((buffer % MemorySize) + MemorySize) % MemorySize;
        if (offset == 0)
        {
            il.Emit(OpCodes.Ldloc, position);
            il.Emit(OpCodes.Stloc, destination);
            return;
        }

        il.Emit(OpCodes.Ldloc, position);
        il.Emit(OpCodes.Ldc_I4, offset);
        il.Emit(OpCodes.Add);

        var doneLabel = il.DefineLabel();
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldc_I4, MemorySize);
        il.Emit(OpCodes.Blt, doneLabel);
        il.Emit(OpCodes.Ldc_I4, MemorySize);
        il.Emit(OpCodes.Sub);

        il.MarkLabel(doneLabel);
        il.Emit(OpCodes.Stloc, destination);
    }

    /// <summary>
    /// Emits the shared <c>Mul</c> family body:
    /// <c>memory[destination] = (byte)(memory[destination] + value * memory[position])</c>.
    /// </summary>
    private static void EmitMul(
        ILGenerator il,
        LocalBuilder memory,
        LocalBuilder position,
        LocalBuilder destination,
        int buffer,
        int value)
    {
        EmitDestination(il, position, destination, buffer);

        il.Emit(OpCodes.Ldloc, memory);
        il.Emit(OpCodes.Ldloc, destination);
        il.Emit(OpCodes.Ldloc, memory);
        il.Emit(OpCodes.Ldloc, destination);
        il.Emit(OpCodes.Ldelem_U1);
        il.Emit(OpCodes.Ldc_I4, value);
        EmitLoadCell(il, memory, position);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Conv_U1);
        il.Emit(OpCodes.Stelem_I1);
    }

    /// <summary>Emits <c>while (memory[position] != 0) position = (position + direction) mod MemorySize;</c>.</summary>
    private static void EmitPointerScan(ILGenerator il, LocalBuilder memory, LocalBuilder position, int direction)
    {
        var headLabel = il.DefineLabel();
        var doneLabel = il.DefineLabel();

        il.MarkLabel(headLabel);
        EmitLoadCell(il, memory, position);
        il.Emit(OpCodes.Brfalse, doneLabel);
        EmitShift(il, position, direction);
        il.Emit(OpCodes.Br, headLabel);
        il.MarkLabel(doneLabel);
    }

    /// <summary>Emits <c>io.Write(memory[position]);</c>.</summary>
    private static void EmitWrite(ILGenerator il, LocalBuilder memory, LocalBuilder position)
    {
        il.Emit(OpCodes.Ldarg_0);
        EmitLoadCell(il, memory, position);
        il.Emit(OpCodes.Callvirt, IoWriteMethod);
    }

    /// <summary>Emits <c>memory[position] = io.Read();</c>.</summary>
    private static void EmitRead(ILGenerator il, LocalBuilder memory, LocalBuilder position)
    {
        il.Emit(OpCodes.Ldloc, memory);
        il.Emit(OpCodes.Ldloc, position);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Callvirt, IoReadMethod);
        il.Emit(OpCodes.Stelem_I1);
    }

    /// <summary>
    /// Emits a call to a method that takes a single <see cref="int"/> argument,
    /// loading the machine from argument 0 first.
    /// </summary>
    /// <param name="il">The generator receiving the instructions.</param>
    /// <param name="value">The integer argument to pass.</param>
    /// <param name="method">The one-parameter method to call virtually.</param>
    private static void AddOneParamMethod(ILGenerator il, int value, MethodInfo method)
    {
        il.Emit(OpCodes.Ldarg_0); // Load 'machine' (Arg 0)
        il.Emit(OpCodes.Ldc_I4, value); // Load integer
        il.Emit(OpCodes.Callvirt, method); // Call method
    }

    /// <summary>
    /// Emits a call to a parameterless method, loading the machine from
    /// argument 0 first.
    /// </summary>
    /// <param name="il">The generator receiving the instructions.</param>
    /// <param name="method">The parameterless method to call virtually.</param>
    private static void AddNoParamMethod(ILGenerator il, MethodInfo method)
    {
        il.Emit(OpCodes.Ldarg_0); // Load 'machine' (Arg 0)
        il.Emit(OpCodes.Callvirt, method); // Call method
    }

    /// <summary>
    /// Emits the machine-based form of a <c>Mul</c>-family op-code: a call with
    /// the value and buffer, followed by the optional offset shift.
    /// </summary>
    /// <param name="il">The generator receiving the instructions.</param>
    /// <param name="opcode">The multiply op-code being translated.</param>
    /// <param name="method">The matching <see cref="IMachine"/> multiply method.</param>
    private static void AddMulCall(ILGenerator il, OpCode opcode, MethodInfo method)
    {
        il.Emit(OpCodes.Ldarg_0); // Load 'machine' (Arg 0)
        il.Emit(OpCodes.Ldc_I4, opcode.Value); // Multiplier
        il.Emit(OpCodes.Ldc_I4, opcode.Buffer); // Destination cell offset
        il.Emit(OpCodes.Callvirt, method); // Call method
        if (opcode.Offset != 0)
        {
            AddOneParamMethod(il, opcode.Offset, ShiftMethod);
        }
    }
}
