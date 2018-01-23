using Android.App;
using Android.Widget;
using Android.OS;
using ZXing.Net.Mobile.Android;
using ZXing.Mobile;
using System.Collections.Generic;
using Android.Support.V7.App;
using System;

namespace AZXing
{
	[Activity(Label = "AZXing", MainLauncher = true, Icon = "@mipmap/icon", Theme = "@style/Theme.AppCompat.Light")]
	public class MainActivity : AppCompatActivity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			Button button = FindViewById<Button>(Resource.Id.myButton);

			button.Click += Button_Click;
		}

		async void Button_Click(object sender, System.EventArgs e)
		{
			if (PermissionsHandler.NeedsPermissionRequest(this))
			{
				await PermissionsHandler.RequestPermissionsAsync(this);
			}

			var opts = new MobileBarcodeScanningOptions
			{
				PossibleFormats = new List<ZXing.BarcodeFormat> { ZXing.BarcodeFormat.QR_CODE },
				InitialDelayBeforeAnalyzingFrames = 500,
			};

			var scanner = new ZXingScannerFragment();

			var tran = this.SupportFragmentManager.BeginTransaction();
			tran.Replace(Resource.Id.Scan, scanner);
			tran.CommitAllowingStateLoss();

			scanner.StartScanning(HandleResult, opts);
		}


		public void HandleResult(ZXing.Result result)
		{
			Console.WriteLine($"result: {result.Text}");
		}
	}
}

