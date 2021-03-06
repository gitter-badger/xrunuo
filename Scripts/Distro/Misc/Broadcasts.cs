using System;
using Server;
using Server.Events;

namespace Server.Misc
{
	public class Broadcasts
	{
		public static void Initialize()
		{
			EventSink.Instance.Crashed += new CrashedEventHandler( EventSink_Crashed );
			EventSink.Instance.Shutdown += new ShutdownEventHandler( EventSink_Shutdown );
		}

		public static void EventSink_Crashed( CrashedEventArgs e )
		{
			try
			{
				World.Broadcast( 0x35, true, "The server has crashed." );
			}
			catch
			{
			}
		}

		public static void EventSink_Shutdown( ShutdownEventArgs e )
		{
			try
			{
				World.Broadcast( 0x35, true, "The server has shut down." );
			}
			catch
			{
			}
		}
	}
}