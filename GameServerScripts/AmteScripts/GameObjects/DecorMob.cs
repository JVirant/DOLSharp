using DOL.AI.Brain;


namespace DOL.GS.Scripts
{
    public class DecorMob : AmteMob
    {
    	public string WeaponTemplate { get; set; }

    	public override void AddAttacker(GameObject attacker) { }
        public override void StartAttack(GameObject attackTarget) { }
        public override bool IsWorthReward
        {
            get { return true; }
            set { }
        }

        public DecorMob()
        {
        	WeaponTemplate = "";
        	m_respawnInterval = 10000;
        }

        public override int GetModified(eProperty property)
        {
            if (WeaponTemplate != "")
            {
                switch (property)
                {
                    case eProperty.Resist_Cold:
                    case eProperty.Resist_Energy:
                    case eProperty.Resist_Heat:
                    case eProperty.Resist_Matter:
                    case eProperty.Resist_Spirit:
                    case eProperty.Resist_Body:
                        return 100;
                }
            }
            return base.GetModified(property);
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;
            if (RoamingRange < 0)
                SetOwnBrain(new BlankBrain());
            return true;
        }

		public override AmteCustomParam GetCustomParam()
		{
			return new AmteCustomParam("WeaponTemplate", () => WeaponTemplate, v => WeaponTemplate = v);
		}
    }
}
