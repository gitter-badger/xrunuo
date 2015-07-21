using System;
using System.IO;
using System.Web;
using System.Web.UI;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Diagnostics;
using HtmlTag = System.Web.UI.HtmlTextWriterTag;
using HtmlAttr = System.Web.UI.HtmlTextWriterAttribute;

namespace Server.Engines.Reports
{
	public class HtmlRenderer
	{
		private string m_Type;
		private string m_Title;
		private string m_OutputDirectory;

		private DateTime m_TimeStamp;
		private ObjectCollection m_Objects;

		private HtmlRenderer( string outputDirectory )
		{
			m_Type = outputDirectory;
			m_Title = ( m_Type == "staff" ? "Staff" : "Stats" );
			m_OutputDirectory = Path.Combine( Environment.BaseDirectory, "output" );

			if ( !Directory.Exists( m_OutputDirectory ) )
				Directory.CreateDirectory( m_OutputDirectory );

			m_OutputDirectory = Path.Combine( m_OutputDirectory, outputDirectory );

			if ( !Directory.Exists( m_OutputDirectory ) )
				Directory.CreateDirectory( m_OutputDirectory );
		}

		public HtmlRenderer( string outputDirectory, Snapshot ss, SnapshotHistory history )
			: this( outputDirectory )
		{
			m_TimeStamp = ss.TimeStamp;

			m_Objects = new ObjectCollection();

			for ( int i = 0; i < ss.Children.Count; ++i )
				m_Objects.Add( ss.Children[i] );

			m_Objects.Add( BarGraph.OverTime( history, "General Stats", "Clients", 1, 100, 6 ) );
			m_Objects.Add( BarGraph.OverTime( history, "General Stats", "Items", 24, 9, 1 ) );
			m_Objects.Add( BarGraph.OverTime( history, "General Stats", "Players", 24, 9, 1 ) );
			m_Objects.Add( BarGraph.OverTime( history, "General Stats", "NPCs", 24, 9, 1 ) );
			m_Objects.Add( BarGraph.DailyAverage( history, "General Stats", "Clients" ) );
			m_Objects.Add( BarGraph.Growth( history, "General Stats", "Clients" ) );

			m_Objects.Add( BarGraph.OverTime( history, "General Stats", "Memory (MB)", 1, 100, 6 ) );
			m_Objects.Add( BarGraph.DailyAverage( history, "General Stats", "Memory (MB)" ) );
			m_Objects.Add( BarGraph.Growth( history, "General Stats", "Memory (MB)" ) );
		}

		public HtmlRenderer( string outputDirectory, StaffHistory history )
			: this( outputDirectory )
		{
			m_TimeStamp = DateTime.Now;

			m_Objects = new ObjectCollection();

			history.Render( m_Objects );
		}

		public void Render()
		{
			Console.WriteLine( "Reports: {0}: Render started", m_Title );

			RenderFull();

			for ( int i = 0; i < m_Objects.Count; ++i )
				RenderSingle( m_Objects[i] );

			Console.WriteLine( "Reports: {0}: Render complete", m_Title );
		}

		private static readonly string FtpHost = Environment.Config.Reports.FtpHost;

		private static readonly string FtpUsername = Environment.Config.Reports.FtpUsername;
		private static readonly string FtpPassword = Environment.Config.Reports.FtpPassword;

		private static readonly string FtpStatsDirectory = Environment.Config.Reports.FtpStatsDirectory;
		private static readonly string FtpStaffDirectory = Environment.Config.Reports.FtpStaffDirectory;

