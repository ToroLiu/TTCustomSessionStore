using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TTCustomSessionStore.Models
{
    [Table("tt_dbinfo")]
    public class TTDbInfo
    {
        [Key, MaxLength(64)]
        public string Key { get; set; }
        [MaxLength(256)]
        public string Value { get; set; }

        public TTDbInfo() {
            Key = string.Empty;
            Value = string.Empty;
        }

        public const string CleanSessionInfo = "db_clean_timestamp";

        public void Save<T>(T obj) {
            this.Value = JsonConvert.SerializeObject(obj, new IsoDateTimeConverter());
        }

        public T Load<T>() {
            return JsonConvert.DeserializeObject<T>(this.Value, new IsoDateTimeConverter());
        }
    }
}
