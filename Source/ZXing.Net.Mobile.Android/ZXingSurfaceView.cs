using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using ZXing.Net.Mobile.Android;
using ZXing.Mobile.CameraAccess;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
	public class ZXingSurfaceView : TextureView, TextureView.ISurfaceTextureListener, IScannerView, IScannerSessionHost
	{
		private readonly TaskCompletionSource<bool> _initialized = new TaskCompletionSource<bool>();

		private CameraAnalyzer _cameraAnalyzer;

		private bool _holderCallbackAdded = false;

		private Action<Result> _scanResultCallback;

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public ZXingSurfaceView(Context context, MobileBarcodeScanningOptions options) : base(context)
		{
			ScanningOptions = options ?? new MobileBarcodeScanningOptions();
			Init();
		}

		protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			Init();
		}

		private void Init()
		{
			if (!_holderCallbackAdded)
			{
				SurfaceTextureListener = this;
				_holderCallbackAdded = true;
			}
		}

		public async void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			await PermissionsHandler.PermissionRequestTask;

			if (_cameraAnalyzer == null)
			{
				_cameraAnalyzer = new CameraAnalyzer(this, this);
				_initialized.SetResult(true);
			}

			_cameraAnalyzer.ResumeAnalysis();

			_cameraAnalyzer.SetupCamera();
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			try
			{
				if (_holderCallbackAdded)
				{
					SurfaceTextureListener = null;
					_holderCallbackAdded = false;
				}
			}
			catch
			{
			}

			_cameraAnalyzer?.ShutdownCamera();

			return true;
		}

		public async void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
		{
			await PermissionsHandler.PermissionRequestTask;

			_cameraAnalyzer?.RefreshCamera();
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface) { }

		public override bool OnTouchEvent(MotionEvent e)
		{
			var r = base.OnTouchEvent(e);

			switch (e.Action)
			{
				case MotionEventActions.Down:
					return true;
				case MotionEventActions.Up:
					var touchX = e.GetX();
					var touchY = e.GetY();
					this.AutoFocus((int)touchX, (int)touchY);
					break;
			}

			return r;
		}

		public async void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
		{
			_scanResultCallback = scanResultCallback;

			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

			await _initialized.Task;

			_cameraAnalyzer.BarcodeFound += OnBarcodeFound;

			_cameraAnalyzer.ResumeAnalysis();
		}


		public void Torch(bool on)
		{
			if (on)
			{
				_cameraAnalyzer.Torch.TurnOn();
			}
			else
			{
				_cameraAnalyzer.Torch.TurnOff();
			}
		}

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			// Reinit things
			Init();
		}

		protected override void OnWindowVisibilityChanged(ViewStates visibility)
		{
			base.OnWindowVisibilityChanged(visibility);

			if (visibility == ViewStates.Visible)
			{
				Init();
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_cameraAnalyzer != null)
				{
					_cameraAnalyzer.BarcodeFound -= OnBarcodeFound;
					_cameraAnalyzer = null;
				}
			}
			base.Dispose(disposing);
		}

		private void OnBarcodeFound(object sender, Result result) => _scanResultCallback?.Invoke(result);

		public bool HasTorch => _cameraAnalyzer?.Torch?.IsSupported ?? false;

		public bool IsTorchOn => _cameraAnalyzer?.Torch?.IsEnabled ?? false;

		public bool IsAnalyzing => _cameraAnalyzer?.IsAnalyzing ?? false;

		public void StopScanning() => _cameraAnalyzer?.ShutdownCamera();

		public void PauseAnalysis() => _cameraAnalyzer?.PauseAnalysis();

		public void ResumeAnalysis() => _cameraAnalyzer?.ResumeAnalysis();

		public void AutoFocus() => _cameraAnalyzer?.AutoFocus();

		public void AutoFocus(int x, int y) => _cameraAnalyzer?.AutoFocus(x, y);

		public void ToggleTorch() => _cameraAnalyzer?.Torch?.Toggle();
	}
}
