using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Valve.VR;
using VRChat.API.Api;
using VRChat.API.Client;
using VRChat.API.Model;
using VRCLogger;
using static VRCLogger.Program;
namespace VRCLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //  Thread.Sleep(1000);
            //   Init();

        }
        public static Task CheckLog()
        {
           // AllocConsole();
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
                if (cfg.GeneralSettings.AvatarLog == true)
                {
                    var s = Task.Run(checkavi);
                    
                }
              

            }
            return Task.Delay(100);

        }
        public static Task checkavi()
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
            return Task.CompletedTask;
        }
        static void init()
        {
            IsGui = true;
            cfg = LoadConfigFile(Environment.CurrentDirectory + "\\Config.json");
            PlayerList = new List<string>();
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
                    MessageBox.Show("Enter your email 2fa code in the console");
                    Console.WriteLine("enter 2fa code");
                    var code = Console.ReadLine();
                    authApi.Verify2FAEmailCode(new TwoFactorEmailCode(code));

                }
                CurrentUser currentUser = authApi.GetCurrentUser();
                Console.WriteLine($"Logged in as {currentUser.DisplayName}");
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
            Task.Run(MainWindow.CheckLog);

        }

        private void FileIdInput_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string avinfo = "";
          //  string image = "";
            try
            {
                var thing = Program.avatarApi.GetAvatar(Input.Text);
                 avinfo = $"Name:{thing.Name}\nAuthor:{thing.AuthorName}\nVersion:{thing._Version}\nImageUrl:{thing.ImageUrl}";
               // image = $"{thing.ImageUrl}";
            }
            catch (Exception ex) 
            {
                 avinfo = "private avatar";
            }
            Console.WriteLine(avinfo);
            MessageBox.Show(avinfo);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (cfg.GeneralSettings.AvatarLog == true)
                Program.avatarApi.SelectAvatar(Input.Text);
            else
            {
                Console.WriteLine("enable avatarlog in the config and restart the program");
                MessageBox.Show("enable avatarlog in the config and restart the program");
            }
        }

        private void textbox69_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AllocConsole();
            init();
        }
    }
}