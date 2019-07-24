// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace OurStory.Sync.Mac
{
	[Register ("ViewController")]
	partial class ViewController
	{
		[Outlet]
		AppKit.NSButton cancelbtn { get; set; }

		[Outlet]
		AppKit.NSTextField downloading { get; set; }

		[Outlet]
		AppKit.NSButton gobtn { get; set; }

		[Outlet]
		AppKit.NSLevelIndicator progress { get; set; }

		[Outlet]
		AppKit.NSTextField status { get; set; }

		[Outlet]
		AppKit.NSLevelIndicator subprogress { get; set; }

		[Outlet]
		AppKit.NSTextField total { get; set; }

		[Outlet]
		AppKit.NSButton transcode { get; set; }

		[Outlet]
		AppKit.NSTextField waiting { get; set; }

		[Outlet]
		AppKit.NSButton xmp { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (gobtn != null) {
				gobtn.Dispose ();
				gobtn = null;
			}

			if (cancelbtn != null) {
				cancelbtn.Dispose ();
				cancelbtn = null;
			}

			if (status != null) {
				status.Dispose ();
				status = null;
			}

			if (xmp != null) {
				xmp.Dispose ();
				xmp = null;
			}

			if (transcode != null) {
				transcode.Dispose ();
				transcode = null;
			}

			if (progress != null) {
				progress.Dispose ();
				progress = null;
			}

			if (subprogress != null) {
				subprogress.Dispose ();
				subprogress = null;
			}

			if (waiting != null) {
				waiting.Dispose ();
				waiting = null;
			}

			if (downloading != null) {
				downloading.Dispose ();
				downloading = null;
			}

			if (total != null) {
				total.Dispose ();
				total = null;
			}
		}
	}
}
