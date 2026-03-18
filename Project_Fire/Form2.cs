using System;
using System.Windows.Forms;
using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using System.Timers;
using System.Linq.Expressions;

namespace Project_Fire
{
    public partial class Form2 : Form
    {
        private IFirebaseClient client;
        private System.Timers.Timer updateTimer;
        private bool handleCreated = false;

        public Form2()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form2_Load);
            this.HandleCreated += new EventHandler(Form2_HandleCreated);

            updateTimer = new System.Timers.Timer();
            updateTimer.Interval = 3000;
            updateTimer.Elapsed += UpdateTimer_Elapsed;
        }

        private void Form2_HandleCreated(object sender, EventArgs e)
        {
            handleCreated = true;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Form2_Load 호출됨");

            client = new FireSharp.FirebaseClient(new FirebaseConfig
            {
                AuthSecret = "6zja7ZvLbRxEuuTmvulUobmmelYMVnWHWz8g4fCp",
                BasePath = "https://projectfire-e50e7-default-rtdb.firebaseio.com"
            });

            if (client != null)
            {
                MessageBox.Show("Firebase 연결 성공!");
            }

            else
            {
                MessageBox.Show("Firebase 연결 실패!");
            }

            // 타이머 시작
            updateTimer.Start();
        }

        private async void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await FetchAndDisplayData();
        }

        private async System.Threading.Tasks.Task FetchAndDisplayData()
        {
            try
            {
                FirebaseResponse response1 = await client.GetAsync("arduino1");
                SensorData arduino1Data = response1.ResultAs<SensorData>();

                FirebaseResponse response2 = await client.GetAsync("arduino2");
                SensorData arduino2Data = response2.ResultAs<SensorData>();

                if (arduino1Data != null && arduino2Data != null)
                {
                    if (handleCreated)
                    {
                        // Arduino 1 화재 감지 조건 체크 함수
                        bool CheckFireDetection(SensorData data, out int conditionCount)
                        {
                            conditionCount = 0;
                            int thresholdCO2 = 800;
                            int thresholdTemp = 60;
                            int flameValue = 0;

                            if (double.TryParse(data.CO2, out double co2) && co2 >= thresholdCO2)
                            {
                                conditionCount++;
                            }

                            if (double.TryParse(data.Temperature, out double temp) && temp >= thresholdTemp)
                            {
                                conditionCount++;
                            }

                            if (double.TryParse(data.Flame, out double flame) && flame == flameValue)
                            {
                                conditionCount++;
                            }

                            return conditionCount >= 2;
                        }

                        // Arduino 1 화재 감지 여부
                        bool fireDetected1 = CheckFireDetection(arduino1Data, out _);

                        // Arduino 2 화재 감지 여부
                        bool fireDetected2 = CheckFireDetection(arduino2Data, out _);

                        // 화재 감지 여부에 따라 센서 오류 확인
                        if (!fireDetected1 && !fireDetected2)
                        {
                            // CO2와 온도 차이 계산
                            bool isCO2Parsed1 = double.TryParse(arduino1Data.CO2, out double co2_arduino1);
                            bool isCO2Parsed2 = double.TryParse(arduino2Data.CO2, out double co2_arduino2);
                            bool isTempParsed1 = double.TryParse(arduino1Data.Temperature, out double temp_arduino1);
                            bool isTempParsed2 = double.TryParse(arduino2Data.Temperature, out double temp_arduino2);

                            double thresholdCO2Difference = 300.0; // CO2 차이 절대 오차 범위
                            double thresholdTempDifference = 20.0; // Temperature 차이 절대 오차 범위

                            bool co2Error = isCO2Parsed1 && isCO2Parsed2 && Math.Abs(co2_arduino1 - co2_arduino2) >= thresholdCO2Difference;
                            bool tempError = isTempParsed1 && isTempParsed2 && Math.Abs(temp_arduino1 - temp_arduino2) >= thresholdTempDifference;

                            string errorMessage = "";

                            if (co2Error)
                                errorMessage += "CO2 센서 오류\n";

                            if (tempError)
                                errorMessage += "온도 센서 오류\n";

                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    MessageBox.Show(errorMessage);
                                });
                            }
                        }

                        this.Invoke((MethodInvoker)delegate
                        {
                            labelArduino1.Text = $"801호\nCO2: {double.Parse(arduino1Data.CO2):F1} ppm\nTemp: {double.Parse(arduino1Data.Temperature):F1} °C\nFlame: {(arduino1Data.Flame == "1" ? "불꽃 감지 없음" : "불꽃 감지!!")}";
                            labelArduino2.Text = $"802호\nCO2: {double.Parse(arduino2Data.CO2):F1} ppm\nTemp: {double.Parse(arduino2Data.Temperature):F1} °C\nFlame: {(arduino2Data.Flame == "1" ? "불꽃 감지 없음" : "불꽃 감지!!")}";
                            textBoxFire1.Text = fireDetected1 ? "화재가 발생했습니다!" : "화재가 감지되지 않았습니다";
                            textBoxFire2.Text = fireDetected2 ? "화재가 발생했습니다!" : "화재가 감지되지 않았습니다";
                        });
                    }
                }

                else
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("데이터를 불러오는 데 실패했습니다.");
                    });
                }
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    MessageBox.Show($"오류 발생: {ex.Message}");
                });
            }
        }

        internal void DisplaySensorData(SensorData arduino1Data, SensorData arduino2Data)
        {
            if (handleCreated)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    double co2Value1, co2Value2, tempValue1, tempValue2;
                    if (double.TryParse(arduino1Data.CO2, out co2Value1) &&
                        double.TryParse(arduino2Data.CO2, out co2Value2) &&
                        double.TryParse(arduino1Data.Temperature, out tempValue1) &&
                        double.TryParse(arduino2Data.Temperature, out tempValue2))
                    {
                        C801.BackColor = co2Value1 >= 800 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                        C802.BackColor = co2Value2 >= 800 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                        T801.BackColor = tempValue1 >= 60 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                        T802.BackColor = tempValue2 >= 60 ? System.Drawing.Color.Red : System.Drawing.Color.Green;
                        labelArduino1.Text = $"801호\nCO2: {co2Value1:F1} ppm\nTemp: {tempValue1:F1} °C\nFlame: {(arduino1Data.Flame == "1" ? "화재 감지!!" : "화재 감지 없음")}";
                        labelArduino2.Text = $"802호\nCO2: {co2Value2:F1} ppm\nTemp: {tempValue2:F1} °C\nFlame: {(arduino2Data.Flame == "1" ? "화재 감지!!" : "화재 감지 없음")}";
                    }
                });
            }
        }

        public class SensorData
        {
            public string CO2 { get; set; }
            public string Temperature { get; set; }
            public string Flame { get; set; }
        }
    }
}