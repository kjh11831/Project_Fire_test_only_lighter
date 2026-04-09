using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System;
using System.Runtime.InteropServices; // 추가: Win32 API 사용
using System.Windows.Forms;
using static Project_Fire.Form2;

namespace Project_Fire
{
    public partial class Form1 : Form
    {
        private Form2 form2;
        private IFirebaseClient client;

        // --- [전역 핫키 등록을 위한 Win32 API 선언] ---
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        const int HOTKEY_ID = 31197; // 핫키 고유 ID (중복되지 않는 아무 숫자나 가능)

        public Form1()
        {
            InitializeComponent();
            button1.Click += button1_Click;

            // 폼이 로드될 때 F9 키를 전역 핫키로 등록 (0은 조합키 없음)
            this.Load += (s, e) => {
                RegisterHotKey(this.Handle, HOTKEY_ID, 0, (int)Keys.F9);
            };

            // 폼이 닫힐 때 핫키 해제
            this.FormClosing += (s, e) => {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
            };
        }

        // --- [윈도우 메시지 처리: 핫키 감지] ---
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312) // WM_HOTKEY 메시지 번호
            {
                if (m.WParam.ToInt32() == HOTKEY_ID)
                {
                    // 다른 창이 활성화되어 있어도 F9를 누르면 버튼 클릭 이벤트 실행
                    button1.PerformClick();
                }
            }
        }

        IFirebaseConfig Config = new FirebaseConfig
        {
            AuthSecret = "6zja7ZvLbRxEuuTmvulUobmmelYMVnWHWz8g4fCp",
            BasePath = "https://projectfire-e50e7-default-rtdb.firebaseio.com/"
        };

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                client = new FireSharp.FirebaseClient(Config);
                if (client == null) return;

                FirebaseResponse response1 = await client.GetAsync("arduino1");
                FirebaseResponse response2 = await client.GetAsync("arduino2");

                if (response1.StatusCode == System.Net.HttpStatusCode.OK && response2.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    SensorData arduino1Data = response1.ResultAs<SensorData>();
                    SensorData arduino2Data = response2.ResultAs<SensorData>();

                    if (arduino1Data != null && arduino2Data != null)
                    {
                        if (form2 == null || form2.IsDisposed)
                        {
                            form2 = new Form2();
                            form2.Show();
                        }
                        // 이미 창이 떠있을 경우를 위해 직접 데이터 전달 함수 호출
                        form2.DisplaySensorData(arduino1Data, arduino2Data);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
            }
        }
    }
}