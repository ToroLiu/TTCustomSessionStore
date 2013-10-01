using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.IO;

using TTEntityFramework.Extensions;
using Newtonsoft.Json;

namespace TTCustomSessionStore.Models
{
    [Table("tt_session_pair")]
    public class TTSessionEntry
    {
        #region Properties
        [Key]
        public long Id { get; set; }
        
        [Column("dbKey"), MaxLength(256)]
        public string Key { get; set; }

        [MaxLength(4096)]
        public string Value { get; set; }

        [TTIndex(true, SubIndexColumns = new[] { "dbKey" })]
        public long SessionId { get; set; }
        public virtual TTSession Session { get; set; }
        #endregion

        public TTSessionEntry() : this(string.Empty) { }
        public TTSessionEntry(string key) {
            this.Key = key;
        }

        public void Save<T>(T obj) {
            this.Value = JsonConvert.SerializeObject(obj);
        }

        public T Load<T>() {
            return JsonConvert.DeserializeObject<T>(this.Value);
        }
    }
}
