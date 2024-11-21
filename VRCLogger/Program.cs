using Valve.VR;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRChat.API.Api;
using VRChat.API.Client;
using VRChat.API.Model;
using File = System.IO.File;
using Newtonsoft.Json;
using static VRCLogger.Confwig;
namespace VRCLogger
{
    public static class Program
    {
      

        public static List<string> PlayerList;
        public static List<string> logfile = new List<string>();
        public static  int NotifTime;
       public static HmdMatrix34_t PosMatrix;
        public static SolidBrush TextBrush;
        public static SolidBrush BackgroundBrush;
        public static int FontSize;
       public static Configuration config;
        public static ApiClient client;
        public static bool IsGui = false;
        public static AuthenticationApi authApi;
        public static UsersApi userApi;
        public static AvatarsApi avatarApi;
        public static PlayermoderationApi modapi;
        public static CVRSystem HMD;
        public static ulong OverlayHandle;
        public static bool VrNotif = false;

        public static bool Running;
        public static void SaveConfig(Config config, string filePath)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        public static Config LoadConfigFile(string filePath) 
        { 
            if (File.Exists(filePath)) 
            { 
                var json = File.ReadAllText(filePath);
                try
                {
                    return JsonConvert.DeserializeObject<Config>(json);

                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            } 
            throw new FileNotFoundException("Configuration file not found."); 
        }
        public static Config cfg;
        public static void LoadConfig()
        {
            
            NotifTime = Convert.ToInt32(3500);
            PosMatrix.m0 = (float)Convert.ToDouble(2);
            PosMatrix.m1 = (float)Convert.ToDouble(0); 
            PosMatrix.m2 = (float)Convert.ToDouble(0);
            PosMatrix.m3 = (float)Convert.ToDouble(cfg.OverlaySettings.Position.X); //lower moves to left and higher moves to right
            PosMatrix.m4 = (float)Convert.ToDouble(0);
            PosMatrix.m5 = (float)Convert.ToDouble(1);
            PosMatrix.m6 = (float)Convert.ToDouble(2.25);
            PosMatrix.m7 = (float)Convert.ToDouble(cfg.OverlaySettings.Position.Y); //lower moves to bottom and higher moves to upper
            PosMatrix.m8 = (float)Convert.ToDouble(0);
            PosMatrix.m9 = (float)Convert.ToDouble(0);
            PosMatrix.m10 = (float)Convert.ToDouble(1);
            PosMatrix.m11 = (float)Convert.ToDouble(cfg.OverlaySettings.Position.Z); // lower moves closer and higher moves further


            TextBrush = new SolidBrush(Color.FromArgb(cfg.OverlaySettings.TextColor.Alpha, cfg.OverlaySettings.TextColor.Red, cfg.OverlaySettings.TextColor.Green, cfg.OverlaySettings.TextColor.Blue));
            BackgroundBrush = new SolidBrush(Color.FromArgb(cfg.OverlaySettings.BackgroundColor.Alpha, cfg.OverlaySettings.BackgroundColor.Red, cfg.OverlaySettings.BackgroundColor.Green, cfg.OverlaySettings.BackgroundColor.Blue));
            FontSize = Convert.ToInt32(cfg.OverlaySettings.FontSize);
        }
        public static void Main(string[] args)
        {

            cfg = LoadConfigFile(Environment.CurrentDirectory + "\\Config.json");
            PlayerList = new List<string>();
            if (!File.Exists(Environment.CurrentDirectory + "\\avatarid.txt"))
            {
                File.Create(Environment.CurrentDirectory + "\\avatarid.txt");
            }
            if (!File.Exists(Environment.CurrentDirectory + "\\DB.txt"))
            {
                File.Create(Environment.CurrentDirectory + "\\DB.txt");
            }
            if (!File.Exists(Environment.CurrentDirectory + "\\Login.txt"))
            {
                File.Create(Environment.CurrentDirectory + "\\Login.txt");
            }
            if (cfg.GeneralSettings.FirstTime == true)
            {
                MessageBox.Show("It is recommended that you delete every folder in your vrchat cache folder to ensure that you do not get rate limited by the api (the folder in question will open)");
                Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow") + "\\VRChat\\VRChat\\Cache-WindowsPlayer");
                cfg.GeneralSettings.FirstTime = false;
                SaveConfig(cfg, Environment.CurrentDirectory + "\\Config.json");

            }
            foreach (var file in System.IO.Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow") + "\\VRChat\\VRChat\\"))
            {
                if (file.EndsWith(".txt") && Regex.IsMatch(file, @"output_log_\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}") == true)
                {
                    var time = System.IO.File.GetCreationTime(file);
                    if (time > lastfilecreationdate)
                    {
                        lastfilecreationdate = time;
                        currentlog = file;
                    }

                }

            }
            VrNotif = cfg.GeneralSettings.Vr;
            if (cfg.GeneralSettings.AvatarLog == true)
            {
                config = new Configuration();
                client = new ApiClient();
                config.UserAgent = "VRCLogger/0.0.1 LogYes";
                if (string.IsNullOrEmpty(System.IO.File.ReadAllText(Environment.CurrentDirectory + "\\Login.txt")) == true)
                {
                    MessageBox.Show("Please put your current vrc login in the Login.txt");

                    Console.WriteLine("Please put your current vrc login in the Login.txt");

                    Console.ReadKey();
                    Environment.Exit(0);
                }
                var loginFile = System.IO.File.ReadAllLines(Environment.CurrentDirectory + "\\Login.txt");
                var login = loginFile.First().Split(':');

                config.Username = login[0];
                config.Password = login[1];
                authApi = new AuthenticationApi(client, client, config);
                userApi = new UsersApi(client, client, config);
                modapi = new PlayermoderationApi(config);
                avatarApi = new AvatarsApi(client, client, config);

                ApiResponse<CurrentUser> currentUserResp = authApi.GetCurrentUserWithHttpInfo();
                if (currentUserResp.RawContent.Contains("emailOtp"))
                {
                    System.Windows.Forms.MessageBox.Show("Enter your email 2fa code in the console");
                    Console.WriteLine("enter 2fa code");
                    var code = Console.ReadLine();
                    authApi.Verify2FAEmailCode(new TwoFactorEmailCode(code));

                }
                CurrentUser currentUser = authApi.GetCurrentUser();
            }
      

            if (VrNotif == true)
            {
                LoadConfig();

                EVRInitError peError = EVRInitError.None;
                HMD = OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Overlay);
                EVROverlayError overlayError = OpenVR.Overlay.CreateOverlay("VRCLogger", "VRCLogger", ref OverlayHandle);
                if (overlayError == EVROverlayError.None)
                {
                    OpenVR.Overlay.SetOverlayWidthInMeters(OverlayHandle, 1f);
                    OpenVR.Overlay.SetOverlayTransformTrackedDeviceRelative(OverlayHandle, OpenVR.k_unTrackedDeviceIndex_Hmd, ref PosMatrix);
                    OpenVR.Overlay.SetOverlayAlpha(OverlayHandle, 0.7f);
                }
                ShowNotification("Hello User");


            }

            foreach (var line in System.IO.File.ReadLines(Environment.CurrentDirectory + "\\db.txt"))
            {
                db.Add(line);
            }
            foreach (var line in System.IO.File.ReadLines(Environment.CurrentDirectory + "\\avatarid.txt"))
            {
                avilog.Add(line);
            }
            CheckLog();

        }

        public static DateTime timewhenstarted = DateTime.Now;
        public static System.DateTime lastfilecreationdate = DateTime.MinValue;
        public static string currentlog = "";
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        public static System.Timers.Timer timer;
        public static List<string> avilog = new List<string>();
        public static bool IsScanning = false;
        public static List<string> db = new List<string>();

        public static DateTime ExtractDate(string line)
        {
            string pattern = @"Date:(\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2}:\d{2} (AM|PM))";
            Match match = Regex.Match(line, pattern);
            if (match.Success)
            {
                return DateTime.ParseExact(match.Groups[1].Value, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
            }
            else
            {
                throw new FormatException("Invalid date format in line: " + line);
            }
        }

        public static readonly Dictionary<Guid, CancellationTokenSource> _setTimeoutHandles = new Dictionary<Guid, CancellationTokenSource>();
        public static void ShowNotification(string msg) 
        { 
            string strImgPath = Environment.CurrentDirectory + "\\Msg.png";
            try
            {
                using (Bitmap bitmap = new Bitmap(4096, 2048))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        Font textFont = new Font("Arial", 48);
                        SizeF textSize = g.MeasureString(msg, textFont);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.FillRectangle(BackgroundBrush, new Rectangle(0, 0, (int)textSize.Width + 20, (int)textSize.Height + 20));
                        RectangleF rectf = new RectangleF(10, 10, textSize.Width, textSize.Height);
                        g.DrawString(msg, textFont, TextBrush, rectf);
                        g.Flush();
                    }
                    bitmap.Save(strImgPath);
                }
            }
            catch 
            {
                Thread.Sleep(500);

                Console.WriteLine("retrying...");
            }

          
            OpenVR.Overlay.SetOverlayFromFile(OverlayHandle, strImgPath);
            OpenVR.Overlay.ShowOverlay(OverlayHandle);
            ClearAllTimeout();
            SetTimeout(() => 
            { 
                OpenVR.Overlay.HideOverlay(OverlayHandle);
            }, 3000); 
        }
        public static void retry(string path, string msg)
        {
            Thread.Sleep(500);

            try
            {
                using (Bitmap bitmap = new Bitmap(4096, 2048))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        Font textFont = new Font("Arial", 48);
                        SizeF textSize = g.MeasureString(msg, textFont);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.FillRectangle(BackgroundBrush, new Rectangle(0, 0, (int)textSize.Width + 20, (int)textSize.Height + 20));
                        RectangleF rectf = new RectangleF(10, 10, textSize.Width, textSize.Height);
                        g.DrawString(msg, textFont, TextBrush, rectf);
                        g.Flush();
                    }
                    bitmap.Save(path);
                }
            }
            catch
            {
                Thread.Sleep(500);

                Console.WriteLine("retrying...");


            }
        }
        public static Guid SetTimeout(Action cb, int delay) 
        {
            return SetTimeout(cb, delay, null); 
        }
        public static Guid SetTimeout(Action cb, int delay, Form uiForm) 
        { 
            Guid g = Guid.NewGuid(); 
            var cts = new CancellationTokenSource(); 
            var token = cts.Token; 
            _setTimeoutHandles.Add(g, cts);
            Task.Run(async () => {
                try 
                {
                    await Task.Delay(delay, token);
                    _setTimeoutHandles.Remove(g);
                    if (uiForm != null) 
                    {
                        uiForm.Invoke(cb); 
                    } else 
                    {
                        cb(); 
                    } 
                } 
                catch (TaskCanceledException) 
                { 
                } 
            }, token); 
            return g; 
        }
        public static void ClearTimeout(Guid g) 
        {
            if (!_setTimeoutHandles.ContainsKey(g))
                return; 
            _setTimeoutHandles[g].Cancel();
            _setTimeoutHandles.Remove(g); 
        }
        public static void ClearAllTimeout() 
        { 
            foreach (var handle in _setTimeoutHandles.Values)
            { 
                handle.Cancel(); 
            } 
            _setTimeoutHandles.Clear(); 
        }
        public static void checkavi()
        {
            foreach (var dir in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow") + "\\VRChat\\VRChat\\Cache-WindowsPlayer"))
            {

                var s = dir.Replace(System.IO.Path.GetDirectoryName(dir) + "\\", "");
                if (!db.Contains(s))
                {
                    string pattern = @"avtr_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
                    string patternwrld = @"wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}";
                    Regex regex = new Regex(pattern);
                    Regex regexwrld = new Regex(patternwrld);

                    string vrca = "";
                    using (StreamReader file = new StreamReader(Directory.GetDirectories(dir).First() + "\\__data"))
                    {
                        IsScanning = true;
                        while ((vrca = file.ReadLine()) != null && IsScanning == true)
                        {
                            if (regexwrld.IsMatch(vrca))
                            {
                                db.Add(s);
                                StringBuilder sb = new StringBuilder();

                                Console.WriteLine("Skipping because its a vrcworld");
                                foreach (var item in db)
                                {
                                    sb.Append(item + "\n");
                                }
                                System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\DB.txt", sb.ToString() + "\n");

                                IsScanning = false;
                            }
                            if (regex.IsMatch(vrca) && IsScanning == true)
                            {
                                var id = regex.Matches(vrca)[0].Value;
                                //   textbox69.Text +=  $"\nNew Avatar Logged:\nFileID:{s}\nAvatarID:{id}.";
                                db.Add(s);
                                var name = "";
                                var asseturl = "";
                                try
                                {


                                    var av = avatarApi.GetAvatar(id);

                                    Console.WriteLine($"Logged avatar: {av.Name} ({av.Id})\nFileID: {s}");
                                    if (av.Name != null && av.ImageUrl != null)
                                    {
                                        name = av.Name;

                                        asseturl = av.ImageUrl;
                                    }
                                    if (VrNotif == true)
                                        ShowNotification($"Logged avatar: {av.Name} ({av.Id}) {av.ImageUrl}");
                                }
                                catch (VRChat.API.Client.ApiException e)
                                {
                                    if (e.ErrorCode == 404)
                                    {
                                        Console.WriteLine($"Logged avatar:{id}\nFileID: {s}");

                                        Console.WriteLine("Private Avatar");
                                    }
                                    else
                                    {
                                        Console.WriteLine(e);

                                    }
                                    name = "null (PRIVATE)";
                                    asseturl = "";
                                }
                                avilog.Add("Folder: " + s + $" AvatarID: {id} Name: {name} ({asseturl})" + $" Date:{System.IO.File.GetCreationTime(Directory.GetDirectories(dir).First() + "\\__data")}\n");
                                if (VrNotif == true)
                                    ShowNotification($"Logged avatar: {name} ({id})");
                                //  textbox69.Text = textbox69.Text + "FileID: " + s + " AvatarID: " + id + $" Date:{DateTime.Now}\n";
                                StringBuilder sb = new StringBuilder();
                                StringBuilder sbavi = new StringBuilder();
                                avilog.Sort((x, y) => { DateTime dateX; DateTime dateY; try { dateX = ExtractDate(x); } catch (FormatException) { dateX = DateTime.MinValue; } try { dateY = ExtractDate(y); } catch (FormatException) { dateY = DateTime.MinValue; } return dateX.CompareTo(dateY); });
                                foreach (var avatar in avilog)
                                {
                                    if (!string.IsNullOrEmpty(avatar))
                                    {
                                        sbavi.Append(avatar + "\n");
                                    }
                                }
                                foreach (var item in db)
                                {
                                    sb.Append(item + "\n");
                                }
                                System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\DB.txt", sb.ToString() + "\n");
                                System.IO.File.WriteAllText(Environment.CurrentDirectory + "\\avatarid.txt", sbavi.ToString() + "\n");
                                // Console.WriteLine($"saved:{sb.ToString()}");
                                IsScanning = false;
                            }
                        }
                    }

                }

            }
        }
    public static Task CheckLog()
        {
            AllocConsole();
            while (true)
            {
                if (cfg.GeneralSettings.JoinLog == true)
                {
                    var dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow") + "\\VRChat\\VRChat\\";


                    string outputlog = "";
                    StreamReader sr;
                    var Copied = currentlog.Replace("output_log", "joinlog_");


                    if (System.IO.File.Exists(Copied))
                    {
                        System.IO.File.Delete(Copied);
                    }
                    System.IO.File.Copy(currentlog, Copied);

                    using (FileStream stream = new FileStream(Copied, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {

                        using (sr = new StreamReader(stream))
                        {
                            while ((outputlog = sr.ReadLine()) != null)
                            {
                                var onJoin = Regex.Match(outputlog, @"(\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2}).* \[Behaviour\] OnPlayerJoined (.+)");
                                var onLeave = Regex.Match(outputlog, @"(\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2}).* \[Behaviour\] OnPlayerLeft (.+)");
                                if (!PlayerList.Contains(onJoin.Value) && !PlayerList.Contains(onLeave.Value))
                                {
                                    if (onLeave.Success)
                                    {
                                        string timestamp = onLeave.Groups[1].Value;
                                        DateTime loggedAt = DateTime.ParseExact(timestamp, "yyyy.MM.dd HH:mm:ss", null);
                                        if (loggedAt >= timewhenstarted)
                                        {
                                            string playerName = onLeave.Groups[2].Value.Trim();
                                            Console.WriteLine(playerName + " has left " + loggedAt + "\n");
                                            if (VrNotif == true)
                                            {
                                                ShowNotification(playerName.Split('(')[0] + "has left");

                                            }

                                            PlayerList.Add(onLeave.Value);
                                        }
                                    }

                                    if (onJoin.Success)
                                    {

                                        string timestamp = onJoin.Groups[1].Value;
                                        DateTime loggedAt = DateTime.ParseExact(timestamp, "yyyy.MM.dd HH:mm:ss", null);
                                        if (loggedAt >= timewhenstarted)
                                        {
                                            string playerName = onJoin.Groups[2].Value.Trim();
                                            Console.WriteLine(playerName + " has joined " + loggedAt + "\n");
                                            if (VrNotif == true)
                                            {
                                                ShowNotification(playerName.Split('(')[0] + "has joined");
                                            }
                                            logfile.Add(playerName + " has joined " + loggedAt + "\n");
                                            PlayerList.Add(onJoin.Value);

                                        }
                                    }

                                }

                            }
                            sr.Close();
                        }
                        stream.Close();
                    }
                }
                if (cfg.GeneralSettings.AvatarLog == true && IsGui == false)
                {
                    checkavi();
                }
                else if (cfg.GeneralSettings.AvatarLog == true && IsGui == true)
                {
                }

            }
            return Task.Delay(100);

        }

    }
}
