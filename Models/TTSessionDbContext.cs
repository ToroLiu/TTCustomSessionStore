using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

using System.Transactions;

namespace TTCustomSessionStore.Models
{
    public class TTSessionDbContext : DbContext
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
   (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DbSet<TTSession> SessionSet { get; set; }
        public DbSet<TTSessionEntry> SessionItemSet { get; set; }
        public DbSet<TTDbInfo> DbInfos { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Configurations.Add(new DbConfTTSession());
        }

        static TTSessionDbContext() {
            Database.SetInitializer(new TTSessionDbContextInitializer());
        }

        /// <summary>
        /// SaveChanges with IsolationLevel and timeout mechanism.
        /// </summary>
        /// <param name="isolationLevel">The IsolationLevel of transaction.</param>
        /// <param name="seconds">The timeout of transaction.</param>
        /// <returns>The updated row numbers of database.</returns>
        public virtual int SaveChanges(IsolationLevel isolationLevel, int seconds=10) { 
            TransactionOptions opt = new TransactionOptions();
            opt.IsolationLevel = isolationLevel;
            opt.Timeout = new TimeSpan(0, 0, seconds);

            using (TransactionScope trans = new TransactionScope(TransactionScopeOption.Required, opt)) {
                int ret;
                try
                {
                    ret = SaveChanges();
                    trans.Complete();
                    return ret;
                }
                catch (Exception e) {
                    log.Error("failed to save changes." + e.Message);                                    
                }
                return 0;
            }
        }
    }

    public class TTSessionDbContextInitializer : DropCreateDatabaseIfModelChanges<TTSessionDbContext>
    {
        protected override void Seed(TTSessionDbContext context)
        {
            base.Seed(context);
        }
    }
}
