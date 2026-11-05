// MicKnob: simple controls for your speakers, your microphone and how your voice sounds.
// Mic boost and voice effects go through Equalizer APO.
// Build: build.bat
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumerator { }

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator {
    [PreserveSig] int EnumAudioEndpoints(int flow, int mask, out IMMDeviceCollection collection);
    [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice device);
}
[ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection {
    [PreserveSig] int GetCount(out int count);
    [PreserveSig] int Item(int index, out IMMDevice device);
}
[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice {
    [PreserveSig] int Activate(ref Guid iid, int ctx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
    [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore store);
    [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
}
[StructLayout(LayoutKind.Sequential)] struct PropKey { public Guid Fmt; public int Pid; }
[StructLayout(LayoutKind.Sequential)] struct PropVar { public short Vt; short a, b, c; public IntPtr Ptr; IntPtr d; }
[ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore {
    [PreserveSig] int GetCount(out int n);
    [PreserveSig] int GetAt(int i, out PropKey k);
    [PreserveSig] int GetValue(ref PropKey k, out PropVar v);
}
[ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume {
    [PreserveSig] int RegisterControlChangeNotify(IntPtr n);
    [PreserveSig] int UnregisterControlChangeNotify(IntPtr n);
    [PreserveSig] int GetChannelCount(out int n);
    [PreserveSig] int SetMasterVolumeLevel(float db, ref Guid ctx);
    [PreserveSig] int SetMasterVolumeLevelScalar(float v, ref Guid ctx);
    [PreserveSig] int GetMasterVolumeLevel(out float db);
    [PreserveSig] int GetMasterVolumeLevelScalar(out float v);
    [PreserveSig] int SetChannelVolumeLevel(int ch, float db, ref Guid ctx);
    [PreserveSig] int SetChannelVolumeLevelScalar(int ch, float v, ref Guid ctx);
    [PreserveSig] int GetChannelVolumeLevel(int ch, out float db);
    [PreserveSig] int GetChannelVolumeLevelScalar(int ch, out float v);
    [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid ctx);
    [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
}
[ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")] class PolicyConfigClient { }
// Undocumented, but it's the only way to change the default device. The Windows sound apps use it too.
[ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPolicyConfig {
    [PreserveSig] int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string id, out IntPtr format);
    [PreserveSig] int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, int def, out IntPtr format);
    [PreserveSig] int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id);
    [PreserveSig] int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr a, IntPtr b);
    [PreserveSig] int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, int def, out long a, out long b);
    [PreserveSig] int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr period);
    [PreserveSig] int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
    [PreserveSig] int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
    [PreserveSig] int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr key, IntPtr value);
    [PreserveSig] int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr key, IntPtr value);
    [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
}
[ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioClient {
    [PreserveSig] int Initialize(int share, int flags, long buffer, long period, IntPtr format, IntPtr session);
    [PreserveSig] int GetBufferSize(out uint frames);
    [PreserveSig] int GetStreamLatency(out long latency);
    [PreserveSig] int GetCurrentPadding(out uint frames);
    [PreserveSig] int IsFormatSupported(int share, IntPtr format, out IntPtr closest);
    [PreserveSig] int GetMixFormat(out IntPtr format);
    [PreserveSig] int GetDevicePeriod(out long normal, out long minimum);
    [PreserveSig] int Start();
    [PreserveSig] int Stop();
    [PreserveSig] int Reset();
    [PreserveSig] int SetEventHandle(IntPtr handle);
    [PreserveSig] int GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object o);
}
[ComImport, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioCaptureClient {
    [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong position, out ulong time);
    [PreserveSig] int ReleaseBuffer(uint frames);
    [PreserveSig] int GetNextPacketSize(out uint frames);
}
[ComImport, Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioRenderClient {
    [PreserveSig] int GetBuffer(uint frames, out IntPtr data);
    [PreserveSig] int ReleaseBuffer(uint frames, uint flags);
}

class Device {
    public string Id, Name, ShortName;
    public IAudioEndpointVolume Volume;

    public override string ToString() { return Name; }

    public int Percent {
        get { float v; Audio.Check(Volume.GetMasterVolumeLevelScalar(out v)); return (int)Math.Round(v * 100); }
        set { var ctx = Guid.Empty; Audio.Check(Volume.SetMasterVolumeLevelScalar(value / 100f, ref ctx)); }
    }
    public bool Muted {
        get { bool m; Audio.Check(Volume.GetMute(out m)); return m; }
        set { var ctx = Guid.Empty; Audio.Check(Volume.SetMute(value, ref ctx)); }
    }
}

static class Audio {
    public const int Output = 0, Input = 1;
    static readonly IMMDeviceEnumerator Enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

    public static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }

    static string Property(IPropertyStore store, string fmt, int pid) {
        var key = new PropKey { Fmt = new Guid(fmt), Pid = pid };
        PropVar value;
        Check(store.GetValue(ref key, out value));
        return Marshal.PtrToStringUni(value.Ptr);
    }

    static Device Describe(IMMDevice device) {
        IPropertyStore store;
        Check(device.OpenPropertyStore(0, out store));
        string id;
        Check(device.GetId(out id));
        return new Device {
            Id = id,
            Name = Property(store, "a45c254e-df1c-4efd-8020-67d146a850e0", 14),
            ShortName = Property(store, "026e516e-b814-414b-83cd-856d6fef4822", 2)
        };
    }

    public static List<Device> Active(int flow) {
        var list = new List<Device>();
        IMMDeviceCollection collection;
        Check(Enumerator.EnumAudioEndpoints(flow, 1, out collection));
        int count;
        Check(collection.GetCount(out count));
        for (int i = 0; i < count; i++) {
            IMMDevice device;
            Check(collection.Item(i, out device));
            list.Add(Describe(device));
        }
        return list;
    }

    // Null when there's no device of this kind plugged in.
    public static Device Default(int flow) {
        IMMDevice device;
        if (Enumerator.GetDefaultAudioEndpoint(flow, 0, out device) != 0) return null;
        var result = Describe(device);
        var iid = typeof(IAudioEndpointVolume).GUID;
        object volume;
        Check(device.Activate(ref iid, 1, IntPtr.Zero, out volume));
        result.Volume = (IAudioEndpointVolume)volume;
        return result;
    }

    public static void SetDefault(string id) {
        var policy = (IPolicyConfig)new PolicyConfigClient();
        for (int role = 0; role < 3; role++) Check(policy.SetDefaultEndpoint(id, role));
    }
}

// Plays the default mic, boost and effects included, through the default speakers or headphones.
class MicMonitor {
    const int EventCallback = 0x00040000, AutoConvertPcm = unchecked((int)0x80000000), SrcDefaultQuality = 0x08000000;
    const uint SilentFlag = 2;
    const long BufferDuration = 1000000; // 100 ms, in 100 ns units

    readonly Action<string> failed;
    System.Threading.Thread thread;
    volatile bool stopping;

    public MicMonitor(Action<string> failed) { this.failed = failed; }

    public void Start() {
        stopping = false;
        thread = new System.Threading.Thread(Run) { IsBackground = true };
        thread.SetApartmentState(System.Threading.ApartmentState.MTA);
        thread.Start();
    }

    public void Stop() {
        if (thread == null) return;
        stopping = true;
        thread.Join();
        thread = null;
    }

    [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")] static extern void CopyMemory(IntPtr to, IntPtr from, UIntPtr bytes);

    static IAudioClient OpenClient(IMMDeviceEnumerator enumerator, int flow) {
        IMMDevice device;
        Audio.Check(enumerator.GetDefaultAudioEndpoint(flow, 0, out device));
        var iid = typeof(IAudioClient).GUID;
        object client;
        Audio.Check(device.Activate(ref iid, 1, IntPtr.Zero, out client));
        return (IAudioClient)client;
    }

    static T GetService<T>(IAudioClient client) {
        var iid = typeof(T).GUID;
        object service;
        Audio.Check(client.GetService(ref iid, out service));
        return (T)service;
    }

    void Run() {
        try {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            var micClient = OpenClient(enumerator, Audio.Input);
            var outClient = OpenClient(enumerator, Audio.Output);
            using (var wake = new System.Threading.AutoResetEvent(false)) {
                IntPtr format;
                Audio.Check(micClient.GetMixFormat(out format));
                int rate = Marshal.ReadInt32(format, 4), frameBytes = Marshal.ReadInt16(format, 12);
                Audio.Check(micClient.Initialize(0, EventCallback, BufferDuration, 0, format, IntPtr.Zero));
                Audio.Check(outClient.Initialize(0, AutoConvertPcm | SrcDefaultQuality, BufferDuration, 0, format, IntPtr.Zero));
                Marshal.FreeCoTaskMem(format);
                Audio.Check(micClient.SetEventHandle(wake.SafeWaitHandle.DangerousGetHandle()));

                var capture = GetService<IAudioCaptureClient>(micClient);
                var render = GetService<IAudioRenderClient>(outClient);
                uint bufferFrames, maxQueued = (uint)(rate * 60 / 1000), cushion = (uint)(rate * 20 / 1000);
                Audio.Check(outClient.GetBufferSize(out bufferFrames));
                maxQueued = Math.Min(maxQueued, bufferFrames);

                // A little silence up front keeps playback from running dry between mic packets.
                IntPtr target;
                Audio.Check(render.GetBuffer(cushion, out target));
                Audio.Check(render.ReleaseBuffer(cushion, SilentFlag));
                Audio.Check(outClient.Start());
                Audio.Check(micClient.Start());
                try {
                    while (!stopping) {
                        wake.WaitOne(100);
                        uint pending;
                        Audio.Check(capture.GetNextPacketSize(out pending));
                        while (pending > 0) {
                            IntPtr source; uint frames, flags, queued; ulong position, time;
                            Audio.Check(capture.GetBuffer(out source, out frames, out flags, out position, out time));
                            Audio.Check(outClient.GetCurrentPadding(out queued));
                            // Drop audio rather than letting the delay grow.
                            if (queued + frames <= maxQueued) {
                                Audio.Check(render.GetBuffer(frames, out target));
                                bool silent = (flags & SilentFlag) != 0;
                                if (!silent) CopyMemory(target, source, (UIntPtr)(frames * (uint)frameBytes));
                                Audio.Check(render.ReleaseBuffer(frames, silent ? SilentFlag : 0));
                            }
                            Audio.Check(capture.ReleaseBuffer(frames));
                            Audio.Check(capture.GetNextPacketSize(out pending));
                        }
                    }
                } finally {
                    micClient.Stop();
                    outClient.Stop();
                }
            }
        } catch (Exception ex) {
            failed(ex.Message);
        }
    }
}

// The Equalizer APO file that holds the mic's boost, bass and clarity.
class MicEffects {
    const string ApoClsid = "{EACD2258-FCAC-4FF4-B36D-419E924A6D79}";
    const string FileName = "mic-knob.txt";
    readonly string configDir = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\EqualizerAPO", "ConfigPath", null) as string;

    public int Boost, Bass, Clarity;

    public bool Supports(Device mic) {
        if (configDir == null || mic == null) return false;
        var fx = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture\" + mic.Id.Substring(mic.Id.LastIndexOf('{')) + @"\FxProperties");
        if (fx == null) return false;
        using (fx)
            return fx.GetValueNames().Any(n => {
                var v = fx.GetValue(n) as string;
                return v != null && v.Equals(ApoClsid, StringComparison.OrdinalIgnoreCase);
            });
    }

    static int Find(string text, string pattern) {
        var match = Regex.Match(text, pattern, RegexOptions.Multiline);
        return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
    }

    public void Load() {
        if (configDir == null) return;
        var path = Path.Combine(configDir, FileName);
        if (!File.Exists(path)) return;
        var text = File.ReadAllText(path);
        Boost = Find(text, @"^Preamp:\s*(\d+)");
        Bass = Find(text, @"^Filter: ON LSC .*Gain (\d+)");
        Clarity = Find(text, @"^Filter: ON HSC .*Gain (\d+)");
    }

    // A null mic clears the effects, so they don't stay on a mic that's no longer selected.
    public void Save(Device mic) {
        if (configDir == null) return;
        var lines = new List<string> { "# Written by MicKnob" };
        if (mic != null) {
            lines.Add("Device: " + mic.ShortName);
            lines.Add("Preamp: " + Boost.ToString(CultureInfo.InvariantCulture) + " dB");
            if (Bass > 0) lines.Add("Filter: ON LSC Fc 150 Hz Gain " + Bass.ToString(CultureInfo.InvariantCulture) + " dB Q 0.7");
            if (Clarity > 0) lines.Add("Filter: ON HSC Fc 3500 Hz Gain " + Clarity.ToString(CultureInfo.InvariantCulture) + " dB Q 0.7");
            lines.Add("Device: all");
        }
        var path = Path.Combine(configDir, FileName);
        var text = string.Join(Environment.NewLine, lines) + Environment.NewLine;
        if (!File.Exists(path) || File.ReadAllText(path) != text) File.WriteAllText(path, text);

        var config = Path.Combine(configDir, "config.txt");
        var include = "Include: " + FileName;
        if (!File.ReadAllLines(config).Any(l => l.Trim().Equals(include, StringComparison.OrdinalIgnoreCase)))
            File.AppendAllText(config, Environment.NewLine + include + Environment.NewLine);
    }
}

static class Theme {
    public static readonly Color Back = Color.FromArgb(30, 31, 34);
    public static readonly Color Field = Color.FromArgb(44, 46, 51);
    public static readonly Color Track = Color.FromArgb(62, 64, 70);
    public static readonly Color Accent = Color.FromArgb(88, 196, 148);
    public static readonly Color Hot = Color.FromArgb(235, 140, 70);
    public static readonly Color Dim = Color.FromArgb(150, 153, 160);
    public static readonly StringFormat Center = new StringFormat {
        Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter
    };

    public static float Scale = 1;
    public static int S(float value) { return (int)(value * Scale); }
    public static Font Font(float size, string family = "Segoe UI") { return new Font(family, size * Scale, GraphicsUnit.Pixel); }
}

// Shared behaviour for the knob and the sliders: a value the user can drag, scroll or arrow-key.
abstract class ValueControl : Control {
    int value;
    public int Max = 100;
    public event Action Changed;

    protected ValueControl() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
    }

    public int Value {
        get { return value; }
        set { this.value = Math.Max(0, Math.Min(Max, value)); Invalidate(); }
    }

    protected void SetByUser(int newValue) {
        newValue = Math.Max(0, Math.Min(Max, newValue));
        if (newValue == value || !Enabled) return;
        Value = newValue;
        if (Changed != null) Changed();
    }

    protected virtual int WheelStep { get { return 1; } }

    protected override void OnMouseWheel(MouseEventArgs e) { SetByUser(value + Math.Sign(e.Delta) * WheelStep); }
    protected override bool IsInputKey(Keys key) {
        return key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right || base.IsInputKey(key);
    }
    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Right) SetByUser(value + WheelStep);
        if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Left) SetByUser(value - WheelStep);
    }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
}

