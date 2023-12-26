using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Animal
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsHealthy { get; set; }
    public string Type { get; set; } 
    public string Color { get; set; } 

    public int FarmId { get; set; }
    public Farm Farm { get; set; }
}

public class Farm
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Animal> Animals { get; set; }
}

public class DatabaseOperations
{
    public void AddAnimalToFarm(MyDbContext context, int farmId, Animal newAnimal)
    {
        var farm = context.Farms.Find(farmId);
        if (farm != null)
        {
            newAnimal.FarmId = farmId;
            newAnimal.Farm = farm;
            context.Animals.Add(newAnimal);
            context.SaveChanges();
        }
        else
        {
            Console.WriteLine("Farm not found. Animal cannot be added.");
        }
    }
        public void RemoveAnimal(MyDbContext context, int animalId)
    {
        var animalToRemove = context.Animals.Find(animalId);
        if (animalToRemove != null)
        {
            context.Animals.Remove(animalToRemove);
            context.SaveChanges();
        }
    }

    public void UpdateFarm(MyDbContext context, int farmId, string newFarmName)
    {
        var farmToUpdate = context.Farms.Find(farmId);
        if (farmToUpdate != null)
        {
            farmToUpdate.Name = newFarmName;
            context.SaveChanges();
        }
    }
}
public class MyDbContext : DbContext
{
    public DbSet<Animal> Animals { get; set; }
    public DbSet<Farm> Farms { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("MyDbContext");
        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>()
            .HasKey(a => a.Id);

        modelBuilder.Entity<Animal>()
            .Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(50);
    }
}

public class Program
{
    static void Main()
    {
        using (var context = new MyDbContext())
        {
            try
            {
                // SQL-запити з використанням LINQ to Entities
                var unionQuery = context.Animals.Where(a => a.Type == "Cow")
                                    .Union(context.Animals.Where(a => a.Type == "Sheep"));

                var exceptQuery = context.Animals.Where(a => a.Type == "Goat")
                                    .Except(context.Animals.Where(a => a.Color == "Black"));

                var intersectQuery = context.Animals.Where(a => a.Type == "Pig")
                                        .Intersect(context.Animals.Where(a => a.Age > 2));

                var joinQuery = context.Farms
                                .Join(context.Animals,
                                    farm => farm.Id,
                                    animal => animal.FarmId,
                                    (farm, animal) => new { FarmName = farm.Name, AnimalType = animal.Type });

                var distinctQuery = context.Animals.Select(a => a.Type).Distinct();

                var groupByQuery = context.Animals
                                    .GroupBy(a => a.Type)
                                    .Select(group => new
                                    {
                                        AnimalType = group.Key,
                                        TotalCount = group.Count(),
                                        AverageAge = group.Average(a => a.Age)
                                    });

                // Різні стратегії завантаження даних
                var farmWithAnimals = context.Farms.Include(farm => farm.Animals).ToList();

                var farm = context.Farms.FirstOrDefault();
                context.Entry(farm).Collection(f => f.Animals).Load();

                // Увімкнення відкладеного завантаження в контексті EF
                var lazyFarm = context.Farms.FirstOrDefault();
                if (lazyFarm != null)
                {
                    var animals = lazyFarm.Animals.ToList();
                }

                // Зміни та збереження невідстежених даних
                var newAnimal = new Animal { Type = "Chicken", Color = "White" };
                context.Animals.Add(newAnimal);
                context.SaveChanges();

                // Виклики збережених процедур і функцій
                static void NewMethod(MyDbContext context)
                {
                    // Виконання SQL-запиту і отримання результату
                    var result = context.Set<Animal>().FromSqlRaw("SELECT * FROM Animal").ToList();
                }
                var functionName = "DatabaseOperations";
                var functionResult = context.Set<Animal>().FromSqlRaw($"SELECT * FROM dbo.{functionName}").ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Операцію завершено: {ex.Message}");
                // Обробка винятків тут
            }
        }

        
    }
}


