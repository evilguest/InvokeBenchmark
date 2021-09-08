using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InvokeBenchmark
{


    public interface IBar
    {
        void Foo(ref ulong value);
    }

    public class Bar : IBar
    {

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Foo(ref ulong value) => value++;
    }

    public static class Helper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Bar GetIBar() => new Bar();

        public static MethodInfo GetFooMethod(IBar bar) => bar.GetType().GetMethod("Foo")!;

        public static IntPtr GetFooAddress(IBar bar) => GetFooMethod(bar).MethodHandle.GetFunctionPointer();
    }

    public abstract class MethodWrapper
    {
        public const string LibraryPath = "FooLibrary.dll";
        public abstract void Foo(ref ulong value);
    }

    public sealed class Baseline : MethodWrapper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Foo(ref ulong value) => value++;
    }

    public sealed class WrapperOverInterface : MethodWrapper
    {
        private readonly IBar _bar;

        public WrapperOverInterface(IBar bar) => _bar = bar;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Foo(ref ulong value) => _bar.Foo(ref value);
    }

    public sealed class WrapperOverDelegate : MethodWrapper
    {

        private readonly FooDelegate _fn;

        public WrapperOverDelegate(IBar bar) => _fn = ((Bar)bar).Foo;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Foo(ref ulong value) => _fn(ref value);

        private delegate void FooDelegate(ref ulong value);
    }

    public sealed unsafe class WrapperOverPtrFromDelegate : MethodWrapper
    {

        private readonly FooDelegate _delegate; // prevent GC cleaning
        private readonly delegate* unmanaged[Cdecl]<ref ulong, void> _fn;

        public WrapperOverPtrFromDelegate(Bar bar)
        {
            _delegate = bar.Foo;
            _fn = (delegate* unmanaged[Cdecl]<ref ulong, void>)Marshal.GetFunctionPointerForDelegate(_delegate);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Foo(ref ulong value) => _fn(ref value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FooDelegate(ref ulong value);
    }

    public sealed class WrapperOverDelegateFromMethodInfo : MethodWrapper
    {

        private readonly FooDelegate _fn;

        public WrapperOverDelegateFromMethodInfo(IBar bar)
            => _fn = (FooDelegate)Delegate.CreateDelegate(typeof(FooDelegate), bar, Helper.GetFooMethod(bar));

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Foo(ref ulong value) => _fn(ref value);

        private delegate void FooDelegate(ref ulong value);
    }

    public sealed class WrapperOverDelegateFromPtr : MethodWrapper
    {
        private readonly IBar _bar;
        private readonly FooDelegateFromPtr _fn;

        public WrapperOverDelegateFromPtr(IBar bar)
        {
            _bar = bar;
            _fn = Marshal.GetDelegateForFunctionPointer<FooDelegateFromPtr>(Helper.GetFooAddress(bar));
        }

        public override void Foo(ref ulong value) => _fn(_bar, ref value);

        private delegate void FooDelegateFromPtr(IBar @this, ref ulong value);
    }

    public sealed unsafe class WrapperOverManagedFunPtr : MethodWrapper
    {
        private readonly IBar _bar;
        private readonly delegate* managed<IBar, ref ulong, void> _fn;

        public WrapperOverManagedFunPtr(IBar bar)
        {
            _bar = bar;
            _fn = (delegate* managed<IBar, ref ulong, void>)Helper.GetFooAddress(bar);
        }

        public override void Foo(ref ulong value) => _fn(_bar, ref value);
    }

    public sealed unsafe class WrapperOverUnmanagedFunPtr : MethodWrapper
    {
        private readonly IBar _bar;
        private readonly delegate* unmanaged<IBar, ref ulong, void> _fn;

        public WrapperOverUnmanagedFunPtr(IBar bar)
        {
            _bar = bar;
            _fn = (delegate* unmanaged<IBar, ref ulong, void>)Helper.GetFooAddress(bar);
        }

        public override void Foo(ref ulong value) => _fn(_bar, ref value);
    }

    public sealed class WrapperOverDllImport : MethodWrapper
    {
        [DllImport(LibraryPath)]
        private static extern void FooNative(ref ulong value);

        public override void Foo(ref ulong value) => FooNative(ref value);
    }

    public sealed class WrapperOverNoGcTransitionDllImport : MethodWrapper
    {
        [DllImport(LibraryPath, CallingConvention = CallingConvention.Cdecl)]
        [SuppressGCTransition]
        private static extern void FooNative(ref ulong value);

        public override void Foo(ref ulong value) => FooNative(ref value);
    }


    public sealed unsafe class WrapperOverDllGetProcAddr : MethodWrapper
    {
        private readonly delegate* unmanaged[Cdecl]<ref ulong, void> _fn;

        public WrapperOverDllGetProcAddr()
        {
            var hModule = NativeLibrary.Load(LibraryPath);
            _fn = (delegate* unmanaged[Cdecl]<ref ulong, void>)NativeLibrary.GetExport(hModule, "FooNative");
        }

        public override void Foo(ref ulong value)
        {
            _fn(ref value);
        }
    }

}