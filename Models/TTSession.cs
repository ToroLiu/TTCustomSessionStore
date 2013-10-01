using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Data.Entity.ModelConfiguration;
using TTEntityFramework.Extensions;

namespace TTCustomSessionStore.Models
{
    [Table("tt_session")]
    public class TTSession
    {
        #region Properties
        [Key]
        public long Id { get; set; }

        [MaxLength(256)]
        [TTIndex(true)]        
        public string SessionId { get; set; }
        public DateTime CreatedTimeUTC { get; set; }
        public DateTime LastAccessTimeUTC { get; set; }

        public virtual ICollection<TTSessionEntry> Items { get; set; }
        #endregion

        public TTSession() : this(string.Empty) {
            CreatedTimeUTC = DateTime.UtcNow;
            LastAccessTimeUTC = DateTime.UtcNow;

            Items = new HashSet<TTSessionEntry>();
        }
        public TTSession(string apiKey) {
            SessionId = apiKey;
        }

        #region Public Operations
        public void Save<T>(string key, T obj) {
            var item = Items.SingleOrDefault(c => c.Key.Equals(key));
            if (item == null) {
                item = new TTSessionEntry(key);
                this.Items.Add(item);
            }
            item.Save(obj);

            this.LastAccessTimeUTC = DateTime.UtcNow;
        }

        public bool IsExistKey(string key) {
            return Items.Any(c => c.Key.Equals(key));
        }
        public T Load<T>(string key) {
            var item = Items.SingleOrDefault(c => c.Key.Equals(key));
            if (item == null) {
                return default(T);
            }

            this.LastAccessTimeUTC = DateTime.UtcNow;
            return item.Load<T>();   
        }
        #endregion
    }

    public class DbConfTTSession : EntityTypeConfiguration<TTSession> {
        public DbConfTTSession()
        {
            this.HasMany(lt => lt.Items)
                .WithRequired(rt => rt.Session)
                .HasForeignKey(rt => rt.SessionId)
                .WillCascadeOnDelete(true);
        }
    }
}
