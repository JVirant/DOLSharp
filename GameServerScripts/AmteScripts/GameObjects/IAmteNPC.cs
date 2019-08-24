using System;
using System.Collections.Generic;

namespace DOL.GS.Scripts
{
	public class AmteCustomParam
	{
		public string name;
		public string defaultValue;
		public AmteCustomParam next;
		public string Value { get { return _getValue(); } set { _setValue(value); } }

		private readonly Func<string> _getValue;
		private readonly Action<string> _setValue;

		public AmteCustomParam(string name, Func<string> get, Action<string> set, string defaultValue = "")
		{
			this.name = name;
			_getValue = get;
			_setValue = set;
			this.defaultValue = defaultValue;
		}
	}

    public interface IAmteNPC
    {
    	AmteCustomParam GetCustomParam();
        IList<string> DelveInfo();
    }
}
