﻿using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace ClipboardMonitorConsoleApp
{
    class ClipboardMonitorForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentProcessId();

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private static string _lastClipboardText = "";

        public ClipboardMonitorForm()
        {
            Console.WriteLine($"Starting clipboard monitor in process {GetCurrentProcessId()}");
            Console.WriteLine($"Running as user: {System.Security.Principal.WindowsIdentity.GetCurrent().Name}");
            
            // Check if we're running in an interactive session
            IntPtr shellWindow = GetShellWindow();
            int shellProcessId;
            GetWindowThreadProcessId(shellWindow, out shellProcessId);
            
            bool isInteractiveSession = shellProcessId != 0;
            Console.WriteLine($"Is interactive session: {isInteractiveSession}");
            
            if (!isInteractiveSession)
            {
                Console.WriteLine("Not running in an interactive session. Clipboard monitoring may not work.");
                Console.WriteLine("Please run the application directly rather than as a service.");
                return;
            }

            bool success = AddClipboardFormatListener(this.Handle);
            if (!success)
            {
                Console.WriteLine("Failed to register clipboard listener!");
            }
            else
            {
                Console.WriteLine("Successfully registered clipboard listener");
            }
            
            // Try to get initial clipboard content
            try
            {
                if (Clipboard.ContainsText())
                {
                    Console.WriteLine("Successfully accessed clipboard");
                    _lastClipboardText = Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing initial clipboard content: {ex.Message}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                Console.WriteLine("Received clipboard update event");
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string currentClipboardText = Clipboard.GetText();
                        Console.WriteLine($"Read clipboard text: {currentClipboardText.Substring(0, Math.Min(20, currentClipboardText.Length))}...");
                        
                        if (currentClipboardText != _lastClipboardText)
                        {
                            _lastClipboardText = currentClipboardText;
                            SendClipboardToServer(currentClipboardText);
                        }
                        else
                        {
                            Console.WriteLine("Clipboard content unchanged, skipping");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Clipboard does not contain text");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading clipboard: {ex.Message}\nStack trace: {ex.StackTrace}");
                }
            }
            base.WndProc(ref m);
        }

        private async void SendClipboardToServer(string content)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(new { content });
                    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    Console.WriteLine("Sending to server...");
                    var response = await client.PostAsync("http://localhost:5001/logs/clipboard", httpContent);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Successfully sent clipboard content: {content.Substring(0, Math.Min(50, content.Length))}...");
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Server returned error {response.StatusCode}: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending to server: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Clipboard monitoring application starting...");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                Application.Run(new ClipboardMonitorForm());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error in clipboard monitor: {ex.Message}\nStack trace: {ex.StackTrace}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}