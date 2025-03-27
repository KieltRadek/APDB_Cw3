namespace Program;

public class OverfillException : Exception
{
    public OverfillException(string message) : base(message) { }
}

public interface IHazardNotifier
{
    void NotifyHazard(string containerNumber);
}

public abstract class Container
{
    public string SerialNumber { get; protected set; }
    public double CargoMass { get; protected set; }
    public double Height { get; protected set; }
    public double TareWeight { get; protected set; }
    public double Depth { get; protected set; } 
    public double MaxPayload { get; protected set; } 

    protected Container(string containerType, double height, double tareWeight, double depth, double maxPayload)
    {
        SerialNumber = GenerateSerialNumber(containerType);
        Height = height;
        TareWeight = tareWeight;
        Depth = depth;
        MaxPayload = maxPayload;
        CargoMass = 0;
    }

    private static int containerCounter = 1;

    private string GenerateSerialNumber(string containerType)
    {
        return $"KON-{containerType}-{containerCounter++}";
    }

    public virtual void EmptyCargo()
    {
        CargoMass = 0;
    }

    public virtual void LoadCargo(double mass)
    {
        if (mass > MaxPayload)
        {
            throw new OverfillException($"Próba załadowania {mass} kg, gdy maksymalna ładowność to {MaxPayload} kg");
        }
        CargoMass = mass;
    }

    public override string ToString()
    {
        return $"{SerialNumber} - Masa ładunku: {CargoMass} kg, Wysokość: {Height} cm, Waga własna: {TareWeight} kg, Głębokość: {Depth} cm, Maks. ładowność: {MaxPayload} kg";
    }
}

public class LiquidContainer : Container, IHazardNotifier
{
    public bool IsHazardous { get; private set; }

    public LiquidContainer(double height, double tareWeight, double depth, double maxPayload, bool isHazardous) 
        : base("L", height, tareWeight, depth, maxPayload)
    {
        IsHazardous = isHazardous;
    }

    public override void LoadCargo(double mass)
    {
        double maxAllowed = IsHazardous ? MaxPayload * 0.5 : MaxPayload * 0.9;
        
        if (mass > maxAllowed)
        {
            NotifyHazard(SerialNumber);
            throw new OverfillException($"Próba załadowania {mass} kg, gdy maksymalna dozwolona ładowność to {maxAllowed} kg");
        }
        
        base.LoadCargo(mass);
    }

    public void NotifyHazard(string containerNumber)
    {
        Console.WriteLine($"UWAGA: Niebezpieczna operacja na kontenerze {containerNumber}!");
    }
}

public class GasContainer : Container, IHazardNotifier
{
    public double Pressure { get; private set; }

    public GasContainer(double height, double tareWeight, double depth, double maxPayload, double pressure) 
        : base("G", height, tareWeight, depth, maxPayload)
    {
        Pressure = pressure;
    }

    public override void EmptyCargo()
    {
        CargoMass = CargoMass * 0.05;
    }

    public override void LoadCargo(double mass)
    {
        if (mass > MaxPayload)
        {
            NotifyHazard(SerialNumber);
            throw new OverfillException($"Próba załadowania {mass} kg, gdy maksymalna ładowność to {MaxPayload} kg");
        }
        CargoMass = mass;
    }

    public void NotifyHazard(string containerNumber)
    {
        Console.WriteLine($"UWAGA: Niebezpieczna operacja na kontenerze {containerNumber} z gazem pod ciśnieniem {Pressure} atm!");
    }
}

public class RefrigeratedContainer : Container
{
    public string ProductType { get; private set; }
    public double Temperature { get; private set; }

    private static readonly Dictionary<string, double> ProductTemperatureRequirements = new Dictionary<string, double>
    {
        {"Bananas", 13.3},
        {"Chocolate", 18},
        {"Fish", 2},
        {"Meat", -15},
        {"Ice cream", -18},
        {"Frozen pizza", -30},
        {"Cheese", 7.2},
        {"Sausages", 5},
        {"Butter", 20.5},
        {"Eggs", 19}
    };

    public RefrigeratedContainer(double height, double tareWeight, double depth, double maxPayload, string productType, double temperature) 
        : base("C", height, tareWeight, depth, maxPayload)
    {
        if (!ProductTemperatureRequirements.ContainsKey(productType))
        {
            throw new ArgumentException($"Nieznany typ produktu: {productType}");
        }

        if (temperature < ProductTemperatureRequirements[productType])
        {
            throw new ArgumentException($"Temperatura {temperature}°C jest niższa niż wymagana {ProductTemperatureRequirements[productType]}°C dla {productType}");
        }

        ProductType = productType;
        Temperature = temperature;
    }

    public override string ToString()
    {
        return base.ToString() + $", Produkt: {ProductType}, Temperatura: {Temperature}°C";
    }
}

public class ContainerShip
{
    public List<Container> Containers { get; private set; }
    public double MaxSpeed { get; private set; } // w węzłach
    public int MaxContainerCount { get; private set; }
    public double MaxWeight { get; private set; } // w tonach

