using Android.App;
using Android.Widget;
using Android.OS;
using Android.Locations;
using XamarinWeatherApp.Model;
using Android.Runtime;
using System;
using Square.Picasso;
using Newtonsoft.Json;
using Android.Content;
using System.Collections.Generic;
using System.Timers;

namespace XamarinWeatherApp
{
    [Activity(Label = "XamarinWeatherApp", MainLauncher = true, Icon = "@drawable/icon",Theme = "@style/Theme.Transparent")]
    public class MainActivity : Activity, ILocationListener
    {
        TextView txtCity, txtLastUpdate, txtDescription, txtHumidity, txtTime, txtCelsius, txtDigitalClock, txtDigitalClockPm;
        ImageView imgView;

        LocationManager locationManager;
        string provider;
        static double lat, lng;
        OpenWeatherMap openWeatherMap = new OpenWeatherMap();
        Timer timerDigital;
        int n;
        string dateString;
        string dateStringPm;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);
            txtDigitalClock = this.FindViewById<TextView>(Resource.Id.txtTimerClock);
            txtDigitalClockPm = this.FindViewById<TextView>(Resource.Id.txtTimerClockPM);

            locationManager = (LocationManager)GetSystemService(Context.LocationService);
            IList<string> providers = new List<string>();
            providers = locationManager.GetProviders(true);
            Location location = null;
            foreach (string p in providers)
            {
                Location l = locationManager.GetLastKnownLocation(p);
                if (l == null)
                {
                    continue;
                }
                if (location == null || l.Accuracy < location.Accuracy)
                {
                    // Found best last known location: %s", l);
                    location = l;
                    provider = p;
                }
            }

            timerDigital = new Timer();
            timerDigital.Interval = 1000;
            timerDigital.Enabled = true;
            timerDigital.Start();
            timerDigital.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                RunOnUiThread(() =>
                {
                    string temp;
                    temp = DateTime.Now.ToShortTimeString();
                    dateString = (temp.Substring(0, temp.Length - 5)).Trim();
                    dateStringPm = temp.Substring(temp.Length - 5, 4);
                    n++;
                    if (n % 2 == 0)
                    {
                        dateString = dateString.Replace(':', ' ');
                        txtDigitalClock.Text = dateString;
                        n = 0;
                    }
                    else
                    {
                        txtDigitalClock.Text = dateString;
                    }
                    txtDigitalClockPm.Text = dateStringPm.Trim();
                });
            };


        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                string dateString = DateTime.Now.ToShortTimeString();
                n++;
                if (n % 2 == 0)
                {
                    dateString = dateString.Replace(':', ' ');
                    txtDigitalClock.Text = dateString;
                    n = 0;
                }
                else
                {
                    txtDigitalClock.Text = dateString;
                }

            });
        }

        protected override void OnResume()
        {
            base.OnResume();
            locationManager.RequestLocationUpdates(provider, 400, 1, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
        }
        public void OnLocationChanged(Location location)
        {
            lat = Math.Round(location.Latitude, 4);
            lng = Math.Round(location.Longitude, 4);

            new GetWeather(this, openWeatherMap).Execute(Common.Common.APIRequest(lat.ToString(), lng.ToString()));
        }

        public void OnProviderDisabled(string provider)
        {
            
        }

        public void OnProviderEnabled(string provider)
        {
           
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            
        }


        private class GetWeather : AsyncTask<string, Java.Lang.Void, string>
        {
            private ProgressDialog pd = new ProgressDialog(Application.Context);
            private MainActivity activity;
            OpenWeatherMap openWeatherMap;

            public GetWeather(MainActivity activity,OpenWeatherMap openWeatherMap)
            {
                this.activity = activity;
                this.openWeatherMap = openWeatherMap;
            }

            protected override void OnPreExecute()
            {
                base.OnPreExecute();
                //pd.Window.SetType(Android.Views.WindowManagerTypes.SystemAlert);
                //pd.SetTitle("Please wait....");
                //pd.Show();
            }
            protected override string RunInBackground(params string[] @params)
            {
                string stream = null;
                string urlString = @params[0];
                Helper.Helper http = new Helper.Helper();
                //urlString = Common.Common.APIRequest(lat.ToString(), lng.ToString());
                stream = http.GetHTTPData(urlString);
                return stream;
            }
            protected override void OnPostExecute(string result)
            {
                base.OnPostExecute(result);
                if (result.Contains("Error: Not City Found"))
                {
                    pd.Dismiss();
                    return;
                }

                openWeatherMap = JsonConvert.DeserializeObject<OpenWeatherMap>(result);
                pd.Dismiss();

                //Controls 

                activity.txtCity = activity.FindViewById<TextView>(Resource.Id.txtCity);
                activity.txtLastUpdate = activity.FindViewById<TextView>(Resource.Id.txtLastUpdate);
                activity.txtDescription = activity.FindViewById<TextView>(Resource.Id.txtDescription);
                activity.txtHumidity = activity.FindViewById<TextView>(Resource.Id.txtHumidity);
                activity.txtTime = activity.FindViewById<TextView>(Resource.Id.txtTime);
                activity.txtCelsius = activity.FindViewById<TextView>(Resource.Id.txtCelsius);

                activity.imgView = activity.FindViewById<ImageView>(Resource.Id.imageView);

                //Add Data 

                activity.txtCity.Text = $"{openWeatherMap.name},{openWeatherMap.sys.country}";
                activity.txtLastUpdate.Text = $"Last Updated: {DateTime.Now.ToString("dd MMMM yyyy HH:mm")}";
                activity.txtDescription.Text = $"{openWeatherMap.weather[0].description}";
                activity.txtHumidity.Text = $"Humidity: {openWeatherMap.main.humidity} %";
                activity.txtTime.Text = $"SunRise: {Common.Common.UnixTimeStampToDateTime(openWeatherMap.sys.sunrise).ToString("HH:mm")}\nSunSet:  {Common.Common.UnixTimeStampToDateTime(openWeatherMap.sys.sunset).ToString("HH:mm")}";
                activity.txtCelsius.Text = $"{openWeatherMap.main.temp} °C";

                if (!string.IsNullOrEmpty(openWeatherMap.weather[0].icon))
                {
                    Picasso.With(activity.ApplicationContext).Load(Common.Common.GetImage(openWeatherMap.weather[0].icon)).Into(activity.imgView);
                }
            }
        }
    }
}

