using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Engines.Quests;

namespace Server.Engines.Quests.Haven
{
	public class SchmendrickApprenticeCorpse : Corpse
	{
		private static Mobile GetOwner()
		{
			Mobile apprentice = new Mobile();

			apprentice.Hue = Utility.RandomSkinHue();
			apprentice.Female = false;
			apprentice.Body = 0x190;
			apprentice.Name = NameList.RandomName( "male" );

			apprentice.Delete();

			return apprentice;
		}

		private static List<Item> GetEquipment()
		{
			List<Item> list = new List<Item>();

			list.Add( new Robe( QuestSystem.RandomBrightHue() ) );
			list.Add( new WizardsHat( Utility.RandomNeutralHue() ) );
			list.Add( new Shoes( Utility.RandomNeutralHue() ) );

			list.Add( new Spellbook() );

			return list;
		}

		private static int m_HairHue;
		private static HairInfo GetHair()
		{
			m_HairHue = Race.Human.RandomHairHue();

			return new HairInfo( Race.Human.RandomHair( false ), m_HairHue );
		}

		private static FacialHairInfo GetFacialHair()
		{
			m_HairHue = Race.Human.RandomHairHue();

			return new FacialHairInfo( Race.Human.RandomFacialHair( false ), m_HairHue );
		}

		private Lantern m_Lantern;

		[Constructable]
		public SchmendrickApprenticeCorpse()
			: base( GetOwner(), GetHair(), GetFacialHair(), GetEquipment() )
		{
			Direction = Direction.West;

			foreach ( Item item in EquipItems )
				DropItem( item );

			m_Lantern = new Lantern();
			m_Lantern.Movable = false;
			m_Lantern.Protected = true;
			m_Lantern.Ignite();
		}

		public SchmendrickApprenticeCorpse( Serial serial )
			: base( serial )
		{
		}

		public override LocalizedText GetNameProperty()
		{
			return new LocalizedText( "a human corpse" );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( ItemID == 0x2006 ) // Corpse form
				list.Add( 1049144, this.Name ); // the remains of ~1_NAME~ the apprentice
			else
				list.Add( 1049145 ); // the remains of a wizard's apprentice
		}

		public override void Open( Mobile from, bool checkSelfLoot )
		{
			if ( !from.InRange( this.GetWorldLocation(), 2 ) )
				return;

			PlayerMobile player = from as PlayerMobile;

			if ( player != null )
			{
				QuestSystem qs = player.Quest;

				if ( qs is UzeraanTurmoilQuest )
				{
					QuestObjective obj = qs.FindObjective( typeof( FindApprenticeObjective ) );

					if ( obj != null && !obj.Completed )
					{
						Item scroll = new SchmendrickScrollOfPower();

						if ( player.PlaceInBackpack( scroll ) )
						{
							player.SendLocalizedMessage( 1049147, "", 0x22 ); // You find the scroll and put it in your pack.
							obj.Complete();
						}
						else
						{
							player.SendLocalizedMessage( 1049146, "", 0x22 ); // You find the scroll, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
							scroll.Delete();
						}

						return;
					}
				}
			}

			from.SendLocalizedMessage( 1049143, "", 0x22 ); // This is the corpse of a wizard's apprentice.  You can't bring yourself to search it without a good reason.
		}

		public override void OnLocationChange( Point3D oldLoc )
		{
			if ( m_Lantern != null && !m_Lantern.Deleted )
				m_Lantern.Location = new Point3D( this.X, this.Y + 1, this.Z );
		}

		public override void OnMapChange()
		{
			if ( m_Lantern != null && !m_Lantern.Deleted )
				m_Lantern.Map = this.Map;
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();

			if ( m_Lantern != null && !m_Lantern.Deleted )
				m_Lantern.Delete();
		}

		public override void Serialize( GenericWriter writer )
		{
			if ( m_Lantern != null && m_Lantern.Deleted )
				m_Lantern = null;

			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( m_Lantern );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			/*int version = */
			reader.ReadInt();

			m_Lantern = (Lantern) reader.ReadItem();
		}
	}
}