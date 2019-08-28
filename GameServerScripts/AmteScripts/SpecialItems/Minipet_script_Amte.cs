// Original code by Dinberg modifié par Norec pour Amtenael seulement

using System;
using System.Collections.Generic;
using System.Text;
using DOL.GS;
using DOL.GS.Spells;
using DOL.AI.Brain;

//Edit Norec
namespace DOL.GS.Scripts
{
	[SpellHandler("SummonNoveltyPetAmte")]
	public class SummonNoveltyPetAmte : SummonSpellHandler
	{
		/// <summary>
		/// Constructs the spell handler
		/// </summary>
		//Edit Norec
		public SummonNoveltyPetAmte(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line) { }

		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			base.ApplyEffectOnTarget(target, effectiveness);

			if (m_pet != null)
			{
				m_pet.Flags |= GameNPC.eFlags.PEACE; //must be peace!

				//No brain for now, so just follow owner.
				m_pet.Follow(Caster, 100, WorldMgr.VISIBILITY_DISTANCE);

				Caster.TempProperties.setProperty(NoveltyPetBrain.HAS_PET, true);
			}

		}

		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			if (Caster.CurrentRegion.IsRvR) //Edit des régions interdite - Edit Norec
			{
				MessageToCaster("You cannot cast this spell here!", DOL.GS.PacketHandler.eChatType.CT_SpellResisted);
				return false;
			}

			if (Caster.TempProperties.getProperty<bool>(NoveltyPetBrain.HAS_PET, false))
			{
				// no message
				return false;
			}

			return base.CheckBeginCast(selectedTarget);
		}

		/// <summary>
		/// These pets aren't controllable!
		/// </summary>
		/// <param name="brain"></param>
		protected override void SetBrainToOwner(IControlledBrain brain)
		{
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new NoveltyPetBrain(owner as GamePlayer);
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(string.Format("  {0}", Spell.Description));

				return list;
			}
		}
	}
}