class Slider : ValueControl {
    int FromX(int x) {
        float inset = Height / 2f;
        return (int)Math.Round((x - inset) / (Width - inset * 2) * Max);
    }
    protected override void OnMouseDown(MouseEventArgs e) {
        Focus();
        if (e.Button == MouseButtons.Left) SetByUser(FromX(e.X));
    }
    protected override void OnMouseMove(MouseEventArgs e) { if (Capture) SetByUser(FromX(e.X)); }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(Theme.Back);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float inset = Height / 2f, y = Height / 2f, line = Theme.S(5);
        float x = inset + (Width - inset * 2) * Value / Max;
        using (var pen = new Pen(Theme.Track, line) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawLine(pen, inset, y, Width - inset, y);
        var color = Enabled ? Theme.Accent : Theme.Dim;
        if (Value > 0)
            using (var pen = new Pen(color, line) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(pen, inset, y, x, y);
        float r = Height * 0.32f;
        using (var brush = new SolidBrush(Enabled ? Color.White : Theme.Dim))
            g.FillEllipse(brush, x - r, y - r, r * 2, r * 2);
    }
}

// 0 to 100 is the Windows mic level. Past 100, each step adds 1 dB of boost.
class Knob : ValueControl {
    public const int FullLevel = 100, HotLevel = 120;
    const float StartAngle = 135, SweepAngle = 270; // GDI+ degrees, clockwise from 3 o'clock
    float dragStartY, dragStartValue;

