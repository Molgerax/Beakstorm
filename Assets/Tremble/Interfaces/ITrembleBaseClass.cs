//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace TinyGoose.Tremble
{
	public interface ITrembleBaseClass
	{

	}

	public static class TrembleBaseClassDefault
	{
		public static ITrembleBaseClass CreateDefault(Type baseClass)
		{
			AssemblyName assName = new("Tremble.Editor.BaseClasses");
			AssemblyBuilder assBuilder = AssemblyBuilder.DefineDynamicAssembly(assName, AssemblyBuilderAccess.Run);
			ModuleBuilder moduleBuilder = assBuilder.DefineDynamicModule(assName + ".dll");
			TypeBuilder typeBuilder = moduleBuilder.DefineType(baseClass.Name + "Test");
			typeBuilder.AddInterfaceImplementation(baseClass);

			MethodBuilder importMethod = typeBuilder.DefineMethod(nameof(IOnImportFromMapEntity.OnImportFromMapEntity),
				MethodAttributes.Public | MethodAttributes.Virtual,
				CallingConventions.HasThis,
				null,
				new[] { typeof(MapBsp), typeof(BspEntity) });

			ILGenerator ilGenerator = importMethod.GetILGenerator();
			ilGenerator.Emit(OpCodes.Nop);
			ilGenerator.Emit(OpCodes.Ret);

			Type type = typeBuilder.CreateType();
			return (ITrembleBaseClass)Activator.CreateInstance(type);
		}
	}
}