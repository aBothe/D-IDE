/*using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using D_Parser;
using System.Windows.Forms;
using ICSharpCode.NRefactory;

namespace D_IDE
{
	class SQLNodeStorageEx : IDisposable
	{
		private const string NODE_TABLE = "nodes";
		private const string MODULE_TABLE = "modules";

        private int transactionDepth = 0;
        private string fileName;
        private SQLiteTransaction transaction;
		private SQLiteConnection connection;
		private Dictionary<long, DataType> rootNodeCache = new Dictionary<long, DataType>();
		private Dictionary<long, DModule> rootModuleCache = new Dictionary<long, DModule>();

        public SQLNodeStorageEx(string fileName, bool readOnly)
		{
            this.fileName = fileName;
            Open(readOnly, false, null);
		}

        public void Open(bool readOnly, bool disableSynchronous, long? cacheSize)
		{
            this.Close();

            StringBuilder connectionString = new StringBuilder();
            connectionString.Append("Data Source=\"" + fileName + "\"");
            if (cacheSize.HasValue) connectionString.Append(";Cache Size=").Append(cacheSize.Value);
            if (disableSynchronous) connectionString.Append(";Synchronous=Off");
            if (readOnly) connectionString.Append(";Read Only=True");

            this.connection = new SQLiteConnection(connectionString.ToString());
            this.connection.Open();
            transactionDepth = 0;
		}

		public void Close()
		{
            if (this.connection != null)
            {
                transactionDepth = (transactionDepth > 1) ? 1 : transactionDepth;
                this.Commit();
                this.connection.Close();
            }
        }
		
		public void BeginTransaction()
        {
            // Nesting transactions with a depth counter. If the connection is closed, we'll commit anyway.
			// EDIT: Read the SQLite doc - there are no nested transactions ;-)
            if (transaction == null) 
                this.transaction = connection.BeginTransaction();
            transactionDepth++;
		}

		public void Commit()
		{
            if (this.transaction != null)
            {
                if (transactionDepth <= 1)
                {
                    this.transaction.Commit();
                    this.transaction.Dispose();
                    this.transaction = null;
                }

                transactionDepth--;
            }
        }

        public void TruncateDatabase()
        {
            TruncateTable(NODE_TABLE);
            TruncateTable(MODULE_TABLE);
        }

        public void TruncateTable(string table)
        {
            ExecuteNonQuery("delete from " + table + ";");
        }

        public void Dispose()
        {
            Close();
            rootModuleCache.Clear();
            rootNodeCache.Clear();
        }

        #region Create Table Statements
        public void InitializeDatabase()
        {
            CreateModuleTable();
            CreateNodeTable();
            CreateIndexOnField(NODE_TABLE, "parent");
        }

        private void CreateModuleTable()
        {
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS " + MODULE_TABLE + " ( " +
                "id INTEGER NULL PRIMARY KEY AUTOINCREMENT," +
                "ModuleName varchar(256) NOT NULL," +
                "ModuleFile varchar(256) NOT NULL," +
                "DOMid INTEGER NOT NULL" +
                ");");
        }

        private void CreateNodeTable()
        {
            ExecuteNonQuery("CREATE TABLE IF NOT EXISTS " + NODE_TABLE + " ( " +
                "id INTEGER NULL PRIMARY KEY AUTOINCREMENT," +
                "fieldtype INTEGER NOT NULL," +
                "name varchar(512) NOT NULL," +
                "typetoken INTEGER NOT NULL," +
                "type varchar(512) NOT NULL," +
                "parent INTEGER NULL," +
                "location varchar(512) NOT NULL," +
                "endlocation varchar(512) NOT NULL," +

                "modifiers varchar(512) NULL," +
                "module varchar(512) NULL," +
                "value varchar(2048) NULL," +
                "param varchar(2048) NULL," +
                "children varchar(4096) NULL," +
                "super varchar(512) NULL," +
                "interface varchar(512) NULL" +
                ");");
        }
        #endregion

		#region Nodes
		public void UpdateRawNodes()
        {
            using (SQLiteDataReader dr = SelectByField(NODE_TABLE, "parent", 0))
            {
                while (dr.Read())
                {
                    long id = (long)dr["id"];
                    rootNodeCache[id] = ReadNode(null, dr);
                }

                dr.Close();
            }
		}

		public DataType ReadNode(DataType parent, long id)
		{
            DataType node = null;

            using (SQLiteDataReader dr = SelectByField(NODE_TABLE, "id", id))
            {
                if (!dr.HasRows) return null;
                else node = ReadNode(parent, dr);

                dr.Close();
            }

            return node;
		}

		public DataType ReadNode(DataType parent, SQLiteDataReader dr)
		{
			if (!dr.HasRows) return null;

			DataType ret = new DataType();
			long id = (long)dr["id"];

			ret.fieldtype = (FieldType)(int)(long)dr["fieldtype"];
			ret.name = (string)dr["name"];
			ret.TypeToken = (int)(long)dr["typetoken"];
			ret.type = dr["type"] as String;
			long parid = (long)dr["parent"];
			ret.parent = parent;//RawNodes.ContainsKey(parid)?RawNodes[parid]:ReadNode(parid);

			string[] ts = ((string)dr["location"]).Split(';');
			ret.StartLocation = new Location(Convert.ToInt32(ts[0]), Convert.ToInt32(ts[1]));
			ts = ((string)dr["endlocation"]).Split(';');
			ret.EndLocation = new Location(Convert.ToInt32(ts[0]), Convert.ToInt32(ts[1]));
			ts = ((string)dr["modifiers"]).Split(';');
			foreach (string s in ts)
				if (!String.IsNullOrEmpty(s)) ret.modifiers.Add(Convert.ToInt32(s));

            ret.module = dr["module"] as String;
            ret.value = dr["value"] as String;

			ts = ((string)dr["param"]).Split(';');
			foreach (string s in ts)
			{
				if (String.IsNullOrEmpty(s)) continue;
				long cid = Convert.ToInt64(s);
				if (cid == id) continue;
				ret.param.Add(rootNodeCache.ContainsKey(cid) ? rootNodeCache[cid] : ReadNode(ret, cid));
			}
			ts = ((string)dr["children"]).Split(';');
			foreach (string s in ts)
			{
				if (String.IsNullOrEmpty(s)) continue;
				long cid = Convert.ToInt64(s);
				if (cid == id) continue;
				ret.Children.Add(rootNodeCache.ContainsKey(cid) ? rootNodeCache[cid] : ReadNode(ret, cid));
			}

			ret.superClass = (string)dr["super"];
			ret.implementedInterface = (string)dr["interface"];

			return ret;
		}

		public long InsertNode(DataType dt)
		{
			if (dt == null) return 0;

			List<long> toupdate = new List<long>(dt.param.Count + dt.Children.Count);

            StringBuilder mods = new StringBuilder();
            foreach (int mod in dt.modifiers) mods.Append(mod).Append(";");

            StringBuilder dtparams = new StringBuilder();
            foreach (DataType c in dt.param)
            {
                long tl = InsertNode(c);
                toupdate.Add(tl);
                dtparams.Append(tl).Append(";");
            }

            StringBuilder dtchldrn = new StringBuilder();
            foreach (DataType c in dt.Children)
            {
                long tl = InsertNode(c);
                toupdate.Add(tl);
                dtchldrn.Append(tl).Append(";");
            }

            int result = InsertValues(NODE_TABLE, 
                null,
                (int)dt.fieldtype,
                dt.name,
                (int)dt.TypeToken,
                dt.type,
                0,
                dt.StartLocation.Column + ";" + dt.StartLocation.Line,
                dt.EndLocation.Column + ";" + dt.EndLocation.Line,
                mods,
                dt.module,
                dt.value,
                dtparams,
                dtchldrn,
                dt.superClass,
                dt.implementedInterface);

            long lastid = GetLastInsertedId(NODE_TABLE);

			// Update the 'parent' values of all parameters and children to lastid
			if (toupdate.Count > 0)
            {
                StringBuilder updateSql = new StringBuilder();
                updateSql.Append("UPDATE ").Append(NODE_TABLE).Append(" SET parent = @p1 WHERE id IN (");
                for (int i = 0; i < toupdate.Count; i++) updateSql.Append(i > 0 ? "," : string.Empty).Append(toupdate[i]);
                updateSql.Append(");");

                result = ExecuteNonQuery(updateSql.ToString(), lastid);

                if (result != toupdate.Count)
				{
					// something happened wrong here! TODO: Throw some exceptions
				}
			}

			return lastid;
		}

		public DataType[] RootNodes
		{
			get
			{
				if (rootNodeCache.Count < 1) UpdateRawNodes();
				return new List<DataType>(rootNodeCache.Values).ToArray();
			}
        }
        #endregion

        #region Modules
        public void UpdateRawModules()
		{
            using (SQLiteDataReader dr = SelectAll(MODULE_TABLE))
            {
                rootModuleCache.Clear();
                while (dr.Read())
                {
                    long mid = (long)dr["id"];
                    rootModuleCache[mid] = ReadModule(dr);
                }
                dr.Close();
            }
		}

		public DModule ReadModule(long id)
		{
            DModule module = null;

            using (SQLiteDataReader dr = SelectByField(MODULE_TABLE, "id", id))
            {
                if (!dr.HasRows) return null;
                else module = ReadModule(dr);

                dr.Close();
            }

            return module;
		}

		public DModule ReadModule(SQLiteDataReader dr)
		{
			if (!dr.HasRows) return null;

            DModule ret = new DModule(null, dr["ModuleFile"] as String);
			ret.ModuleName = dr["ModuleName"] as String;

			long dom = (long)dr["DOMid"];
			ret.dom = rootNodeCache.ContainsKey(dom) ? rootNodeCache[dom] : ReadNode(null, dom);
			rootModuleCache[(long)dr["id"]] = ret;

			return ret;
		}

		public long InsertModule(DModule mod)
		{
			if (mod == null) return 0;

			long domid = InsertNode(mod.dom);
            int result = InsertValues(MODULE_TABLE, null, mod.ModuleName, mod.mod_file, domid);

            return GetLastInsertedId(MODULE_TABLE);
		}

		public DModule[] Modules
		{
			get
			{
				if (rootModuleCache.Count < 1) UpdateRawModules();
				return new List<DModule>(rootModuleCache.Values).ToArray();
			}
        }
        #endregion
        
        #region SqlLite Helpers
        private long GetLastInsertedId(string table)
        {
            object o = ExecuteScalar("SELECT last_insert_rowid() FROM " + MODULE_TABLE);
            return (o != null) ? (long)o : -1;
        }

        private int CreateIndexOnField(string table, string field)
        {
            StringBuilder sqlb = new StringBuilder(127);
            sqlb.Append("CREATE INDEX IF NOT EXISTS idx_").Append(table).Append("_").Append(field)
                .Append(" ON ").Append(table).Append(" (").Append(field).Append(")");

            return ExecuteNonQuery(sqlb.ToString());
        }

        private SQLiteDataReader SelectByField(string table, string field, object fieldData)
        {
            StringBuilder sqlb = new StringBuilder(127);
            sqlb.Append("SELECT * FROM ").Append(table).Append(" WHERE ").Append(field).Append(" = @p1");
            return ExecuteReader(sqlb.ToString(), fieldData);
        }

        private SQLiteDataReader SelectAll(string table)
        {
            StringBuilder sqlb = new StringBuilder(32);
            sqlb.Append("SELECT * FROM ").Append(table);
            return ExecuteReader(sqlb.ToString());
        }

        private int InsertValues(string table, params object[] sqlparams)
        {
            StringBuilder sqlb = new StringBuilder(127);
            sqlb.Append("INSERT INTO ").Append(table).Append(" VALUES (");
            for (int i = 1; i <= sqlparams.Length; i++)
            {
                if (i > 1) sqlb.Append(", ");
                sqlb.Append("@p").Append(i);
            }
            sqlb.Append(");");
            return ExecuteNonQuery(sqlb.ToString(), sqlparams);
        }

        private int ExecuteNonQuery(string sql, params object[] sqlparams)
        {
            System.Diagnostics.Debug.WriteLine(sql);
            SQLiteCommand cmd = CreateCommand();
            cmd.CommandText = sql;
            AppendParameters(cmd, sqlparams);

            return cmd.ExecuteNonQuery();
        }

        private object ExecuteScalar(string sql, params object[] sqlparams)
        {
            System.Diagnostics.Debug.WriteLine(sql);
            SQLiteCommand cmd = CreateCommand();
            cmd.CommandText = sql;
            AppendParameters(cmd, sqlparams);

            return cmd.ExecuteScalar();
        }

        private SQLiteDataReader ExecuteReader(string sql, params object[] sqlparams)
        {
            System.Diagnostics.Debug.WriteLine(sql);
            SQLiteCommand cmd = CreateCommand();
            cmd.CommandText = sql;
            AppendParameters(cmd, sqlparams);

            return cmd.ExecuteReader();
        }

        private void AppendParameters(SQLiteCommand cmd, object[] sqlparams)
        {
            for (int i = 0; i < sqlparams.Length; i++)
            {
                SQLiteParameter p = cmd.CreateParameter();
                p.ParameterName = "@p" + (i + 1);
                p.Value = (sqlparams[i] == null) ? DBNull.Value : sqlparams[i];
                cmd.Parameters.Add(p);
            }
        }

        private SQLiteCommand CreateCommand()
        {
            SQLiteCommand cmd = connection.CreateCommand();
            if (this.transaction != null) cmd.Transaction = this.transaction;

            return cmd;
        }
        #endregion
	}
}
*/