    protected override void OnMouseDown(MouseEventArgs e) {
        Focus();
        dragStartY = e.Y;
        dragStartValue = Value;
    }
    protected override void OnMouseMove(MouseEventArgs e) {
        if (Capture) SetByUser((int)Math.Round(dragStartValue + (dragStartY - e.Y) * 200f / (Height * 1.3f)));
    }
    protected override void OnMouseDoubleClick(MouseEventArgs e) { SetByUser(FullLevel); }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(Theme.Back);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        float u = Width / 170f, cx = Width / 2f, cy = Height / 2f, arcR = 74 * u, knobR = 58 * u;
        float fraction = Value / (float)Max;
        var color = !Enabled ? Theme.Dim : Value > HotLevel ? Theme.Hot : Theme.Accent;
        var arc = new RectangleF(cx - arcR, cy - arcR, arcR * 2, arcR * 2);

        using (var pen = new Pen(Theme.Track, 8 * u) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(pen, arc, StartAngle, SweepAngle);
        if (Value > 0)
            using (var pen = new Pen(color, 8 * u) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(pen, arc, StartAngle, SweepAngle * fraction);

        var knob = new RectangleF(cx - knobR, cy - knobR, knobR * 2, knobR * 2);
        using (var fill = new LinearGradientBrush(knob, Color.FromArgb(78, 81, 88), Color.FromArgb(44, 46, 51), 90f))
            g.FillEllipse(fill, knob);

        double angle = (StartAngle + SweepAngle * fraction) * Math.PI / 180;
        float dotR = knobR - 11 * u, dot = 5 * u;
        using (var brush = new SolidBrush(color))
            g.FillEllipse(brush, cx + dotR * (float)Math.Cos(angle) - dot, cy + dotR * (float)Math.Sin(angle) - dot, dot * 2, dot * 2);

        var big = Value > FullLevel ? "+" + (Value - FullLevel) + " dB" : Value + "%";
        var small = Value > FullLevel ? "Boost" : "Mic volume";
        using (var font = Theme.Font(20, "Segoe UI Semibold"))
            g.DrawString(big, font, Brushes.White, new PointF(cx, cy - 4 * u), Theme.Center);
        using (var font = Theme.Font(11))
        using (var brush = new SolidBrush(Color.Gainsboro))
            g.DrawString(small, font, brush, new PointF(cx, cy + 17 * u), Theme.Center);
    }
}

class Pill : Control {
    bool on;
    public Color OnColor = Theme.Accent;