		public void Upload()
		{
			if ( FtpHost == null )
				return;

			Console.WriteLine( "Reports: {0}: Upload started", m_Title );

			#region "windows upload
			//			string filePath = Path.Combine( m_OutputDirectory, "upload.ftp" );
			//
			//			using ( StreamWriter op = new StreamWriter( filePath ) )
			//			{
			//				op.WriteLine( "open \"{0}\"", FtpHost );
			//				op.WriteLine( FtpUsername );
			//				op.WriteLine( FtpPassword );
			//				op.WriteLine( "cd \"{0}\"", ( m_Type == "staff" ? FtpStaffDirectory : FtpStatsDirectory ) );
			//				op.WriteLine( "mput \"{0}\"", Path.Combine( m_OutputDirectory, "*.html" ) );
			//				op.WriteLine( "binary" );
			//				op.WriteLine( "mput \"{0}\"", Path.Combine( m_OutputDirectory, "*.png" ) );
			//				op.WriteLine( "disconnect" );
			//				op.Write( "quit" );
			//			}
			//
			//			ProcessStartInfo psi = new ProcessStartInfo();
			//
			//			psi.FileName = "ftp";
			//			psi.Arguments = String.Format( "-i -s:\"{0}\"", filePath );
			//
			//			psi.CreateNoWindow = true;
			//			psi.WindowStyle = ProcessWindowStyle.Hidden;
			//
			//			try
			//			{
			//				Process p = Process.Start( psi );
			//
			//				p.WaitForExit();
			//			} 
			//			catch (Exception e)
			//			{
			//                Console.WriteLine(e.Message);
			//			}
			#endregion

			#region "linux upload"
			ProcessStartInfo psi = new ProcessStartInfo();

			psi.FileName = "sh";
			psi.Arguments = String.Format( "-c \"ncftpput -u {0} -p {1} {2} {3} {4}/*\"", FtpUsername, FtpPassword, FtpHost, ( m_Type == "staff" ? FtpStaffDirectory : FtpStatsDirectory ), m_OutputDirectory );

			psi.CreateNoWindow = true;
			psi.WindowStyle = ProcessWindowStyle.Hidden;

			try
			{
				Process p = Process.Start( psi );

				p.WaitForExit();
			}
			catch ( Exception e )
			{
				Console.WriteLine( e.Message );
			}
			#endregion

			Console.WriteLine( "Reports: {0}: Upload complete", m_Title );
		}

		public void RenderFull()
		{
			string filePath = Path.Combine( m_OutputDirectory, "index.html" );

			using ( StreamWriter op = new StreamWriter( filePath ) )
			{
				using ( HtmlTextWriter html = new HtmlTextWriter( op, "\t" ) )
				{
					RenderFull( html );
				}
			}
		}

		private static readonly string ShardTitle = Environment.Config.ServerName;

		public void RenderFull( HtmlTextWriter html )
		{
			html.RenderBeginTag( HtmlTag.Html );

			html.RenderBeginTag( HtmlTag.Head );

			html.RenderBeginTag( HtmlTag.Title );
			html.Write( "{0} Statistics", ShardTitle );
			html.RenderEndTag();

			html.AddAttribute( "rel", "stylesheet" );
			html.AddAttribute( HtmlAttr.Type, "text/css" );
			html.AddAttribute( HtmlAttr.Href, "styles.css" );
			html.RenderBeginTag( HtmlTag.Link );
			html.RenderEndTag();

			html.RenderEndTag();

			html.RenderBeginTag( HtmlTag.Body );

			for ( int i = 0; i < m_Objects.Count; ++i )
			{
				RenderDirect( m_Objects[i], html );
				html.Write( "<br><br>" );
			}

			html.RenderBeginTag( HtmlTag.Center );
			TimeZone tz = TimeZone.CurrentTimeZone;
			bool isDaylight = tz.IsDaylightSavingTime( m_TimeStamp );
			TimeSpan utcOffset = tz.GetUtcOffset( m_TimeStamp );

			html.Write( "Snapshot taken at {0:d} {0:t}. All times are {1}.", m_TimeStamp, tz.StandardName );
			html.RenderEndTag();

			html.RenderEndTag();

			html.RenderEndTag();
		}

		public static string SafeFileName( string name )
		{
			return name.ToLower().Replace( ' ', '_' );
		}

