//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace DOL.GS.Scripts
//{
//    [Serializable]
//    public struct WeakReference<T> where T : class
//    {
//        private readonly System.WeakReference wrapped;
//        public WeakReference(T target)
//        {
//            wrapped = new System.WeakReference(target);
//        }
//        public WeakReference(T target, bool trackResurrection)
//        {
//            wrapped = new System.WeakReference(target, trackResurrection);
//        }

//        public bool IsAlive
//        {
//            get
//            {
//                return wrapped.IsAlive;
//            }
//        }

//        public T Target
//        {
//            get
//            {
//                return wrapped.Target as T;
//            }
//            set
//            {
//                wrapped.Target = value;
//            }
//        }

//        public bool TrackResurrection
//        {
//            get
//            {
//                return wrapped.TrackResurrection;
//            }
//        }
//    }
//}