    public Pill() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand;
        Font = Theme.Font(13);
    }

    public bool On {
        get { return on; }
        set { if (on != value) { on = value; Invalidate(); } }
    }

    protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
    protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics;
        g.Clear(Theme.Back);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using (var path = new GraphicsPath()) {
            float d = Height - 1;
            path.AddArc(0, 0, d, d, 90, 180);
            path.AddArc(Width - 1 - d, 0, d, d, 270, 180);
            path.CloseFigure();
            using (var brush = new SolidBrush(on && Enabled ? OnColor : Enabled ? Theme.Track : Theme.Field))
                g.FillPath(brush, path);
        }
        using (var brush = new SolidBrush(!Enabled ? Theme.Dim : on ? Theme.Back : Color.White))
            g.DrawString(Text, Font, brush, new RectangleF(0, 0, Width, Height), Theme.Center);
    }
}

class SoundForm : Form {
    const int MaxBoost = 30;
    static readonly int[,] Presets = { { 0, 0 }, { 8, 0 }, { 0, 6 }, { 6, 6 } }; // bass, clarity
    static readonly string[] PresetNames = { "Natural", "Deep", "Clear", "Full" };

    readonly MicEffects effects = new MicEffects();
    readonly MicMonitor monitor;
    readonly Timer saveTimer = new Timer { Interval = 150 };
    readonly Timer pollTimer = new Timer { Interval = 500 };
    readonly ToolTip tips = new ToolTip();

