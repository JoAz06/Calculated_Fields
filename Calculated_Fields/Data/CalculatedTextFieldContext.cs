using Calculated_Fields.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace Calculated_Fields.Data
{
    public class CalculatedTextFieldContext : DbContext
    {
        public CalculatedTextFieldContext(DbContextOptions<CalculatedTextFieldContext> options)
        : base(options)
        {
        }

        public DbSet<TextField> TextField { get; set; }
    }
}
