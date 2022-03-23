using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Gridify.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EntityFrameworkIntegrationTests.cs;

public class MyDbContext : DbContext
{
   public DbSet<User> Users { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<User>().Property<string>("shadow1");

      var converter = new ValueConverter<Number, string>(v => v.ToString(), v => new Number(v));
      modelBuilder.Entity<User>().Property(q => q.Number).HasConversion(converter);

      base.OnModelCreating(modelBuilder);
   }

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
      optionsBuilder.UseSqlServer("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
      optionsBuilder.AddInterceptors(new SuppressCommandResultInterceptor());
      optionsBuilder.AddInterceptors(new SuppressConnectionInterceptor());
      optionsBuilder.EnableServiceProviderCaching();

      base.OnConfiguring(optionsBuilder);
   }
}

public class User
{
   public int Id { get; set; }
   public string Name { get; set; }
   public DateTime? CreateDate { get; set; }
   public Guid FkGuid { get; set; }

   [TypeConverter(typeof(string))]
   public Number Number { get; set; }
}



// ef core conversion test for value types #72
// [TypeConverter(typeof(NumberConvertor))]
public record Number
{

   public Number(string val)
   {
      const string pattern = @"^([a-zA-Z]{3})-(\d{2}\.\d{4})-(\d{5})$";
      var match = Regex.Match(val, pattern);
      if (!match.Success) return;
      if (match.Groups[1].Success)
      {
         Prefix = match.Groups[1].Value;
      }

      if (match.Groups[2].Success)
      {
         var sp = match.Groups[2].Value.Split(".");
         Date = new DateTime(int.Parse(sp[1]), int.Parse(sp[0]), 1);
      }
      if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out var s))
         Sequence = s;
   }

   public string Prefix { get; }
   public DateTime Date { get; }
   public int Sequence { get; }

   public static implicit operator Number(string b) => new(b);
   public override string ToString() => $"{Prefix}-{Date.ToString("MM")}.{Date.ToString("yyyy")}-{Sequence.ToString("00000")}";
}

public class NumberConvertor : TypeConverter
{
   public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
   {
      return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
   }

   public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
   {
      return new Number(value?.ToString());
   }
}