		public void RenderSingle( PersistableObject obj )
		{
			string filePath = Path.Combine( m_OutputDirectory, SafeFileName( FindNameFrom( obj ) ) + ".html" );

			using ( StreamWriter op = new StreamWriter( filePath ) )
			{
				using ( HtmlTextWriter html = new HtmlTextWriter( op, "\t" ) )
				{
					RenderSingle( obj, html );
				}
			}
		}

		private string FindNameFrom( PersistableObject obj )
		{
			if ( obj is Report )
				return ( obj as Report ).Name;
			else if ( obj is Chart )
				return ( obj as Chart ).Name;

			return "Invalid";
		}

		public void RenderSingle( PersistableObject obj, HtmlTextWriter html )
		{
			html.RenderBeginTag( HtmlTag.Html );

			html.RenderBeginTag( HtmlTag.Head );

			html.RenderBeginTag( HtmlTag.Title );
			html.Write( "{0} Statistics - {1}", ShardTitle, FindNameFrom( obj ) );
			html.RenderEndTag();

			html.AddAttribute( "rel", "stylesheet" );
			html.AddAttribute( HtmlAttr.Type, "text/css" );
			html.AddAttribute( HtmlAttr.Href, "styles.css" );
			html.RenderBeginTag( HtmlTag.Link );
			html.RenderEndTag();

			html.RenderEndTag();

			html.RenderBeginTag( HtmlTag.Body );

			html.RenderBeginTag( HtmlTag.Center );

			RenderDirect( obj, html );

			html.Write( "<br>" );

			TimeZone tz = TimeZone.CurrentTimeZone;
			bool isDaylight = tz.IsDaylightSavingTime( m_TimeStamp );
			TimeSpan utcOffset = tz.GetUtcOffset( m_TimeStamp );

			html.Write( "Snapshot taken at {0:d} {0:t}. All times are {1}.", m_TimeStamp, tz.StandardName );
			html.RenderEndTag();

			html.RenderEndTag();

			html.RenderEndTag();
		}

		public void RenderDirect( PersistableObject obj, HtmlTextWriter html )
		{
			if ( obj is Report )
				RenderReport( obj as Report, html );
			else if ( obj is BarGraph )
				RenderBarGraph( obj as BarGraph, html );
			else if ( obj is PieChart )
				RenderPieChart( obj as PieChart, html );
		}

		private void RenderPieChart( PieChart chart, HtmlTextWriter html )
		{
			PieChartRenderer pieChart = new PieChartRenderer( Color.White );

			pieChart.ShowPercents = chart.ShowPercents;

			string[] labels = new string[chart.Items.Count];
			string[] values = new string[chart.Items.Count];

			for ( int i = 0; i < chart.Items.Count; ++i )
			{
				ChartItem item = chart.Items[i];

				labels[i] = item.Name;
				values[i] = item.Value.ToString();
			}

			pieChart.CollectDataPoints( labels, values );

			Bitmap bmp = pieChart.Draw();

			string fileName = chart.FileName + ".png";
			bmp.Save( Path.Combine( m_OutputDirectory, fileName ), ImageFormat.Png );

			html.Write( "<!-- " );

			html.AddAttribute( HtmlAttr.Href, "#" );
			html.AddAttribute( HtmlAttr.Onclick, String.Format( "javascript:window.open('{0}.html','ChildWindow','width={1},height={2},resizable=no,status=no,toolbar=no')", SafeFileName( FindNameFrom( chart ) ), bmp.Width + 30, bmp.Height + 80 ) );
			html.RenderBeginTag( HtmlTag.A );
			html.Write( chart.Name );
			html.RenderEndTag();

			html.Write( " -->" );

			html.AddAttribute( HtmlAttr.Cellpadding, "0" );
			html.AddAttribute( HtmlAttr.Cellspacing, "0" );
			html.AddAttribute( HtmlAttr.Border, "0" );
			html.RenderBeginTag( HtmlTag.Table );

			html.RenderBeginTag( HtmlTag.Tr );
			html.AddAttribute( HtmlAttr.Class, "tbl-border" );
			html.RenderBeginTag( HtmlTag.Td );

			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Cellpadding, "4" );
			html.AddAttribute( HtmlAttr.Cellspacing, "1" );
			html.RenderBeginTag( HtmlTag.Table );