    readonly ComboBox outputList, micList;
    readonly Slider outputSlider = new Slider(), bassSlider = new Slider { Max = 12 }, claritySlider = new Slider { Max = 12 };
    readonly Label outputLabel, bassLabel, clarityLabel, status;
    readonly Pill outputMute = new Pill { OnColor = Theme.Hot }, micMute = new Pill { OnColor = Theme.Hot };
    readonly Pill hearButton = new Pill { Text = "Hear myself" };
    readonly Knob knob = new Knob();
    readonly Pill[] presetPills = new Pill[Presets.GetLength(0)];

    Device output, mic;
    List<Device> outputs = new List<Device>(), mics = new List<Device>();
    bool micSupported, hearing;

    public SoundForm() {
        using (var g = CreateGraphics()) Theme.Scale = g.DpiX / 96;
        Text = "Sound Controls";
        BackColor = Theme.Back;
        ForeColor = Color.White;
        Font = Theme.Font(13);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Theme.S(380), Theme.S(620));

        AddHeader("What you hear", 12);
        outputList = AddList(38);
        outputLabel = AddLabel("", 16, 76, 96);
        Place(outputSlider, 112, 74, 176, 26);
        Place(outputMute, 296, 72, 68, 30);
        AddDivider(112);

        AddHeader("Your microphone", 122);
        micList = AddList(148);
        Place(knob, 105, 184, 170, 170);
        Place(micMute, 16, 360, 168, 34);
        Place(hearButton, 196, 360, 168, 34);
        AddDivider(406);

