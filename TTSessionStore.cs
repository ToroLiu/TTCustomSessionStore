using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using TTEntityFramework.Extensions;

namespace TTCustomSessionStore
{
    using Models;
    using System.Diagnostics;

    public class TTSessionStore : IDisposable
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
   (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private TTSessionDbContext _db = new TTSessionDbContext();

        #region Session Operations
        /// <summary>
        /// Get the session with apiKey.
        /// </summary>
        /// <param name="apiKey">The sesion api key.</param>
        /// <returns>The session with apiKey.</returns>
        private TTSession GetSession(string apiKey) {
            if (string.IsNullOrEmpty(apiKey)) {
                return null;
            }

            return _db.SessionSet.SingleOrDefault(obj => obj.SessionId.Equals(apiKey));
        }

        /// <summary>
        /// Create a new Session.
        /// </summary>
        /// <param name="apiKey">The api key of session. If it is null or empty, a new apiKey will be generated and assigned.</param>
        /// <returns>A session instance.</returns>
        private TTSession CreateSession(string apiKey) {
            if (string.IsNullOrEmpty(apiKey))
                apiKey = GenerateApiKey();

            TTSession session = new TTSession() {  SessionId = apiKey };
            _db.SessionSet.Add(session);
            _db.SaveChanges();

            return session;
        }
        #endregion

        #region
        public string SessionId { get; private set; }
        public TimeSpan Timeout { get; set; }

        public TTSessionStore() : this(string.Empty) { }
        public TTSessionStore(string apiKey) {
            if (string.IsNullOrEmpty(apiKey))
                apiKey = GenerateApiKey();

            SessionId = apiKey;
            // Default timeout.
            Timeout = new TimeSpan(0, 30, 0);

            TTSession session = GetSession(apiKey);
            if (session == null)
                session = CreateSession(apiKey);

            Session = session;
        }

        public TTSession Session { get; private set; }
        #endregion
        
        #region
        private static readonly Random _random = new Random();
        /// <summary>
        /// Generate a random ApiKey string for accessing the custom session store.
        /// </summary>
        /// <returns>ApiKey string.</returns>
        public static string GenerateApiKey()
        {
            byte[] guid = Guid.NewGuid().ToByteArray();
            byte[] bytes = new byte[128];
            _random.NextBytes(bytes);

            return Convert.ToBase64String(guid) + Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Save all changes into database.
        /// </summary>
        /// <returns></returns>
        public int SaveChanges(int seconds=10) {
            return _db.SaveChanges(IsolationLevel.ReadCommitted, seconds);
        }
        #endregion

        #region Dispose Interface Implementation
        protected bool _isDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing) {
            if (this._isDisposed == true)
                return;

            if (disposing) {
                if (_db != null) {
                    _db.Dispose();
                    _db = null;
                }

                this._isDisposed = true;
            }
        }
        #endregion

        #region Clean Session Mechanism
        public void Clear()
        {
            var list = Session.Items.ToList();

            list.ForEach(c => {
                Session.Items.Remove(c);
                _db.SessionItemSet.Remove(c);
            });
        }

        public void CheckTimeout() { 
            //! Check timeout with lazy manner.
            DateTime limit = DateTime.UtcNow - Timeout;
            if (Session.LastAccessTimeUTC < limit) {
                Session.Items.Clear();
                Session.LastAccessTimeUTC = DateTime.UtcNow;
                this.SaveChanges();
            }

            DeleteTimeout();
        }

        #region Lazy<TTCleanInfo>
        private static Lazy<TTCleanInfo> _lazy = new Lazy<TTCleanInfo>(() => {
            TTCleanInfo info = null;
            using (TTSessionDbContext db = new TTSessionDbContext())
            {
                TTDbInfo dbInfo = db.DbInfos.SingleOrDefault(obj => obj.Key.Equals(TTDbInfo.CleanSessionInfo));
                if (dbInfo == null)
                {
                    dbInfo = new TTDbInfo();
                    dbInfo.Key = TTDbInfo.CleanSessionInfo;

                    info = new TTCleanInfo();
                    dbInfo.Save<TTCleanInfo>(info);

                    db.DbInfos.Add(dbInfo);
                    db.SaveChanges(IsolationLevel.ReadCommitted, 5);
                }
                else
                {
                    info = dbInfo.Load<TTCleanInfo>();
                }
            }

            Debug.Assert(info != null);
            return info;
        });
        #endregion

        private static TTCleanInfo CleanInfo  { 
            get { return _lazy.Value; }
        }

        //! The session expire time.
        private static readonly TimeSpan kExpiredTime = new TimeSpan(6, 0, 0);

        //! The session expire count.
        private static int kExpiredCount = 10000;

        private static void DeleteTimeout() {
            Debug.Assert(CleanInfo != null);

            DateTime limit = DateTime.UtcNow - kExpiredTime;
            CleanInfo.Count += 1;
            
            bool doClean = (CleanInfo.Count > kExpiredCount || CleanInfo.LastestCleanTime < limit);
            if (doClean == true)
            {
                #region Do Clean expired session objects.
                TransactionOptions opt = new TransactionOptions();
                opt.IsolationLevel = IsolationLevel.ReadCommitted;
                opt.Timeout = new TimeSpan(0, 0, 20);
                using (TransactionScope trans = new TransactionScope(TransactionScopeOption.Required, opt)) {
                    using (TTSessionDbContext db = new TTSessionDbContext())
                    {
                        TTSqlHelper<TTSession> helper = new TTSqlHelper<TTSession>(db);
                        string sql = "WHERE " + helper.ColumnNameOf(x => x.LastAccessTimeUTC) + " < {0}";
                        helper.Delete(sql, limit);

                        CleanInfo.Count = 0;
                        CleanInfo.LastestCleanTime = DateTime.UtcNow;

                        TTDbInfo dbInfo = db.DbInfos.Single(obj => obj.Key.Equals(TTDbInfo.CleanSessionInfo));
                        dbInfo.Save(CleanInfo);
                        db.SaveChanges();
                                                
                        trans.Complete();
                    }
                }
                #endregion
            }
        }
        #endregion
    }
}
