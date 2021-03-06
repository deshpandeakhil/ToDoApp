﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ToDo.Web.Data
{
    public class ToDoContext : IdentityDbContext
    {
        public ToDoContext(DbContextOptions<ToDoContext> options) : base(options)
        {
            //Database.Log = str => Debug.WriteLine(str);
            Database.Migrate();
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

       
        }

        public DbSet<Item> Items { get; set; }

        public DbSet<SubItem> SubItems { get; set; }

        public DbSet<Priority> Priorities { get; set; }

        public DbSet<AppUser> AppUsers { get; set; }

    }
}
