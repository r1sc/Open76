using System.Collections.Generic;
using Assets.Scripts.Entities;

namespace Assets.Scripts.System
{
    public class EntityManager
    {
        private static EntityManager _instance;
        public static EntityManager Instance
        {
            get { return _instance ?? (_instance = new EntityManager()); }
        }

        public List<Car> Cars { get; }

        private readonly Dictionary<int, Car> _carLookup;

        private EntityManager()
        {
            Cars = new List<Car>();
            _carLookup = new Dictionary<int, Car>();
        }
        
        public void RegisterCar(Car car)
        {
            if (!_carLookup.ContainsKey(car.Id))
            {
                _carLookup.Add(car.Id, car);
            }

            Cars.Add(car);
        }

        public void UnregisterCar(Car car)
        {
            if (_carLookup.ContainsKey(car.Id))
            {
                _carLookup.Remove(car.Id);
            }

            Cars.Remove(car);
        }

        public Car GetCar(int id)
        {
            if (!_carLookup.TryGetValue(id, out Car car))
            {
                return null;
            }

            return car;
        }
    }
}
