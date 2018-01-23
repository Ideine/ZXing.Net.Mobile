using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Graphics;
using ZXing.Mobile.CameraAccess;

namespace ZXing.Mobile
{
	public class ZXingSurfaceView : TextureView, TextureView.ISurfaceTextureListener, IScannerView, IScannerSessionHost
	{
		public ZXingSurfaceView(Context context, MobileBarcodeScanningOptions options)
			: base(context)
		{
			ScanningOptions = options ?? new MobileBarcodeScanningOptions();
			Init();
		}

		protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			Init();
		}

		bool addedHolderCallback = false;

		private void Init()
		{
			if (!addedHolderCallback)
			{
				this.SurfaceTextureListener = this;
				addedHolderCallback = true;
			}
		}

		public async void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
		{
			await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

			if (_cameraAnalyzer == null)
				_cameraAnalyzer = new CameraAnalyzer(this, this);

			_cameraAnalyzer.ResumeAnalysis();

			_cameraAnalyzer.SetupCamera();
		}

		public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
		{
			try
			{
				if (addedHolderCallback)
				{
					this.SurfaceTextureListener = null;
					addedHolderCallback = false;
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
			await ZXing.Net.Mobile.Android.PermissionsHandler.PermissionRequestTask;

			_cameraAnalyzer?.RefreshCamera();
		}

		public void OnSurfaceTextureUpdated(SurfaceTexture surface)
		{

		}

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

		public void AutoFocus()
		{
			_cameraAnalyzer.AutoFocus();
		}

		public void AutoFocus(int x, int y)
		{
			_cameraAnalyzer.AutoFocus(x, y);
		}

		public void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
		{
			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

			_cameraAnalyzer.BarcodeFound += (sender, result) =>
			{
				scanResultCallback?.Invoke(result);
			};
			_cameraAnalyzer.ResumeAnalysis();
		}

		public void StopScanning()
		{
			_cameraAnalyzer.ShutdownCamera();
		}

		public void PauseAnalysis()
		{
			_cameraAnalyzer.PauseAnalysis();
		}

		public void ResumeAnalysis()
		{
			_cameraAnalyzer.ResumeAnalysis();
		}

		public void Torch(bool on)
		{
			if (on)
				_cameraAnalyzer.Torch.TurnOn();
			else
				_cameraAnalyzer.Torch.TurnOff();
		}

		public void ToggleTorch()
		{
			_cameraAnalyzer.Torch.Toggle();
		}

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public bool IsTorchOn => _cameraAnalyzer.Torch.IsEnabled;

		public bool IsAnalyzing => _cameraAnalyzer.IsAnalyzing;

		private CameraAnalyzer _cameraAnalyzer;

		public bool HasTorch => _cameraAnalyzer.Torch.IsSupported;

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
				Init();
		}
	}
}
