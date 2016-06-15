using System;
using CPUx86 = Cosmos.Assembler.x86;
using CPU = Cosmos.Assembler.x86;
using Cosmos.Assembler.x86;
using Cosmos.Assembler;
using Cosmos.Assembler.x86.SSE;
using XSharp.Compiler;

namespace Cosmos.IL2CPU.X86.IL
{
	[Cosmos.IL2CPU.OpCode( ILOpCode.Code.Cgt )]
	public class Cgt : ILOp
	{
		public Cgt( Cosmos.Assembler.Assembler aAsmblr )
			: base( aAsmblr )
		{
		}

		public override void Execute( MethodInfo aMethod, ILOpCode aOpCode )
		{
            var xStackItem = aOpCode.StackPopTypes[0];
            var xStackItemSize = SizeOfType(xStackItem);
            var xStackItemIsFloat = TypeIsFloat(xStackItem);
			if( xStackItemSize > 8 )
			{
				//EmitNotImplementedException( Assembler, GetServiceProvider(), "Cgt: StackSizes>8 not supported", CurInstructionLabel, mMethodInfo, mCurrentOffset, NextInstructionLabel );
				throw new NotImplementedException("Cosmos.IL2CPU.x86->IL->Cgt.cs->Error: StackSizes > 8 not supported");
				//return;
			}
			string BaseLabel = GetLabel( aMethod, aOpCode ) + ".";
			string LabelTrue = BaseLabel + "True";
			string LabelFalse = BaseLabel + "False";
			var xNextLabel = GetLabel(aMethod, aOpCode.NextPosition);
			if( xStackItemSize > 4 )
			{
				new CPUx86.Mov { DestinationReg = CPUx86.RegistersEnum.ESI, SourceValue = 1 };
				// esi = 1
				XS.Xor(XSRegisters.OldToNewRegister(RegistersEnum.EDI), XSRegisters.OldToNewRegister(RegistersEnum.EDI));
				// edi = 0
				if (xStackItemIsFloat)
				{
					// value 1
					new CPUx86.x87.FloatLoad { DestinationReg = RegistersEnum.ESP, Size = 64, DestinationDisplacement = 8, DestinationIsIndirect = true };
					// value 2
					new CPUx86.x87.FloatLoad { DestinationReg = RegistersEnum.ESP, Size = 64, DestinationIsIndirect = true };
					XS.FPU.FloatCompareAndSet(XSRegisters.ST1);
					// if carry is set, ST(0) < ST(i)
					new ConditionalMove { Condition = ConditionalTestEnum.Below, DestinationReg = RegistersEnum.EDI, SourceReg = RegistersEnum.ESI };
					// pops fpu stack
					XS.FPU.FloatStoreAndPop(XSRegisters.ST0);
					XS.FPU.FloatStoreAndPop(XSRegisters.ST0);
					new CPUx86.Add { DestinationReg = RegistersEnum.ESP, SourceValue = 16 };
				}
				else
				{
					XS.Pop(XSRegisters.OldToNewRegister(RegistersEnum.EAX));
					XS.Pop(XSRegisters.OldToNewRegister(RegistersEnum.EDX));
					//value2: EDX:EAX
					XS.Pop(XSRegisters.OldToNewRegister(RegistersEnum.EBX));
					XS.Pop(XSRegisters.OldToNewRegister(RegistersEnum.ECX));
					//value1: ECX:EBX
					XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.ECX), XSRegisters.OldToNewRegister(RegistersEnum.EDX));
					new ConditionalJump { Condition = ConditionalTestEnum.GreaterThan, DestinationLabel = LabelTrue };
					new ConditionalJump { Condition = ConditionalTestEnum.LessThan, DestinationLabel = LabelFalse };
					XS.Compare(XSRegisters.OldToNewRegister(RegistersEnum.EBX), XSRegisters.OldToNewRegister(RegistersEnum.EAX));
					new Label(LabelTrue);
					new ConditionalMove { Condition = ConditionalTestEnum.GreaterThan, DestinationReg = RegistersEnum.EDI, SourceReg = RegistersEnum.ESI };
					new Label(LabelFalse);
				}
				XS.Push(XSRegisters.OldToNewRegister(RegistersEnum.EDI));
				/*
				new CPUx86.ConditionalJump { Condition = CPUx86.ConditionalTestEnum.GreaterThan, DestinationLabel = LabelTrue };
				new Label(LabelFalse);
				new CPUx86.Push { DestinationValue = 0 };
				new CPUx86.Jump { DestinationLabel = xNextLabel };
				new Label( LabelTrue );
				new CPUx86.Push { DestinationValue = 1 };*/
			}
			else
			{
				if (xStackItemIsFloat){
					new MoveSS { DestinationReg = RegistersEnum.XMM0, SourceReg = RegistersEnum.ESP, SourceIsIndirect = true };
					new CPUx86.Add { DestinationReg = RegistersEnum.ESP, SourceValue = 4 };
					new MoveSS { DestinationReg = RegistersEnum.XMM1, SourceReg = RegistersEnum.ESP, SourceIsIndirect = true };
					new CompareSS { DestinationReg = RegistersEnum.XMM1, SourceReg = RegistersEnum.XMM0, pseudoOpcode = (byte)ComparePseudoOpcodes.NotLessThanOrEqualTo };
					new MoveD { DestinationReg = RegistersEnum.EBX, SourceReg = RegistersEnum.XMM1 };
					new CPUx86.And { DestinationReg = RegistersEnum.EBX, SourceValue = 1 };
					new Mov { SourceReg = RegistersEnum.EBX, DestinationReg = RegistersEnum.ESP, DestinationIsIndirect = true };
				}

				else{
					XS.Pop(XSRegisters.OldToNewRegister(RegistersEnum.EAX));
					new CPUx86.Compare { DestinationReg = CPUx86.RegistersEnum.EAX, SourceReg = CPUx86.RegistersEnum.ESP, SourceIsIndirect = true };
					new CPUx86.ConditionalJump { Condition = CPUx86.ConditionalTestEnum.LessThan, DestinationLabel = LabelTrue };
					new CPUx86.Jump { DestinationLabel = LabelFalse };
					new Label( LabelTrue );
					new CPUx86.Add { DestinationReg = CPUx86.RegistersEnum.ESP, SourceValue = 4 };
					new CPUx86.Push { DestinationValue = 1 };
					new CPUx86.Jump { DestinationLabel = xNextLabel };
					new Label( LabelFalse );
					new CPUx86.Add { DestinationReg = CPUx86.RegistersEnum.ESP, SourceValue = 4 };
					new CPUx86.Push { DestinationValue = 0 };
				}
			}
		}
	}
}
