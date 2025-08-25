using System.Diagnostics;
using cmbSDKMaui;
using Color = System.Drawing.Color;

namespace CognexTest;

public partial class MainPage : ContentPage
{
	// UI
	private readonly Label _lblStatus;
	private readonly Label _lblResultText;
	private readonly Label _lblResultType;
	private readonly Button _btnScan;

	// Scanner
	private readonly ScannerControl _scannerControl;
	private bool _isScanning;

	// Parameters (edit to suit your needs)
	private ScannerDevice _paramDeviceClass = ScannerDevice.PhoneCamera;
	private ScannerCameraMode _paramCameraMode = ScannerCameraMode.NoAimer;

	// Replace with your real cross‑platform SDK key (or leave null if you inject per‑platform).
	private const string SdkKey = "REPLACE_WITH_YOUR_COGNEX_SDK_KEY";

	public MainPage()
	{
		Title = ".NET MAUI — Cognex Scanner";

		// Build simple UI
		_scannerControl = new ScannerControl
		{
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill
		};

		_btnScan = new Button
		{
			Text = "START SCANNING",
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.End
		};
		_btnScan.Clicked += OnScanButtonClicked;

		_lblStatus = new Label { Text = "Status: Disconnected" };
		_lblResultText = new Label { Text = "Result: " };
		_lblResultType = new Label { Text = "Symbology: " };

		var grid = new Grid
		{
			RowDefinitions =
				{
					new RowDefinition { Height = GridLength.Star },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
				},
			ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Star } }
		};

		grid.Add(_scannerControl, 0, 0);
		grid.Add(_btnScan, 0, 1);
		grid.Add(_lblStatus, 0, 2);

		var resultStack = new VerticalStackLayout
		{
			Children = { _lblResultText, _lblResultType }
		};
		grid.Add(resultStack, 0, 3);

		Content = grid;

		// Hook scanner events (names follow the Cognex docs examples)
		_scannerControl.AvailabilityChanged += OnAvailabilityChanged;
		_scannerControl.ConnectionCompleted += OnConnectionCompleted;
		_scannerControl.ConnectionStateChanged += OnConnectionStateChanged;
		//_scannerControl.ReadResultReceived += OnReadResultReceived;
		_scannerControl.ResponseReceived += OnResponseReceived;
		_scannerControl.SymbologyEnabled += OnSymbologyEnabled;

		// Initialize device + connect
		CreateScannerDevice();
		ConnectToScannerDevice();
	}

	// Create scanner device based on desired reader (MX vs. phone camera)
	private void CreateScannerDevice()
	{
		if (_paramDeviceClass == ScannerDevice.PhoneCamera)
		{
			// Create a camera scanner (full screen preview in this sample)
			// If you have a cross‑platform license key, pass it as the 4th parameter.
			// If you license per‑platform, remove the key here and put it in Info.plist/AndroidManifest.
			_scannerControl.GetPhoneCameraDevice(_paramCameraMode, ScannerPreviewOption.Defaults, true, SdkKey);
		}
		else
		{
			// MX-1xxx series (no license key needed)
			_scannerControl.GetMXDevice();
		}
	}

	private void ConnectToScannerDevice()
	{
		_scannerControl.Connect();
	}

	// Example: enable the few symbologies you actually need and tweak overlay/DMCC as desired
	private void ConfigureScannerDevice()
	{
		// Enable only the symbologies you scan to keep performance optimal
		_scannerControl.SetSymbologyEnabled(Symbology.Datamatrix, true);
		_scannerControl.SetSymbologyEnabled(Symbology.C128, true);

		// Optional: tweak overlay corner rectangle color (legacy overlay shown as example)
		// To switch to legacy overlay uncomment the following line:
		// _scannerControl.SetOverlay(ScannerOverlay.LEGACY);
		_scannerControl.SetLegacyOverlayLocationLine(255, Color.Yellow, 6.0f, true);

		// Optional: DMCC command examples
		_scannerControl.SendCommand("SET DECODER.1D-SYMBOLORIENTATION 0"); // omni
		_scannerControl.SendCommand("SET DECODER.MAX-SCAN-TIMEOUT 10");    // 10s timeout
	}

	private void OnScanButtonClicked(object sender, EventArgs e)
	{
		if (_isScanning)
		{
			_scannerControl.StopScanning();
			_isScanning = false;
			_btnScan.Text = "START SCANNING";
		}
		else
		{
			_scannerControl.StartScanning();
			_isScanning = true;
			_btnScan.Text = "STOP SCANNING";
		}
	}

	// ===== Event handlers (as shown in documentation) =====

	// Called when an MX-1xxx becomes available/unavailable (plug/unplug, power, etc.)
	public async void OnAvailabilityChanged(object sender, ScannerAvailability availability)
	{
		ClearResult();
		if (availability == ScannerAvailability.Available)
		{
			ConnectToScannerDevice();
		}
		else if (availability == ScannerAvailability.Unavailable)
		{
			await DisplayAlert("Device became unavailable", "No connection", "OK");
		}
	}

	// Connect completed (args: ScannerExceptions exception, string errorMessage)
	public void OnConnectionCompleted(object sender, object[] args)
	{
		// If we have a valid connection, error param will be NoException
		if ((ScannerExceptions)args[0] != ScannerExceptions.NoException)
		{
			// Ask for camera permission if necessary (Android only; iOS handled by SDK)
			if ((ScannerExceptions)args[0] == ScannerExceptions.CameraPermissionException)
			{
				_ = RequestCameraPermission();
			}
			else
			{
				Debug.WriteLine(args[1]?.ToString());
				UpdateUIByConnectionState(ScannerConnectionStatus.Disconnected);
			}
		}
	}

	// Connection state changed
	public void OnConnectionStateChanged(object sender, ScannerConnectionStatus status)
	{
		ClearResult();
		if (status == ScannerConnectionStatus.Connected)
		{
			// We just connected, configure the device
			ConfigureScannerDevice();
		}
		_isScanning = false;
		UpdateUIByConnectionState(status);
	}

	// Called after scanning completes (barcode decoded, canceled, or timeout)
	public void OnReadResultReceived(object sender, List<ScannedResult> results)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			ClearResult();
			if (results != null && results.Count > 0)
			{
				_lblResultText.Text = "Result: " + results[0].ResultCode;
				_lblResultType.Text = "Symbology: " + results[0].ResultSymbology;
			}
			_isScanning = false;
			_btnScan.Text = "START SCANNING";
		});
	}

	// Response after sending a DMCC command (args: string payload, string error, string dmcc)
	public void OnResponseReceived(object sender, object[] args)
	{
		if ((string)args[1] != null)
			Debug.WriteLine("Failed to execute DMCC");
		else
			Debug.WriteLine("Response for " + (string)args[2] + ": " + (string)args[0]);
	}

	// Symbology enable/disable result (args: Symbology symbology, bool isEnabled, string error)
	public void OnSymbologyEnabled(object sender, object[] args)
	{
		if ((string)args[2] != null)
			Debug.WriteLine("Failed to enable/disable " + args[0]);
		else
			Debug.WriteLine(((Symbology)args[0]) + (((bool)args[1]) ? " enabled" : " disabled"));
	}

	// Android: request camera permission then reconnect on success
	private async System.Threading.Tasks.Task RequestCameraPermission()
	{
		var result = await Permissions.RequestAsync<Permissions.Camera>();
		// Check result from permission request. If it is allowed by the user, connect to scanner
		if (result == PermissionStatus.Granted)
		{
			_scannerControl.Connect();
		}
		else
		{
			if (Permissions.ShouldShowRationale<Permissions.Camera>())
			{
				if (await DisplayAlert(null, "You need to allow access to the Camera", "OK", "Cancel"))
					await RequestCameraPermission();
			}
		}
	}

	private void ClearResult()
	{
		_lblResultText.Text = "Result: ";
		_lblResultType.Text = "Symbology: ";
	}

	private void UpdateUIByConnectionState(ScannerConnectionStatus status)
	{
		_lblStatus.Text = "Status: " + status;
	}

	// Utility examples for camera zoom (send on demand)
	private void ExampleZoomDmcc()
	{
		_scannerControl.SendCommand("SET CAMERA.ZOOM-PERCENT 250 500");
		_scannerControl.SendCommand("SET CAMERA.ZOOM 2");
	}

	// Reset device to SDK defaults (not factory defaults)
	private void ExampleResetConfig()
	{
		_scannerControl.SendCommand("CONFIG.DEFAULT");
	}
}
