using HisseFiyatlari.Models; 
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HisseFiyatlari.Data; 


public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<MarketData> MarketDatas { get; set; }
    public DbSet<Menu> Menus { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<Partner> Partners { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
}