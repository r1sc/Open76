using System.Collections.Generic;
using Assets.Scripts.CarSystems;

namespace Assets.Scripts.System
{
    public class EntityManager
    {
        private static EntityManager _instance;
        public static EntityManager Instance => _instance ?? (_instance = new EntityManager());

        public List<Car> Cars { get; }

        private Dictionary<int, Car> _carLookup;

        private EntityManager()
        {
            Cars = new List<Car>();
            _carLookup = new Dictionary<int, Car>();
        }
        
        public void RegisterCar(Car Car)
        {
            if (!_carLookup.ContainsKey(Car.Id))
            {
                _carLookup.Add(Car.Id, Car);
            }

            Cars.Add(Car);
        }

        public void UnregisterCar(Car Car)
        {
            if (_carLookup.ContainsKey(Car.Id))
            {
                _carLookup.Remove(Car.Id);
            }

            Cars.Remove(Car);
        }

        public Car GetCar(int id)
        {
            Car car;
            if (!_carLookup.TryGetValue(id, out car))
            {
                return null;
            }

            return car;
        }
    }
}
