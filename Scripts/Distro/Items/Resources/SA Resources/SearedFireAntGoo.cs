using System;
using Server;

namespace Server.Items
{
	public class SearedFireAntGoo : Item
	{
		public override int LabelNumber { get { return 1112902; } } // Seared Fire Ant Goo

		[Constructable]
		public SearedFireAntGoo()
			: this( 1 )
		{
		}

		[Constructable]
		public SearedFireAntGoo( int amount )
			: base( 0x122E )
		{
			Weight = 1.0;
			Amount = amount;
			Hue = 0x30;
		}

		public SearedFireAntGoo( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			/*int version = */
			reader.ReadInt();
		}
	}
}