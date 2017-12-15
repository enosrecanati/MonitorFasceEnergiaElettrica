using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using MonitorFasceEnergiaElettrica.Services;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace MonitorFasceEnergiaElettrica
{
    public class Program
    {
        private static bool _initialised;

        public static NetworkTimeProtocolService NtpService { get; private set; }

        public static PWM RedLedDutyCyclePwm = new PWM(PWMChannels.PWM_PIN_D5, 100, .5, false);
        public static PWM YellowLedDutyCyclePwm = new PWM(PWMChannels.PWM_PIN_D6, 100, .5, false);
        public static PWM GreenLedDutyCyclePwm = new PWM(PWMChannels.PWM_PIN_D9, 100, .5, false);

        public static int[] SaturdayFasceMap = new int[] { 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2 };
        public static int[] SundayFasceMap = new int[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public static int[] WeekdaysFasceMap = new int[] { 2, 2, 2, 2, 2, 2, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2 };

        public static void Main()
        {
            Initialise();

            for (;;)
            {
                if (_initialised)
                {
                    DateTime now = DateTime.Now;

                    int[] fasceMap = GetFasceMapFromDateTime(now);
                    int fasciaValue = fasceMap[now.Hour];

                    Debug.Print("Now: " + now.ToString());
                    Debug.Print("Fascia: F" + (fasciaValue + 1).ToString());

                    switch (fasciaValue)
                    {
                        case 0:
                            Debug.Print("Blinking RED LED");

                            StartBlinkingRedLed();
                            StopBlinkingYellowLed();
                            StopBlinkingGreenLed();

                            break;
                        case 1:
                            Debug.Print("Blinking YELLOW LED");

                            StopBlinkingRedLed();
                            StartBlinkingYellowLed();
                            StopBlinkingGreenLed();

                            break;
                        case 2:
                            Debug.Print("Blinking GREEN LED");

                            StopBlinkingRedLed();
                            StopBlinkingYellowLed();
                            StartBlinkingGreenLed();

                            break;
                        default:
                            Debug.Print("Stop Blinking LEDs");

                            StopBlinkingRedLed();
                            StopBlinkingYellowLed();
                            StopBlinkingGreenLed();

                            break;
                    }
                }
                else
                {
                    Debug.Print("Not Initialised!!!");
                }

                Thread.Sleep(60000);
            }
        }

        private static void Initialise()
        {
            InitialiseLeds();
            InitialiseNtpService();
        }

        private static void InitialiseLeds()
        {
            RedLedDutyCyclePwm.Stop();
            YellowLedDutyCyclePwm.Stop();
            GreenLedDutyCyclePwm.Stop();
        }

        private static void InitialiseNtpService()
        {
            NtpService = new NetworkTimeProtocolService();
            NtpService.DateTimeUpdated += (object sender, DateTimeUpdatedEventArgs args) =>
            {
                Debug.Print("ntpService.DateTimeUpdated");
                Debug.Print(args.DateTime.ToString());

                Utility.SetLocalTime(args.DateTime);

                _initialised = true;
            };

            NtpService.Start();   
        }

        public static int[] GetFasceMapFromDateTime(DateTime dateTime)
        {
            int[] fasceMap = new int[0];

            switch (dateTime.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    Debug.Print("SundayFasceMap");

                    fasceMap = SundayFasceMap;

                    break;
                case DayOfWeek.Saturday:
                    Debug.Print("SaturdayFasceMap");

                    fasceMap = SaturdayFasceMap;

                    break;    
                default:
                    Debug.Print("WeekdaysFasceMap");

                    fasceMap = WeekdaysFasceMap;

                    break;
            }

            return fasceMap;
        }

        public static void StartBlinkingGreenLed()
        {
            GreenLedDutyCyclePwm.Start();
        }

        public static void StartBlinkingRedLed()
        {
            RedLedDutyCyclePwm.Start();
        }

        public static void StartBlinkingYellowLed()
        {
            YellowLedDutyCyclePwm.Start();
        }

        public static void StopBlinkingGreenLed()
        {
            GreenLedDutyCyclePwm.Stop();
        }

        public static void StopBlinkingRedLed()
        {
            RedLedDutyCyclePwm.Stop();
        }

        public static void StopBlinkingYellowLed()
        {
            YellowLedDutyCyclePwm.Stop();
        }
    }
}
