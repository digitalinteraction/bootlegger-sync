﻿using System;
using System.IO;
using AppKit;
using Bootlegger.Sync.Lib;
using Foundation;

namespace Bootlegger.Sync.Mac
{
	public partial class ViewController : NSViewController
	{
		public ViewController(IntPtr handle) : base(handle)
		{
		}

		Engine engine;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			cancelbtn.Enabled = false;


			// Do any additional setup after loading the view.
			engine = new Engine();
			engine.OnSignin += () =>
			{
				BeginInvokeOnMainThread(new Action(() =>
				{
					status.StringValue = "Connected...";
					gobtn.Enabled = true;
				}));
			};

			engine.OnUpdateNumbers += (a1, a2, a3) =>
			  {
				  BeginInvokeOnMainThread(new Action(() =>
				  {
					  total.StringValue = a3.ToString();
					  downloading.StringValue = a2.ToString();
					  waiting.StringValue = a3.ToString();
				  }));
			  };
			engine.OnSubProgress += (perc) =>
			  {
				  BeginInvokeOnMainThread((Action)delegate ()
				  {
					  subprogress.IntValue = perc;
				  });
			  };
			engine.OnProgress += (perc) =>
			  {
				  BeginInvokeOnMainThread((Action)delegate ()
				  {
					  progress.IntValue = perc;
					  if (perc == 100)
					  {
						  NotifyMeAction();
					  }
				  });
			  };
			engine.OnStatusUpdate += (perc) =>
			  {
				  BeginInvokeOnMainThread((Action)delegate ()
				  {
					  status.StringValue = perc;
				  });
			  };
			engine.OnEnableGo += () =>
			  {
				  BeginInvokeOnMainThread((Action)delegate ()
				  {
					  gobtn.Enabled = true;
				  });
			  };

			cancelbtn.Activated += (o, e) =>
			{
				engine.Cancel();
			};

			gobtn.Activated += (o, e) =>
			{


				var dlg = NSOpenPanel.OpenPanel;
				dlg.CanChooseFiles = false;
				dlg.CanChooseDirectories = true;
				dlg.CanCreateDirectories = true;


				if (dlg.RunModal() == 1)
				{
					// Nab the first file
					var url = dlg.Urls[0];

					if (url != null)
					{
						var path = url.Path;
						View.Window.SetTitleWithRepresentedFilename(Path.GetFileName(path));
						View.Window.RepresentedUrl = url;
						engine.SavePath = path;
						engine.ShouldTranscode = (transcode.State == NSCellStateValue.On);
						engine.ShouldApplyXMP = (xmp.State == NSCellStateValue.On);
						engine.StartSync();
						cancelbtn.Enabled = true;
						transcode.Enabled = false;
						xmp.Enabled = false;
						gobtn.Enabled = false;

					}
				}
			};
		}

		public void NotifyMeAction()
		{
			// First we create our notification and customize as needed
			NSUserNotification not = null;

			try
			{
				not = new NSUserNotification();
			}
			catch
			{
				new NSAlert
				{
					MessageText = "NSUserNotification Not Supported",
					InformativeText = "This API was introduced in OS X Mountain Lion (10.8)."
				}.RunSheetModal(View.Window);
				return;
			}

			not.Title = "Sync Complete";
			not.InformativeText = "Your Bootlegger Shoot is Up-to-Date";
			not.DeliveryDate = (NSDate)DateTime.Now;
			not.SoundName = NSUserNotification.NSUserNotificationDefaultSoundName;

			// We get the Default notification Center
			NSUserNotificationCenter center = NSUserNotificationCenter.DefaultUserNotificationCenter;

			//center.DidDeliverNotification += (s, e) =>
			//{
			//	Console.WriteLine("Notification Delivered");
			//	DeliveredColorWell.Color = NSColor.Green;
			//};

			//center.DidActivateNotification += (s, e) =>
			//{
			//	Console.WriteLine("Notification Touched");
			//	TouchedColorWell.Color = NSColor.Green;
			//};

			// If we return true here, Notification will show up even if your app is TopMost.
			center.ShouldPresentNotification = (c, n) => { return true; };

			center.ScheduleNotification(not);

		}

		public override void ViewWillAppear()
		{
			base.ViewWillAppear();
			View.Window.Title = "Bootlegger Sync v" + Engine.VERSION;
			View.Window.Delegate = new MyWindowDelegate(engine);
		}

		private class MyWindowDelegate : NSWindowDelegate
		{
			Engine engine;

			public MyWindowDelegate(Engine engine)
			{
				this.engine = engine;
			}

			public override bool WindowShouldClose(NSObject sender)
			{
				if (engine.IsRunning)
				{
					NSAlert alert = NSAlert.WithMessage("Closing Application", "Yes", "No", null, "Currently performing sync, are you sure you want to exit?");
					var retval = alert.RunModal();
					if (retval != 0)
					{
						NSApplication.SharedApplication.Terminate(this);
						return true;
					}
					else {
						return false;
					}
				}
				else
				{
					NSApplication.SharedApplication.Terminate(this);
					return true;
				}
			}
		}

		public override NSObject RepresentedObject
		{
			get
			{
				return base.RepresentedObject;
			}
			set
			{
				base.RepresentedObject = value;
				// Update the view, if already loaded.
			}
		}


	}
}
