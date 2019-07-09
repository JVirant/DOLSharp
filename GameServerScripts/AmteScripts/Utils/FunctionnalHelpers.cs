using System;

namespace AmteScripts.Utils
{
    public static class FunctionnalHelpers
    {
        private delegate Action<T> RecursiveAct<T>(RecursiveAct<T> r);
        private delegate Action<T1, T2> RecursiveAct<T1, T2>(RecursiveAct<T1, T2> r);
        private delegate Func<T, R> Recursive<T, R>(Recursive<T, R> r);
        private delegate Func<T1, T2, R> Recursive<T1, T2, R>(Recursive<T1, T2, R> r);

        public static Action<T> Y<T>(Func<Action<T>, Action<T>> f)
        {
            RecursiveAct<T> rec = r => t => f(r(r))(t);
            return rec(rec);
        }

        public static Action<T1, T2> Y<T1, T2>(Func<Action<T1, T2>, Action<T1, T2>> f)
        {
            RecursiveAct<T1, T2> rec = r => (t1, t2) => f(r(r))(t1, t2);
            return rec(rec);
        }

        public static Func<T1, TResult> Y<T1, TResult>(Func<Func<T1, TResult>, Func<T1, TResult>> f)
        {
            Recursive<T1, TResult> rec = r => t1 => f(r(r))(t1);
            return rec(rec);
        }

        public static Func<T1, T2, TResult> Y<T1, T2, TResult>(Func<Func<T1, T2, TResult>, Func<T1, T2, TResult>> f)
        {
            Recursive<T1, T2, TResult> rec = r => (t1, t2) => f(r(r))(t1, t2);
            return rec(rec);
        }
    }
}