    public ContainerShip(double maxSpeed, int maxContainerCount, double maxWeight)
    {
        Containers = new List<Container>();
        MaxSpeed = maxSpeed;
        MaxContainerCount = maxContainerCount;
        MaxWeight = maxWeight;
    }

    public void LoadContainer(Container container)
    {
        if (Containers.Count >= MaxContainerCount)
        {
            throw new InvalidOperationException($"Nie można dodać więcej kontenerów. Maksymalna liczba: {MaxContainerCount}");
        }

        double totalWeight = Containers.Sum(c => c.CargoMass + c.TareWeight) + container.CargoMass + container.TareWeight;
        if (totalWeight / 1000 > MaxWeight)
        {
            throw new InvalidOperationException($"Przekroczono maksymalną wagę ładunku. Maksymalna waga: {MaxWeight} ton");
        }

        Containers.Add(container);
    }

    public void LoadContainers(List<Container> containers)
    {
        foreach (var container in containers)
        {
            LoadContainer(container);
        }
    }

    public void RemoveContainer(string serialNumber)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container != null)
        {
            Containers.Remove(container);
        }
        else
        {
            throw new ArgumentException($"Kontener o numerze {serialNumber} nie został znaleziony na statku");
        }
    }

    public void ReplaceContainer(string serialNumber, Container newContainer)
    {
        RemoveContainer(serialNumber);
        LoadContainer(newContainer);
    }

    public void UnloadContainer(string serialNumber)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        container?.EmptyCargo();
    }

    public void MoveContainerToAnotherShip(string serialNumber, ContainerShip anotherShip)
    {
        var container = Containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container == null)
        {
            throw new ArgumentException($"Kontener o numerze {serialNumber} nie został znaleziony na statku");
        }

        RemoveContainer(serialNumber);
        anotherShip.LoadContainer(container);
    }

    public void PrintShipInfo()
    {
        Console.WriteLine($"Statek (prędkość maks.: {MaxSpeed} węzłów, maks. liczba kontenerów: {MaxContainerCount}, maks. waga: {MaxWeight} ton)");
        Console.WriteLine("Załadowane kontenery:");
        foreach (var container in Containers)
        {
            Console.WriteLine($"- {container}");
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Przykładowe użycie
        
        // Tworzenie kontenerów
        var liquidContainer = new LiquidContainer(200, 500, 100, 1000, false);
        var hazardousLiquidContainer = new LiquidContainer(200, 500, 100, 1000, true);
        var gasContainer = new GasContainer(150, 400, 120, 800, 2.5);
        var refrigeratedContainer = new RefrigeratedContainer(250, 600, 150, 1200, "Bananas", 13.3);
        
        // Ładowanie kontenerów
        try
        {
            liquidContainer.LoadCargo(900); // OK
            hazardousLiquidContainer.LoadCargo(400); // OK (50% z 1000)
            gasContainer.LoadCargo(700);
            refrigeratedContainer.LoadCargo(1000);
            
            // Próba przeładowania - powinno rzucić wyjątek
            hazardousLiquidContainer.LoadCargo(600);
        }
        catch (OverfillException ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
        
        // Tworzenie statków
        var ship1 = new ContainerShip(20, 5, 10); // prędkość 20 węzłów, max 5 kontenerów, max 10 ton
        var ship2 = new ContainerShip(15, 10, 20);
        
        // Ładowanie kontenerów na statek
        ship1.LoadContainer(liquidContainer);
        ship1.LoadContainer(hazardousLiquidContainer);
        ship2.LoadContainer(gasContainer);
        ship2.LoadContainer(refrigeratedContainer);
        
        // Wyświetlanie informacji o statkach
        Console.WriteLine("\nStan statku 1:");
        ship1.PrintShipInfo();
        
        Console.WriteLine("\nStan statku 2:");
        ship2.PrintShipInfo();
        
        // Przenoszenie kontenera między statkami
        Console.WriteLine("\nPrzenoszenie kontenera...");
        ship1.MoveContainerToAnotherShip(liquidContainer.SerialNumber, ship2);
        
        Console.WriteLine("\nStan po przeniesieniu:");
        ship1.PrintShipInfo();
        ship2.PrintShipInfo();
        
        // Rozładowanie kontenera
        Console.WriteLine("\nRozładowanie kontenera z gazem...");
        ship2.UnloadContainer(gasContainer.SerialNumber);
        Console.WriteLine($"Masa ładunku po rozładowaniu: {gasContainer.CargoMass} kg");
        
        // Zastępowanie kontenera
        var newRefrigeratedContainer = new RefrigeratedContainer(250, 600, 150, 1200, "Chocolate", 18);
        Console.WriteLine("\nZastępowanie kontenera...");
        ship2.ReplaceContainer(refrigeratedContainer.SerialNumber, newRefrigeratedContainer);
        ship2.PrintShipInfo();
    }
}