			html.RenderBeginTag( HtmlTag.Tr );

			html.AddAttribute( HtmlAttr.Colspan, "10" );
			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Align, "center" );
			html.AddAttribute( HtmlAttr.Class, "header" );
			html.RenderBeginTag( HtmlTag.Td );
			html.Write( chart.Name );
			html.RenderEndTag();
			html.RenderEndTag();

			html.RenderBeginTag( HtmlTag.Tr );

			html.AddAttribute( HtmlAttr.Colspan, "10" );
			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Align, "center" );
			html.AddAttribute( HtmlAttr.Class, "entry" );
			html.RenderBeginTag( HtmlTag.Td );

			html.AddAttribute( HtmlAttr.Width, bmp.Width.ToString() );
			html.AddAttribute( HtmlAttr.Height, bmp.Height.ToString() );
			html.AddAttribute( HtmlAttr.Src, fileName );
			html.RenderBeginTag( HtmlTag.Img );
			html.RenderEndTag();

			html.RenderEndTag();
			html.RenderEndTag();

			html.RenderEndTag();
			html.RenderEndTag();
			html.RenderEndTag();
			html.RenderEndTag();

			bmp.Dispose();
		}

		private void RenderBarGraph( BarGraph graph, HtmlTextWriter html )
		{
			BarGraphRenderer barGraph = new BarGraphRenderer( Color.White );

			barGraph.RenderMode = graph.RenderMode;

			barGraph._regions = graph.Regions;
			barGraph.SetTitles( graph.xTitle, null );

			if ( graph.yTitle != null )
			{
				barGraph.VerticalLabel = graph.yTitle;
			}

			barGraph.FontColor = Color.Black;
			barGraph.ShowData = ( graph.Interval == 1 );
			barGraph.VerticalTickCount = graph.Ticks;

			string[] labels = new string[graph.Items.Count];
			string[] values = new string[graph.Items.Count];

			for ( int i = 0; i < graph.Items.Count; ++i )
			{
				ChartItem item = graph.Items[i];

				labels[i] = item.Name;
				values[i] = item.Value.ToString();
			}

			barGraph._interval = graph.Interval;
			barGraph.CollectDataPoints( labels, values );

			Bitmap bmp = barGraph.Draw();

			string fileName = graph.FileName + ".png";
			bmp.Save( Path.Combine( m_OutputDirectory, fileName ), ImageFormat.Png );

			html.Write( "<!-- " );

			html.AddAttribute( HtmlAttr.Href, "#" );
			html.AddAttribute( HtmlAttr.Onclick, String.Format( "javascript:window.open('{0}.html','ChildWindow','width={1},height={2},resizable=no,status=no,toolbar=no')", SafeFileName( FindNameFrom( graph ) ), bmp.Width + 30, bmp.Height + 80 ) );
			html.RenderBeginTag( HtmlTag.A );
			html.Write( graph.Name );
			html.RenderEndTag();

			html.Write( " -->" );

			html.AddAttribute( HtmlAttr.Cellpadding, "0" );
			html.AddAttribute( HtmlAttr.Cellspacing, "0" );
			html.AddAttribute( HtmlAttr.Border, "0" );
			html.RenderBeginTag( HtmlTag.Table );

			html.RenderBeginTag( HtmlTag.Tr );
			html.AddAttribute( HtmlAttr.Class, "tbl-border" );
			html.RenderBeginTag( HtmlTag.Td );

			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Cellpadding, "4" );
			html.AddAttribute( HtmlAttr.Cellspacing, "1" );
			html.RenderBeginTag( HtmlTag.Table );

			html.RenderBeginTag( HtmlTag.Tr );

			html.AddAttribute( HtmlAttr.Colspan, "10" );
			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Align, "center" );
			html.AddAttribute( HtmlAttr.Class, "header" );
			html.RenderBeginTag( HtmlTag.Td );
			html.Write( graph.Name );
			html.RenderEndTag();
			html.RenderEndTag();

			html.RenderBeginTag( HtmlTag.Tr );

			html.AddAttribute( HtmlAttr.Colspan, "10" );
			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Align, "center" );
			html.AddAttribute( HtmlAttr.Class, "entry" );
			html.RenderBeginTag( HtmlTag.Td );

			html.AddAttribute( HtmlAttr.Width, bmp.Width.ToString() );
			html.AddAttribute( HtmlAttr.Height, bmp.Height.ToString() );
			html.AddAttribute( HtmlAttr.Src, fileName );
			html.RenderBeginTag( HtmlTag.Img );
			html.RenderEndTag();

			html.RenderEndTag();
			html.RenderEndTag();

			html.RenderEndTag();
			html.RenderEndTag();
			html.RenderEndTag();
			html.RenderEndTag();

			bmp.Dispose();
		}

		private void RenderReport( Report report, HtmlTextWriter html )
		{
			html.AddAttribute( HtmlAttr.Width, report.Width );
			html.AddAttribute( HtmlAttr.Cellpadding, "0" );
			html.AddAttribute( HtmlAttr.Cellspacing, "0" );
			html.AddAttribute( HtmlAttr.Border, "0" );
			html.RenderBeginTag( HtmlTag.Table );

			html.RenderBeginTag( HtmlTag.Tr );
			html.AddAttribute( HtmlAttr.Class, "tbl-border" );
			html.RenderBeginTag( HtmlTag.Td );

			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Cellpadding, "4" );
			html.AddAttribute( HtmlAttr.Cellspacing, "1" );
			html.RenderBeginTag( HtmlTag.Table );

			html.RenderBeginTag( HtmlTag.Tr );
			html.AddAttribute( HtmlAttr.Colspan, "10" );
			html.AddAttribute( HtmlAttr.Width, "100%" );
			html.AddAttribute( HtmlAttr.Align, "center" );
			html.AddAttribute( HtmlAttr.Class, "header" );
			html.RenderBeginTag( HtmlTag.Td );
			html.Write( report.Name );
			html.RenderEndTag();
			html.RenderEndTag();

			bool isNamed = false;

			for ( int i = 0; i < report.Columns.Count && !isNamed; ++i )
				isNamed = ( report.Columns[i].Name != null );

			if ( isNamed )
			{
				html.RenderBeginTag( HtmlTag.Tr );

				for ( int i = 0; i < report.Columns.Count; ++i )
				{
					ReportColumn column = report.Columns[i];

					html.AddAttribute( HtmlAttr.Class, "header" );
					html.AddAttribute( HtmlAttr.Width, column.Width );
					html.AddAttribute( HtmlAttr.Align, column.Align );
					html.RenderBeginTag( HtmlTag.Td );

					html.Write( column.Name );

					html.RenderEndTag();
				}

				html.RenderEndTag();
			}

			for ( int i = 0; i < report.Items.Count; ++i )
			{
				ReportItem item = report.Items[i];

				html.RenderBeginTag( HtmlTag.Tr );

				for ( int j = 0; j < item.Values.Count; ++j )
				{
					if ( !isNamed && j == 0 )
						html.AddAttribute( HtmlAttr.Width, report.Columns[j].Width );

					html.AddAttribute( HtmlAttr.Align, report.Columns[j].Align );
					html.AddAttribute( HtmlAttr.Class, "entry" );
					html.RenderBeginTag( HtmlTag.Td );

					if ( item.Values[j].Format == null )
						html.Write( item.Values[j].Value );
					else
						html.Write( int.Parse( item.Values[j].Value ).ToString( item.Values[j].Format ) );

					html.RenderEndTag();
				}

				html.RenderEndTag();
			}

			html.RenderEndTag();
			html.RenderEndTag();
			html.RenderEndTag();
			html.RenderEndTag();
		}
	}
}