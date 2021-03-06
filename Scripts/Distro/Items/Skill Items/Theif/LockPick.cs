using System;
using System.Collections;
using System.Linq;
using Server.Network;
using Server.Targeting;
using Server.Items;

namespace Server.Items
{
	public interface ILockpickable : IPoint2D
	{
		int LockLevel { get; set; }
		bool Locked { get; set; }
		Mobile Locker { get; set; }
		Mobile Picker { get; set; }
		int MaxLockLevel { get; set; }
		int RequiredSkill { get; set; }

		bool CheckAccess( Mobile from );
		int LockPick( Mobile from );
		int FailLockPick( Mobile from );
	}

	public static class LockpickableExtensions
	{
		public static bool IsLockedAndTrappedByPlayer( this ILockpickable item )
		{
			bool isTrapEnabled = item is TrapableContainer && ( (TrapableContainer) item ).TrapEnabled;
			bool isLockedByPlayer = item.Locker != null && item.Locker.IsPlayer;

			return isLockedByPlayer && isTrapEnabled;
		}
	}

	[FlipableAttribute( 0x14fc, 0x14fb )]
	public class Lockpick : Item
	{
		[Constructable]
		public Lockpick()
			: this( 1 )
		{
		}

		[Constructable]
		public Lockpick( int amount )
			: base( 0x14FC )
		{
			Stackable = true;
			Amount = amount;
			Weight = 0.1;
		}

		public Lockpick( Serial serial )
			: base( serial )
		{
		}

		public virtual bool CheckSuccess( Mobile from, ILockpickable target )
		{
			return from.CheckTargetSkill( SkillName.Lockpicking, target, target.LockLevel, target.MaxLockLevel );
		}

		public virtual void OnSuccess( Mobile from, ILockpickable target )
		{
			from.PlaySound( 0x4A );
		}

		public virtual void OnFailure( Mobile from, ILockpickable target )
		{
			// When failed, a 25% chance to break the lockpick
			if ( Utility.Random( 4 ) == 0 )
			{
				( (Item) target ).SendLocalizedMessageTo( from, 502074 ); // You broke the lockpick.

				from.PlaySound( 0x3A4 );
				Consume();
			}
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

		public override void OnDoubleClick( Mobile from )
		{
			from.SendLocalizedMessage( 502068 ); // What do you want to pick?
			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private static Hashtable m_Table = new Hashtable();

			private Lockpick m_Item;

			public InternalTarget( Lockpick item )
				: base( 1, false, TargetFlags.None )
			{
				m_Item = item;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Item.Deleted )
					return;

				if ( targeted is ILockpickable )
				{
					Item item = (Item) targeted;
					from.Direction = from.GetDirectionTo( item );

					if ( ( (ILockpickable) targeted ).Locked )
					{
						if ( m_Table[from] != null )
						{
							from.SendLocalizedMessage( 500119 ); // You must wait to perform another action.
							return;
						}

						from.PlaySound( 0x241 );

						InternalTimer t = new InternalTimer( from, (ILockpickable) targeted, m_Item );
						m_Table[from] = t;
						t.Start();
					}
					else
					{
						// The door is not locked
						from.SendLocalizedMessage( 502069 ); // This does not appear to be locked
					}
				}
				else
				{
					from.SendLocalizedMessage( 501666 ); // You can't unlock that!
				}
			}

			private class InternalTimer : Timer
			{
				private Mobile m_From;
				private ILockpickable m_Item;
				private Lockpick m_Lockpick;

				public InternalTimer( Mobile from, ILockpickable item, Lockpick lockpick )
					: base( TimeSpan.Zero )
				{
					m_From = from;
					m_Item = item;
					m_Lockpick = lockpick;
				}

				protected override void OnTick()
				{
					m_Table.Remove( m_From );

					Item item = (Item) m_Item;

					if ( !m_From.InRange( item.GetWorldLocation(), 1 ) )
						return;

					int message = -1;

					if ( m_Item.LockLevel == 0 || m_Item.LockLevel == -255 )
					{
						// LockLevel of 0 means that the target can't be picklocked
						// LockLevel of -255 means it's magic locked

						message = 502073; // This lock cannot be picked by normal means
					}
					else if ( !m_Item.IsLockedAndTrappedByPlayer() && m_From.Skills[SkillName.Lockpicking].Value < m_Item.RequiredSkill )
					{
						message = 502072; // You don't see how that lock can be manipulated.
					}
					else if ( m_Item.CheckAccess( m_From ) )
					{
						if ( m_Lockpick.CheckSuccess( m_From, m_Item ) )
						{
							message = m_Item.LockPick( m_From );
							m_Lockpick.OnSuccess( m_From, m_Item );
						}
						else
						{
							message = m_Item.FailLockPick( m_From );
							m_Lockpick.OnFailure( m_From, m_Item );
						}
					}

					if ( message != -1 )
						item.SendLocalizedMessageTo( m_From, message );
				}
			}
		}
	}
}
