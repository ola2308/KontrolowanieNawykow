using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KontrolaNawykow.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public int? Wiek { get; set; }
        public double? Waga { get; set; }
        public double? Wzrost { get; set; }

        [MaxLength(50)]
        public string? AktywnoscFizyczna { get; set; }

        [MaxLength(50)]
        public string? RodzajPracy { get; set; }

        public UserGoal? Cel { get; set; }
        public double? CustomBmi { get; set; }
        public int? CustomCaloriesDeficit { get; set; }

        public List<Recipe> Recipes { get; set; }
        public List<MealPlan> MealPlans { get; set; }
        public List<CustomFood> CustomFoods { get; set; }
        public List<ShoppingList> ShoppingLists { get; set; }
        public List<ToDo> ToDos { get; set; }
    }

    public enum UserGoal
    {
        ZdroweNawyki,
        Schudniecie,
        PrzybranieMasy
    }

    public class Recipe
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
        public string Instructions { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public bool IsPublic { get; set; }

        public List<RecipeIngredient> RecipeIngredients { get; set; }
    }

    public class RecipeIngredient
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Recipe")]
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }

        [ForeignKey("Ingredient")]
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }

        public float Amount { get; set; }
    }

    public class Ingredient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }

        public List<RecipeIngredient> RecipeIngredients { get; set; }
    }

    public class MealPlan
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public MealType MealType { get; set; }
        public int? RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public string CustomEntry { get; set; }
        public bool Eaten { get; set; }
    }

    public enum MealType
    {
        Sniadanie,
        Obiad,
        Kolacja,
        Przekaska
    }

    public class CustomFood
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Name { get; set; }
        public int Calories { get; set; }
        public float Protein { get; set; }
        public float Fat { get; set; }
        public float Carbs { get; set; }
    }

    public class ShoppingList
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        public float Amount { get; set; }
        public bool Status { get; set; }
    }

    public class ToDo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public string Task { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserStatisticsViewModel
    {
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public int TotalRecipes { get; set; }
        public int TotalMealPlans { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
    }
}
