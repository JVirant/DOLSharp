using System.Collections.Generic;
using System.Numerics;

namespace DOL.GS
{
	public class GamePath
	{
		public enum MarkerModel
		{
			Brown,
			Green,
			Red,
			Blue,
			Yellow,
		}

		public string Name;
		public Region Region;
		public bool HasLavaEffect = false;
		public List<(GameLocation point, short speed, MarkerModel model)> Points = new List<(GameLocation point, short speed, MarkerModel model)>();

		public List<GameStaticItem> DebugObjs = new List<GameStaticItem>();

		public GamePath(string name, Region region)
		{
			Name = name;
			Region = region;
		}

		public void Append(GameLocation point, short speed, MarkerModel model = MarkerModel.Brown)
		{
			Points.Add((point, speed, model));
		}

		public void Show()
		{
			Hide();

			foreach (var (pt, speed, model) in Points)
			{
				//Create a new object
				var obj = new GameStaticItem();
				obj.Position = pt.Position + Vector3.UnitZ;
				obj.CurrentRegion = Region;
				obj.Heading = pt.Heading;
				obj.Name = $"{pt.Name}--{speed} spd";
				obj.Model = 2965;
				switch (model)
				{
					case MarkerModel.Green: obj.Model = 2967; break;
					case MarkerModel.Red: obj.Model = 2969; break;
					case MarkerModel.Blue: obj.Model = 2961; break;
					case MarkerModel.Yellow: obj.Model = 2963; break;
				}
				obj.Emblem = 0;
				obj.AddToWorld();
				DebugObjs.Add(obj);
			}
		}
		public void Hide()
		{
			foreach (var obj in DebugObjs)
				obj.Delete();
			DebugObjs.Clear();
		}
	}
}