        AddHeader("How your voice sounds", 416);
        for (int i = 0; i < presetPills.Length; i++) {
            int preset = i;
            presetPills[i] = new Pill { Text = PresetNames[i] };
            presetPills[i].Click += (s, e) => SetVoice(Presets[preset, 0], Presets[preset, 1]);
            Place(presetPills[i], 16 + i * 89, 442, 81, 30);
        }
        bassLabel = AddLabel("", 16, 484, 140);
        Place(bassSlider, 156, 482, 208, 26);
        clarityLabel = AddLabel("", 16, 516, 140);
        Place(claritySlider, 156, 514, 208, 26);
        status = AddLabel("", 16, 548, 348);
        status.Height = Theme.S(60);
        status.ForeColor = Theme.Dim;

        tips.SetToolTip(hearButton, "Plays your mic back to you. Use headphones, or it can squeal.");
        tips.SetToolTip(knob, "Drag up or down, or scroll. Past 100% adds extra boost. Double-click to go back to 100%.");
        tips.SetToolTip(bassSlider, "Adds low tones, for a fuller, deeper voice.");
        tips.SetToolTip(claritySlider, "Adds high tones, for crisper words. Can also make hiss louder.");

        outputList.SelectionChangeCommitted += (s, e) => PickDevice(outputList, outputs);
        micList.SelectionChangeCommitted += (s, e) => PickDevice(micList, mics);
        outputSlider.Changed += () => { if (output != null) output.Percent = outputSlider.Value; ShowValues(); };
        outputMute.Click += (s, e) => { if (output != null) output.Muted = !output.Muted; Poll(); };
        micMute.Click += (s, e) => { if (mic != null) mic.Muted = !mic.Muted; Poll(); };
        hearButton.Click += (s, e) => SetHearing(!hearing);
        knob.Changed += KnobChanged;
        bassSlider.Changed += () => SetVoice(bassSlider.Value, claritySlider.Value);
        claritySlider.Changed += () => SetVoice(bassSlider.Value, claritySlider.Value);

        saveTimer.Tick += (s, e) => { saveTimer.Stop(); SaveEffects(); };
        pollTimer.Tick += (s, e) => Poll();
        monitor = new MicMonitor(message => {
            try {
                BeginInvoke(new Action(() => {
                    SetHearing(false);
                    MessageBox.Show(this, message, "Couldn't play your mic");
                }));
            } catch (InvalidOperationException) { } // the window is already closing
        });

