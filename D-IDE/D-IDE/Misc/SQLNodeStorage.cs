using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using D_Parser;
using System.Windows.Forms;
using ICSharpCode.NRefactory;

namespace D_IDE
{
	class SQLNodeStorage : IDisposable
	{
		#region Generic

		public SQLiteConnection Connection;
		public const string NodeTable = "nodes";
		public const string ModuleTable = "modules";
		public Dictionary<long, DataType> RootNodeCache = new Dictionary<long, DataType>();
		public Dictionary<long, DModule> RootModuleCache = new Dictionary<long, DModule>();

        public SQLNodeStorage(string DBFile, bool readOnly)
        {
            StringBuilder connectionString = new StringBuilder();
            connectionString.Append("Data Source=\"" + DBFile + "\"");
            if (readOnly) connectionString.Append(";Read Only=True");

            Connection = new SQLiteConnection(connectionString.ToString());
			Connection.Open();
		}

		public void Close()
		{
			Connection.Close();
		}

		public void CreateModuleTable()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText =
				"CREATE TABLE IF NOT EXISTS " + ModuleTable + " ( " +
				"id INTEGER NULL PRIMARY KEY AUTOINCREMENT," +
				"ModuleName varchar(256) NOT NULL," +
				"ModuleFile varchar(256) NOT NULL," +
				"DOMid INTEGER NOT NULL" +
				");";
			cmd.ExecuteNonQuery();
		}
		public void CreateNodeTable()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText =
				"CREATE TABLE IF NOT EXISTS " + NodeTable + " ( " +
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
				");";
			cmd.ExecuteNonQuery();
		}

		public void TruncateDatabase()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "delete from " + NodeTable + ";delete from " + ModuleTable;
			cmd.ExecuteNonQuery();
		}

		public void Dispose()
		{
			Close();
			RootModuleCache.Clear();
			RootNodeCache.Clear();
		}
		#endregion


		public void BeginTransAction()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "BEGIN TRANSACTION";
			cmd.ExecuteNonQuery();
		}

		public void Commit()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "COMMIT TRANSACTION";
			cmd.ExecuteNonQuery();
		}


		#region Nodes
		public long LastInsertedNodeId
		{
			get
			{
				SQLiteCommand cmd = Connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid() FROM " + NodeTable;
				object o = cmd.ExecuteScalar();
				return o!=null?(long)o:-1;
			}
		}

		public void UpdateRawNodes()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "SELECT * FROM " + NodeTable + " WHERE parent=0";

			SQLiteDataReader dr = cmd.ExecuteReader();
			RootNodeCache.Clear();
			while (dr.Read())
			{
				long id = (long)dr["id"];
				RootNodeCache[id] = ReadNode(null, dr);
			}
		}

		public DataType ReadNode(DataType parent, long id)
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "SELECT * FROM " + NodeTable + " where id=" + id.ToString();
			SQLiteDataReader dr = cmd.ExecuteReader();
			if (!dr.HasRows) return null;
			dr.Read();

			return ReadNode(parent, dr);
		}

		public DataType ReadNode(DataType parent, SQLiteDataReader dr)
		{
			if (!dr.HasRows) return null;

			DataType ret = new DataType();
			long id = (long)dr["id"];

			ret.fieldtype = (FieldType)(int)(long)dr["fieldtype"];
			ret.name = (string)dr["name"];
			ret.TypeToken = (int)(long)dr["typetoken"];
			ret.type = (string)dr["type"];
			long parid = (long)dr["parent"];
			ret.parent = parent;//RawNodes.ContainsKey(parid)?RawNodes[parid]:/*ReadNode(parid)*/;

			string[] ts = ((string)dr["location"]).Split(';');
			ret.StartLocation = new Location(Convert.ToInt32(ts[0]), Convert.ToInt32(ts[1]));
			ts = ((string)dr["endlocation"]).Split(';');
			ret.EndLocation = new Location(Convert.ToInt32(ts[0]), Convert.ToInt32(ts[1]));
			ts = ((string)dr["modifiers"]).Split(';');
			foreach (string s in ts)
				if (!String.IsNullOrEmpty(s)) ret.modifiers.Add(Convert.ToInt32(s));

			ret.module = (string)dr["module"];
			ret.value = (string)dr["value"];

			ts = ((string)dr["param"]).Split(';');
			foreach (string s in ts)
			{
				if (String.IsNullOrEmpty(s)) continue;
				long cid = Convert.ToInt64(s);
				if (cid == id) continue;
				ret.param.Add(RootNodeCache.ContainsKey(cid) ? RootNodeCache[cid] : ReadNode(ret, cid));
			}
			ts = ((string)dr["children"]).Split(';');
			foreach (string s in ts)
			{
				if (String.IsNullOrEmpty(s)) continue;
				long cid = Convert.ToInt64(s);
				if (cid == id) continue;
				ret.Children.Add(RootNodeCache.ContainsKey(cid) ? RootNodeCache[cid] : ReadNode(ret, cid));
			}

			ret.superClass = (string)dr["super"];
			ret.implementedInterface = (string)dr["interface"];

			return ret;
		}

		public long InsertNode(DataType dt)
		{
			if (dt == null) return 0;
			SQLiteCommand cmd = Connection.CreateCommand();

			List<long> toupdate = new List<long>(dt.param.Count + dt.Children.Count);

			string cmdtxt =
				"INSERT INTO " + NodeTable + " VALUES(" +
				"null," + ((int)dt.fieldtype).ToString() + "," +
				"'" + dt.name + "'," +
				((int)dt.TypeToken).ToString() + "," +
				"'" + dt.type + "'," +
				"0," + // parent - this gets modified afterwards
				"'" + dt.StartLocation.Column.ToString() + ";" + dt.StartLocation.Line.ToString() + "'," +
				"'" + dt.EndLocation.Column.ToString() + ";" + dt.EndLocation.Line.ToString() + "'," +

				"'";
			foreach (int mod in dt.modifiers)
				cmdtxt += mod.ToString() + ";";
			cmdtxt += "','" + dt.module + "','" + dt.value.Replace("'", "") + "','";
			foreach (DataType c in dt.param)
			{
				long tl = InsertNode(c);
				toupdate.Add(tl);
				cmdtxt += tl.ToString() + ";";
			}
			cmdtxt += "','";
			foreach (DataType c in dt.Children)
			{
				long tl = InsertNode(c);
				toupdate.Add(tl);
				cmdtxt += tl.ToString() + ";";
			}
			cmdtxt += "','" + dt.superClass + "','" + dt.implementedInterface + "');";
			cmd.CommandText = cmdtxt;
			cmd.ExecuteNonQuery();
			long lastid = LastInsertedNodeId;

			// Update the 'parent' values of all parameters and children to lastid
			if (toupdate.Count > 0)
			{
				cmd = Connection.CreateCommand();
				cmd.CommandText = "UPDATE " + NodeTable + " SET parent=" + lastid.ToString() + " WHERE id IN (";
				foreach (long ci in toupdate) cmd.CommandText += ci.ToString() + ",";
				cmd.CommandText = cmd.CommandText.TrimEnd(',') + ")";

				if (cmd.ExecuteNonQuery() != toupdate.Count)
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
				if (RootNodeCache.Count < 1) UpdateRawNodes();
				return new List<DataType>(RootNodeCache.Values).ToArray();
			}
		}
		#endregion



		#region Modules
		public long LastInsertedModuleId
		{
			get
			{
				SQLiteCommand cmd = Connection.CreateCommand();
				cmd.CommandText = "SELECT last_insert_rowid() FROM " + ModuleTable;
				object o = cmd.ExecuteScalar();
				return o != null ? (long)o : -1;
			}
		}

		public void UpdateRawModules()
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "SELECT * FROM " + ModuleTable;
			SQLiteDataReader dr = cmd.ExecuteReader();
			
			RootModuleCache.Clear();
			while (dr.Read())
			{
				long mid = (long)dr["id"];
				RootModuleCache[mid] = ReadModule(dr);
			}
		}

		public DModule ReadModule(long id)
		{
			SQLiteCommand cmd = Connection.CreateCommand();
			cmd.CommandText = "SELECT * FROM " + ModuleTable + " where id=" + id.ToString();
			SQLiteDataReader dr = cmd.ExecuteReader();
			if (!dr.HasRows) return null;
			dr.Read();

			return ReadModule(dr);
		}

		public DModule ReadModule(SQLiteDataReader dr)
		{
			if (!dr.HasRows) return null;

			DModule ret = new DModule(null, (string)dr["ModuleFile"]);
			ret.ModuleName = (string)dr["ModuleName"];

			long dom = (long)dr["DOMid"];
			ret.dom = RootNodeCache.ContainsKey(dom) ? RootNodeCache[dom] : ReadNode(null, dom);
			RootModuleCache[(long)dr["id"]] = ret;
			return ret;
		}

		public long InsertModule(DModule mod)
		{
			if (mod == null) return 0;
			SQLiteCommand cmd = Connection.CreateCommand();

			long domid = InsertNode(mod.dom);
			cmd.CommandText =
				"INSERT INTO " + ModuleTable + " VALUES(null,'" + mod.ModuleName + "','" + mod.mod_file + "'," + domid.ToString() + ");";

			cmd.ExecuteNonQuery();

			return LastInsertedModuleId;
		}

		public DModule[] Modules
		{
			get
			{
				if (RootModuleCache.Count < 1) UpdateRawModules();
				return new List<DModule>(RootModuleCache.Values).ToArray();
			}
		}
		#endregion
	}
}
