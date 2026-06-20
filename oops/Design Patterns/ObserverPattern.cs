namespace oops.Design_Patterns
{
    // Mainly 4 entities are involved in Observer Design Pattern.
    // 1. The IObserver Interface - Defines an single update method to be called when the Subject changes. This is the Observer interface that all observers must implement.
    // 2. The Subject Abstract class -  Maintains a list of observers. Defines method to attach ,detach and notify the observers
    // 3. The Concrete Observer Class - The real class that wants to be nofified when any subject class changes. It implements the IObserver interface and defines the update method to handle the notification from the subject.
    // 4. The Concrete Subject Class - The real class that wants to notify the observers when it changes.

    // Any class wants to gets notified must implement the IObserver interface
    interface IObserver
    {
        void Update();
    }

    // Any class who want to notify their observers must inherits Subject class
    abstract class Subject
    {
        private readonly List<IObserver> observers = new();

        public void Attach(IObserver observer)
        {
            observers.Add(observer);
        }

        public void Remove(IObserver observer)
        {
            observers.Remove(observer);
        }

        public void Notify()
        {
            foreach(IObserver observer in observers)
            {
                observer.Update();
            }
        }
    }

    // Defining Concerete classes

    class CurrentWeatherDisplay: IObserver
    {
        private readonly WeatherStation weatherStation;

        public CurrentWeatherDisplay(WeatherStation weatherStation)
        {
            this.weatherStation = weatherStation;
            this.weatherStation.Attach(this);
        }

        public void DisplayTempreture()
        {
            Console.WriteLine($"The current tempreture is {this.weatherStation.CurrentTempreture}");
        }

        public void Update()
        {
            DisplayTempreture();
        }
    }

    class WeatherStation: Subject
    {
        private int currentTempreture;

        public int CurrentTempreture
        {
            set
            {
                currentTempreture = value;
                Notify();
            }
            get
            {
                return currentTempreture;
            }
        }
    }
}
