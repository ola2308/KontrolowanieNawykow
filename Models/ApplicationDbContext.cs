using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace KontrolaNawykow.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<CustomFood> CustomFoods { get; set; }
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ToDo> ToDos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfiguracja relacji RecipeIngredient z Recipe
            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Recipe)
                .WithMany(r => r.RecipeIngredients)
                .HasForeignKey(ri => ri.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji RecipeIngredient z Ingredient
            modelBuilder.Entity<RecipeIngredient>()
                .HasOne(ri => ri.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji User z Recipe
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.User)
                .WithMany(u => u.Recipes)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji User z MealPlan
            modelBuilder.Entity<MealPlan>()
                .HasOne(mp => mp.User)
                .WithMany(u => u.MealPlans)
                .HasForeignKey(mp => mp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji User z CustomFood
            modelBuilder.Entity<CustomFood>()
                .HasOne(cf => cf.User)
                .WithMany(u => u.CustomFoods)
                .HasForeignKey(cf => cf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji User z ShoppingList
            modelBuilder.Entity<ShoppingList>()
                .HasOne(sl => sl.User)
                .WithMany(u => u.ShoppingLists)
                .HasForeignKey(sl => sl.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji User z ToDo
            modelBuilder.Entity<ToDo>()
                .HasOne(td => td.User)
                .WithMany(u => u.ToDos)
                .HasForeignKey(td => td.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Opcjonalnie: Unikalny email dla User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}