        effects.Load();
        Poll();
        ActiveControl = knob;
        pollTimer.Start();
    }

    void Place(Control control, int x, int y, int w, int h) {
        control.SetBounds(Theme.S(x), Theme.S(y), Theme.S(w), Theme.S(h));
        Controls.Add(control);
    }

    Label AddLabel(string text, int x, int y, int w) {
        var label = new Label { Text = text, AutoSize = false, TextAlign = ContentAlignment.TopLeft };
        Place(label, x, y, w, 24);
        return label;
    }

    void AddHeader(string text, int y) {
        var label = AddLabel(text, 16, y, 348);
        label.Font = Theme.Font(15, "Segoe UI Semibold");
    }

    void AddDivider(int y) { Place(new Label { BackColor = Theme.Track }, 16, y, 348, 1); }

    ComboBox AddList(int y) {
        var list = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Field, ForeColor = Color.White
        };
        list.SetBounds(Theme.S(16), Theme.S(y), Theme.S(348), 0);
        Controls.Add(list);
        return list;
    }

    // Keeps the window in step with Windows: new devices, volume keys, changes made in other apps.
    void Poll() {
        try {
            bool outputChanged = Refresh(outputList, Audio.Output, ref outputs, ref output);
            bool micChanged = Refresh(micList, Audio.Input, ref mics, ref mic);
            if (micChanged) MicChanged();
            if ((outputChanged || micChanged) && hearing) {
                monitor.Stop();
                if (output != null && mic != null) monitor.Start(); else SetHearing(false);
            }
            SyncValues();
        } catch (COMException) {
            output = null; // a device just went away, so the next poll starts fresh
            mic = null;
        }
    }

    bool Refresh(ComboBox list, int flow, ref List<Device> known, ref Device current) {
        if (list.DroppedDown) return false;
        var found = Audio.Active(flow);
        var now = Audio.Default(flow);
        bool listChanged = !found.Select(d => d.Id + d.Name).SequenceEqual(known.Select(d => d.Id + d.Name));
        bool defaultChanged = (now == null ? null : now.Id) != (current == null ? null : current.Id);
        if (listChanged) {
            known = found;
            list.Items.Clear();
            foreach (var d in found) list.Items.Add(d);
        }
        if (listChanged || defaultChanged) {
            current = now;
            var id = now == null ? null : now.Id;
            list.SelectedIndex = known.FindIndex(d => d.Id == id);
        }
        return defaultChanged;
    }

    void MicChanged() {
        micSupported = effects.Supports(mic);
        knob.Enabled = micMute.Enabled = hearButton.Enabled = mic != null;
        knob.Max = micSupported ? Knob.FullLevel + MaxBoost : Knob.FullLevel;
        bassSlider.Enabled = claritySlider.Enabled = micSupported;
        foreach (var pill in presetPills) pill.Enabled = micSupported;
        status.Text = mic == null ? "No microphone found. Plug one in and it will show up here."
            : micSupported ? "Your settings stay on after you close this window."
            : "Extra boost and voice effects aren't turned on for this mic. To add them, open Equalizer APO's Device Selector, tick this mic, then restart your PC.";
        SaveEffects(); // the boost and effects follow the selected mic
    }

    void SyncValues() {
        if (output != null) {
            if (!outputSlider.Capture) outputSlider.Value = output.Percent;
            outputMute.On = output.Muted;
        }
        outputSlider.Enabled = outputMute.Enabled = output != null;
        outputMute.Text = outputMute.On ? "Muted" : "Mute";

        if (mic != null) {
            int level = mic.Percent;
            // If something else turned the mic down, there's no point keeping the boost on top.
            if (level < Knob.FullLevel && effects.Boost > 0) { effects.Boost = 0; QueueSave(); }
            if (!knob.Capture) knob.Value = level < Knob.FullLevel ? level : Knob.FullLevel + (micSupported ? effects.Boost : 0);
            micMute.On = mic.Muted;
        }
        micMute.Text = micMute.On ? "Mic muted" : "Mute mic";
        hearButton.On = hearing;
        hearButton.Text = hearing ? "Hearing myself" : "Hear myself";
        bassSlider.Value = effects.Bass;
        claritySlider.Value = effects.Clarity;
        ShowValues();
    }

    void ShowValues() {
        outputLabel.Text = "Volume " + outputSlider.Value + "%";
        bassLabel.Text = "Bass (deeper): " + bassSlider.Value;
        clarityLabel.Text = "Clarity (crisper): " + claritySlider.Value;
        for (int i = 0; i < presetPills.Length; i++)
            presetPills[i].On = effects.Bass == Presets[i, 0] && effects.Clarity == Presets[i, 1];
    }

    void PickDevice(ComboBox list, List<Device> devices) {
        if (list.SelectedIndex < 0) return;
        try { Audio.SetDefault(devices[list.SelectedIndex].Id); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Couldn't switch device"); }
        ActiveControl = knob; // so arrow keys and scrolling don't switch devices by accident
        Poll();
    }

    void KnobChanged() {
        if (mic == null) return;
        mic.Percent = Math.Min(knob.Value, Knob.FullLevel);
        effects.Boost = Math.Max(0, knob.Value - Knob.FullLevel);
        QueueSave();
    }

    void SetVoice(int bass, int clarity) {
        effects.Bass = bass;
        effects.Clarity = clarity;
        bassSlider.Value = bass;
        claritySlider.Value = clarity;
        ShowValues();
        QueueSave();
    }

    void SetHearing(bool on) {
        hearing = on;
        if (on) monitor.Start(); else monitor.Stop();
        hearButton.On = on;
        hearButton.Text = on ? "Hearing myself" : "Hear myself";
    }

    void QueueSave() {
        saveTimer.Stop();
        saveTimer.Start();
    }

    void SaveEffects() {
        try { effects.Save(micSupported ? mic : null); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Couldn't save your mic settings"); }
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
        pollTimer.Stop();
        if (saveTimer.Enabled) { saveTimer.Stop(); SaveEffects(); }
        monitor.Stop();
        base.OnFormClosing(e);
    }

    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main() {
        SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.Run(new SoundForm());
    }
}
