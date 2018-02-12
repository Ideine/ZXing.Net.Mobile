using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using System.Threading.Tasks;
using Android;
using Android.Runtime;

namespace ZXing.Mobile
{
	public class ZXingScannerFragment : Fragment, IZXingScanner<View>, IScannerView
	{
		private readonly TaskCompletionSource<bool> _initialized = new TaskCompletionSource<bool>();

		public View CustomOverlayView { get; set; }

		public bool UseCustomOverlayView { get; set; }

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public string TopText { get; set; }

		public string BottomText { get; set; }

		private FrameLayout _frame;

		private ZXingSurfaceView _scanner;

		private ZxingOverlayView _zxingOverlay;

		private Action<Result> _scanCallback;

		public ZXingScannerFragment()
		{
			UseCustomOverlayView = false;
		}

		protected ZXingScannerFragment(IntPtr javaRef, JniHandleOwnership transfer) : base(javaRef, transfer) { }


		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			_frame = (FrameLayout)inflater.Inflate(Resource.Layout.zxingscannerfragmentlayout, container, false);

			using (var layoutParams = GetChildLayoutParams())
			{
				try
				{
					_scanner = new ZXingSurfaceView(Activity, ScanningOptions);

					_initialized.SetResult(true);

					_frame.AddView(_scanner, layoutParams);


					if (!UseCustomOverlayView)
					{
						_zxingOverlay = new ZxingOverlayView(Activity)
						{
							TopText = TopText ?? "",
							BottomText = BottomText ?? ""
						};
						_frame.AddView(_zxingOverlay, layoutParams);
					}
					else if (CustomOverlayView != null)
					{
						_frame.AddView(CustomOverlayView, layoutParams);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Create Surface View Failed: " + ex);
				}
			}

			Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "ZXingScannerFragment->OnResume exit");

			return _frame;
		}

		public override void OnStart()
		{
			base.OnStart();
			// won't be 0 if OnCreateView has been called before.
			if (_frame.ChildCount == 0)
			{
				using (var layoutParams = GetChildLayoutParams())
				{
					// reattach scanner and overlay views.
					_frame.AddView(_scanner, layoutParams);

					if (!UseCustomOverlayView)
					{
						_frame.AddView(_zxingOverlay, layoutParams);
					}
					else if (CustomOverlayView != null)
					{
						_frame.AddView(CustomOverlayView, layoutParams);
					}
				}
			}
		}

		public override void OnStop()
		{
			if (_scanner != null)
			{
				_scanner.StopScanning();

				_frame?.RemoveView(_scanner);
			}

			if (!UseCustomOverlayView)
			{
				_frame?.RemoveView(_zxingOverlay);
			}
			else if (CustomOverlayView != null)
			{
				_frame?.RemoveView(CustomOverlayView);
			}

			base.OnStop();
		}

		public async void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null)
		{
			ScanningOptions = options;
			_scanCallback = scanResultHandler;

			await _initialized.Task;

			Scan();
		}

		private LinearLayout.LayoutParams GetChildLayoutParams() => new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
		{
			Weight = 1
		};

		public void Torch(bool on) => _scanner?.Torch(on);

		public void AutoFocus() => _scanner?.AutoFocus();

		public void AutoFocus(int x, int y) => _scanner?.AutoFocus(x, y);

		private void Scan() => _scanner?.StartScanning(_scanCallback, ScanningOptions);

		public void StopScanning() => _scanner?.StopScanning();

		public void PauseAnalysis() => _scanner?.PauseAnalysis();

		public void ResumeAnalysis() => _scanner?.ResumeAnalysis();

		public void ToggleTorch() => _scanner?.ToggleTorch();

		public bool IsTorchOn => _scanner?.IsTorchOn ?? false;

		public bool IsAnalyzing => _scanner?.IsAnalyzing ?? false;

		public bool HasTorch => _scanner?.HasTorch ?? false;
	}
}

