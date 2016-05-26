using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using GrovePi;
using GrovePi.I2CDevices;
using GrovePi.Sensors;

namespace PiMe.Helpers
{
    internal class Pi
    {
        private readonly IBuzzer _buzzer;
        private readonly IRgbLcdDisplay _display;
        private readonly ILightSensor _light;
        private readonly ILed _led;
        private readonly ITemperatureAndHumiditySensor _temp;
        private readonly IButtonSensor _button;
        private readonly IUltrasonicRangerSensor _ultrasonicRanger;

        public Pi()
        {
            var builder = DeviceFactory.Build;

            _display = builder.RgbLcdDisplay();

            _light = builder.LightSensor(Pin.AnalogPin0);

            _led = builder.Led(Pin.DigitalPin7);
            //_temp = builder.TemperatureAndHumiditySensor(Pin.AnalogPin1, TemperatureAndHumiditySensorModel.DHT11);

            _ultrasonicRanger = builder.UltraSonicSensor(Pin.DigitalPin3);

            _button = builder.ButtonSensor(Pin.DigitalPin2);
            //_buzzer = builder.Buzzer(Pin.DigitalPin3);
        }

        public async Task Buzz(int milliseconds = 1000)
        {
            _buzzer.ChangeState(SensorStatus.On);
            await Task.Delay(100);
            _buzzer.ChangeState(SensorStatus.Off);

            await Task.Delay(200);

            _buzzer.ChangeState(SensorStatus.On);
            await Task.Delay(100);
            _buzzer.ChangeState(SensorStatus.Off);

            await Task.Delay(200);

            _buzzer.ChangeState(SensorStatus.On);
            await Task.Delay(500);
            _buzzer.ChangeState(SensorStatus.Off);
        }

        public void DisplayText(string line1)
        {
            DisplayText(line1, "", null);
        }

        public void DisplayText(string line1, Color background)
        {
            DisplayText(line1, "", background);
        }

        public void DisplayText(string line1, string line2, Color? background = null)
        {
            if (line1.Length > 16) line1 = line1.Substring(0, 16);
            if (line2.Length > 16) line2 = line2.Substring(0, 16);

            _display.SetText(line1.PadRight(16) + line2);

            if (background != null)
            {
                _display.SetBacklightRgb(background.Value.R, background.Value.G, background.Value.B);
            }
        }

        public int GetRange()
        {
            return _ultrasonicRanger.MeasureInCentimeters();
        }

        //public TemperatureAndHumiditySensorValue GetTemperatureAndHumidity()
        //{
        //    return _temp.TemperatureAndHumidity();
        //}

        public int GetLight()
        {
            return _light.SensorValue();
        }

        public void SetLed(bool isOn)
        {
            _led.ChangeState(isOn ? SensorStatus.On : SensorStatus.Off);
        }

        public bool IsButtonPressed()
        {
            return _button.CurrentState == SensorStatus.On;
        }

    }
}
