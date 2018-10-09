namespace Assets.Scripts.Car.Components
{
    public interface IComponent
    {
        bool Enabled { get; set; }
        int Health { get; set; }
    }
}
