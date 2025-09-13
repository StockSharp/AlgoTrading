//+------------------------------------------------------------------+
//|                                                     DBSQLite.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Basic ORM (Object-Relational Mapping) MQL5 <-> SQLite            |
//+------------------------------------------------------------------+
// NB: structures with simple types and strings are allowed only
// for binding to DB

// use this in your code to disable debug logging by PRTF
// #define PRTF

#ifndef PRTF
#include <MQL5Book/PRTF.mqh>
#endif
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/TypeName.mqh>

// use this to fill empty MqlParam.string_value with param name by default
// #define DB_PARAM_NAMES

#define TYPE_NULL ((ENUM_DATATYPE)(0))
#define TYPE_BLOB ((ENUM_DATATYPE)(-1))

#define DB_FIELD(S,T,N)    struct S##_##T##_##N: DBEntity<S>::DBField<T> { S##_##T##_##N() : DBEntity<S>::DBField<T>(#N) {}}; const S##_##T##_##N _##S##_##T##_##N;
#define DB_FIELD_C1(S,T,N,C1) struct S##_##T##_##N: DBEntity<S>::DBField<T> { S##_##T##_##N() : DBEntity<S>::DBField<T>(#N, C1) {}}; const S##_##T##_##N _##S##_##T##_##N;
#define DB_FIELD_C2(S,T,N,C1,C2) struct S##_##T##_##N: DBEntity<S>::DBField<T> { S##_##T##_##N() : DBEntity<S>::DBField<T>(#N, C1 + " " + C2) {}}; const S##_##T##_##N _##S##_##T##_##N;

/*
   DB_FIELD expands to:
   
   struct Struct_Type_Name: DBEntity<Struct>::DBField<Type>
   {
      Struct_Type_Name() : DBEntity<Struct>::DBField<Type>("Name"){}
   };
   const Struct_Type_Name _Struct_Type_Name;
*/

//+------------------------------------------------------------------+
//| String templates of DB-constraints per field (useful reference)  |
//+------------------------------------------------------------------+
namespace DB_CONSTRAINT
{
   const string PRIMARY_KEY = "PRIMARY KEY";
   const string UNIQUE = "UNIQUE";
   const string NOT_NULL = "NOT NULL";
   const string CHECK = "CHECK (%s)";                    // expression required
   const string CURRENT_TIMESTAMP = "CURRENT_TIMESTAMP"; // "CURRENT_TIMESTAMP" doesn't work in SQLite as expected
   const string CURRENT_TIME = "CURRENT_TIME";           // -- // --
   const string CURRENT_DATE = "CURRENT_DATE";           // -- // --
   const string AUTOINCREMENT = "AUTOINCREMENT";
   const string DEFAULT = "DEFAULT (%s)";                // expression (e.g. constant, function) required
}

//+------------------------------------------------------------------+
//| General DB types                                                 |
//+------------------------------------------------------------------+
namespace DB_TYPE
{
   const string INTEGER = "INTEGER";
   const string REAL = "REAL";
   const string TEXT = "TEXT";
   const string BLOB = "BLOB"; // "NONE" also ok, both means anything
   const string NONE = "NONE";
   const string _NULL = "NULL";
}

//+------------------------------------------------------------------+
//| Scalar value type is used to instantiate simple DBRow objects    |
//+------------------------------------------------------------------+
struct DBValue
{
   int fake; // not used, struct can not be empty
};

//+------------------------------------------------------------------+
//| Pseudo-type to declare blob fields in objects using DB_FIELD     |
//+------------------------------------------------------------------+
enum blob
{
};

//+------------------------------------------------------------------+
//| Description of table column, returned by pragma table_info('tbl')|
//+------------------------------------------------------------------+
struct DBTableColumn
{
   int cid;
   string name;
   string type;
   bool not_null;
   string default_value;
   bool primary_key;
};

//+------------------------------------------------------------------+
//| DB-aware meta-type                                               |
//+------------------------------------------------------------------+
template<typename S>
struct DBEntity
{
   template<typename T>
   struct DBField
   {
      T f;
      DBField(const string name, const string constraints = "")
      {
         const int n = EXPAND(prototype);
         prototype[n][0] = affinity(TYPENAME(T));
         prototype[n][1] = name;
         if(StringLen(constraints) > 0            // eliminates STRING_SMALL_LEN(5035)
            && StringFind(constraints, "%") >= 0)
         {
            Print("Constraint requires an expression (skipped): ", constraints);
         }
         else
         {
            prototype[n][2] = constraints;
         }
      }
      
      static string affinity(const string type)
      {
         const static string ints[] =
         {
            "bool", "char", "short", "int", "long",
            "uchar", "ushort", "uint", "ulong", "datetime",
            "color", "enum"
         };
         for(int i = 0; i < ArraySize(ints); ++i)
         {
            if(type == ints[i]) return DB_TYPE::INTEGER;
         }
         
         if(type == "float" || type == "double") return DB_TYPE::REAL;
         
         if(type == "string") return DB_TYPE::TEXT;
         
         return DB_TYPE::BLOB;
      }
   };
   
   static string prototype[][3]; // 0 - type, 1 - name, 2 - constraint
};

template<typename S>
static string DBEntity::prototype[][3];

//+------------------------------------------------------------------+
//| Meta-info about columns of query results                         |
//+------------------------------------------------------------------+
struct DBRowColumn
{
   string name;
   // NB: the next is per row information, because in SQLite
   // fields are dynamically typed (properties defined at table creation
   // are just recommendations and can be overwritten by actual data being stored)
   ENUM_DATABASE_FIELD_TYPE type;
   int size;
};

//+------------------------------------------------------------------+
//| Reading query results into general purpose array of MqlParams    |
//+------------------------------------------------------------------+
class DBRow
{
protected:
   const int query;
   int columns;
   DBRowColumn info[];
   MqlParam data[];
   const bool cache;
   int cursor;
   
   virtual bool DBread()
   {
      return PRTF(DatabaseRead(query));
   }
   
public:
   DBRow(const int q, const bool c = false):
      query(q), cache(c), columns(0), cursor(-1)
   {
   }
   
   int length() const
   {
      return columns;
   }
   
   DBRowColumn description(const int i) const
   {
      static const DBRowColumn null = {};
      if(i < 0 || i >= columns) return null;
      return info[i];
   }
   
   virtual bool next()
   {
      if(cache && cursor >= 0)
      {
         SetUserError(1);
         return false; // can't get next on cached instance
      }
      
      const bool success = DBread();
      if(success)
      {
         if(cursor == -1)
         {
            columns = DatabaseColumnsCount(query);
            ArrayResize(info, columns);
            if(cache) ArrayResize(data, columns);
            for(int i = 0; i < columns; ++i)
            {
               DatabaseColumnName(query, i, info[i].name);
               info[i].type = DatabaseColumnType(query, i);
               info[i].size = DatabaseColumnSize(query, i);
               if(cache) data[i] = this[i];
            }
         }
         else
         {
            // NB: since value type in every column can change for specific row,
            // it could be useful to rescan DatabaseColumnType and DatabaseColumnSize 
         }
         ++cursor;
      }
      return success;
   }
   
   int name2index(const string name) const
   {
      for(int i = 0; i < columns; ++i)
      {
         if(name == info[i].name) return i;
      }
      Print("Wrong column name: ", name);
      SetUserError(3); // wrong column name
      return -1;
   }

   // read column value by name
   MqlParam operator[](const string name) const
   {
      const int i = name2index(name);
      if(i != -1) return this[i];
      static MqlParam param = {};
      return param;
   }
   
   // read column value by index
   virtual MqlParam operator[](const int i = 0) const
   {
      MqlParam param = {};
      if(i < 0 || i >= columns) return param;
      if(ArraySize(data) > 0 && cursor != -1) // return cache, if exists
      {
         return data[i];
      }
      #ifdef DB_PARAM_NAMES
      param.string_value = "[" + names[i] + "]";
      #endif
      switch(info[i].type)
      {
      case DATABASE_FIELD_TYPE_INTEGER:
         switch(info[i].size)
         {
         case 1:
            param.type = TYPE_CHAR;
            break;
         case 2:
            param.type = TYPE_SHORT;
            break;
         case 4:
            param.type = TYPE_INT;
            break;
         case 8:
         default:
            param.type = TYPE_LONG;
            break;
         }
         DatabaseColumnLong(query, i, param.integer_value);
         break;
      case DATABASE_FIELD_TYPE_FLOAT:
         param.type = info[i].size == 4 ? TYPE_FLOAT : TYPE_DOUBLE;
         DatabaseColumnDouble(query, i, param.double_value);
         break;
      case DATABASE_FIELD_TYPE_TEXT:
         param.type = TYPE_STRING;
         DatabaseColumnText(query, i, param.string_value);
         break;
      case DATABASE_FIELD_TYPE_BLOB: // this is just a workaround
         {                           // there is no other means to pass blob via MqlParam
            uchar blob[];            // use getBlob to get exact binary data
            DatabaseColumnBlob(query, i, blob);
            uchar key[], text[];
            if(CryptEncode(CRYPT_BASE64, blob, key, text))
            {
               param.string_value = CharArrayToString(text);
            }
         }
         param.type = TYPE_BLOB;
         break;
      case DATABASE_FIELD_TYPE_NULL:
         param.type = TYPE_NULL;
         break;
      }
      return param;
   }

   // read blob column value by index
   template<typename S>
   int getBlob(const int i, S &object[])
   {
      if(ArraySize(data) > 0)
      {
         Print("Cached rows do not support blobs");
         SetUserError(2);
         return false; // can't get blob from cache: raw blobs are not cachable
      }
      return DatabaseColumnBlob(query, i, object);
   }

   // read blob column value by name
   template<typename S>
   int getBlob(const string name, S &object[])
   {
      const int i = name2index(name);
      if(i != -1)
      {
         return getBlob(i, object);
      }
      return 0;
   }
   
   void readAll(MqlParam &params[]) const
   {
      ArrayResize(params, columns);
      for(int i = 0; i < columns; ++i)
      {
         params[i] = this[i];
      }
   }
   
   void moveAll(MqlParam &params[])
   {
      if(cache) // only cached row fills 'data' array
      {
         ArraySwap(params, data);
      }
   }
   
};

//+------------------------------------------------------------------+
//| DB row for reading DB records into objects/structs               |
//+------------------------------------------------------------------+
template<typename S>
class DBRowStruct: public DBRow
{
protected:
   S object;
   
   virtual bool DBread() override
   {
      // NB: derived structures are not allowed;
      // number of struct fields must not exceed number of columns in table/query
      return PRTF(DatabaseReadBind(query, object));
   }

public:
   DBRowStruct(const int q, const bool c = false): DBRow(q, c)
   {
   }

   S get() const
   {
      return object;
   }
};

//+------------------------------------------------------------------+
//| DB query wrapper class                                           |
//+------------------------------------------------------------------+
class DBQuery
{
protected:
   const string sql;
   const int db;
   const int handle;
   AutoPtr<DBRow> row;    // current
   AutoPtr<DBRow> rows[]; // cached (if requested)
   
public:
   DBQuery(const int owner, const string s): db(owner), sql(s),
      handle(PRTF(DatabasePrepare(db, sql)))
   {
      row = NULL;
   }
   
   ~DBQuery()
   {
      DatabaseFinalize(handle);
   }
   
   bool isValid() const
   {
      return handle != INVALID_HANDLE;
   }
   
   int getHandle() const
   {
      return handle;
   }
   
   int getDB() const
   {
      return db;
   }
   
   template<typename T>
   bool bind(const int index, const T value)
   {
      return PRTF(DatabaseBind(handle, index, value));
   }

   template<typename T>
   bool bindBlob(const int index, const T &value[])
   {
      return PRTF(DatabaseBindArray(handle, index, value));
   }
   
   bool bindNull(const int index)
   {
      static const uchar null[] = {};
      return bindBlob(index, null);
   }
   
   template<typename S>
   DBRow *start() // S &
   {
      DatabaseReset(handle);
      row = TYPENAME(S) == "DBValue" ? new DBRow(handle) : new DBRowStruct<S>(handle);
      return row[];
   }

   virtual bool reset()
   {
      return DatabaseReset(handle);
   }
   
   // use 'execute' for parametric queries with inputs and NO outputs (batch DB edit)
   // use 'DBRow/DBRowStruct::next' to get outputs/results from DB
   virtual bool execute()
   {
      // NB: can return false and set error DATABASE_NO_MORE_DATA(5126),
      // which is NOT an error!
      return PRTF(DatabaseRead(handle));
   }
   
   // get an array of cached rows
   bool readAll(DBRow *&result[], const bool detach = false)
   {
      DatabaseReset(handle);
      
      DBRow *temp;
      while((temp = new DBRow(handle, true)) != NULL && temp.next()) // generate new row on each iteration
      {
         PUSH(result, temp);
         if(!detach) PUSH(rows, temp); // keep trace of objects to clean them up
         temp = NULL;
      }
      delete temp; // clean up excessive instance for ERR_DATABASE_NO_MORE_DATA
      return true;
   }
   
   template<typename S>
   bool readAll(S &result[])
   {
      DatabaseReset(handle);
      
      DBRowStruct<S> *temp = new DBRowStruct<S>(handle);
      while(temp.next())
      {
         PUSH(result, temp.get());
      }
      delete temp;
      return true;
   }
};

//+------------------------------------------------------------------+
//| Main database wrapper class                                      |
//+------------------------------------------------------------------+
class DBSQLite
{
protected:
   const string path;
   const int handle;
   const uint flags;
   int transaction;
   AutoPtr<DBQuery> queries[];

public:
   DBSQLite(const string file, const uint opts =
      DATABASE_OPEN_CREATE | DATABASE_OPEN_READWRITE):
      path(file), flags(opts), handle(DatabaseOpen(file, opts)), transaction(0)
   {
   }
   
   ~DBSQLite(void)
   {
      if(handle != INVALID_HANDLE)
      {
         DatabaseClose(handle);
      }
   }
   
   int getHandle() const
   {
      return handle;
   }

   bool isOpen() const
   {
      return handle != INVALID_HANDLE;
   }
   
   bool execute(const string sql) // query without bound inputs/outputs (hardcoded logic)
   {
      return PRTF(DatabaseExecute(handle, sql));
   }

   DBQuery *prepare(const string sql)
   {
      DBQuery *q = new DBQuery(handle, sql);
      if(!q.isValid())
      {
         delete q;
         return NULL;
      }
      return PUSH(queries, q);
   }

   template<typename S>
   string columns(const string table_constraints = "") const
   {
      static const string continuation = ",\n";
      string result = "";
      const int n = ArrayRange(DBEntity<S>::prototype, 0);
      if(!n) return NULL;
      for(int i = 0; i < n; ++i)
      {
         result += StringFormat("%s%s %s %s",
            i > 0 ? continuation : "",
            DBEntity<S>::prototype[i][1], DBEntity<S>::prototype[i][0], DBEntity<S>::prototype[i][2]);
      }
      if(StringLen(table_constraints))
      {
         result += continuation + table_constraints;
      }
      return result;
   }
   
   template<typename S>
   bool createTable(const bool not_exist = false, const string name = NULL,
      const string table_constraints = "") const
   {
      const static string query = "CREATE TABLE %s %s (%s);";
      const string fields = columns<S>(table_constraints);
      if(fields == NULL)
      {
         Print("Structure '", TYPENAME(S), "' with table fields is not initialized");
         SetUserError(4);
         return false;
      }
      // attempt to create a table that already exists
      // without using IF NOT EXISTS will give an error.
      const string sql = StringFormat(query,
         (not_exist ? "IF NOT EXISTS" : ""), StringLen(name) ? name : TYPENAME(S), fields);
      return DatabaseExecute(handle, PRTF(sql));
   }

   template<typename S>
   bool createSimpleTable() const
   {
      return createTable(true);
   }

   bool hasTable(const string table) const
   {
      return DatabaseTableExists(handle, table);
   }

   bool deleteTable(const string name) const
   {
      const static string query = "DROP TABLE IF EXISTS '%s';";
      return DatabaseExecute(handle, StringFormat(query, name));
      /*
      // equivalent:
      if(!DatabaseTableExists(handle, name)) return true;
      if(!DatabaseExecute(handle, StringFormat("DROP TABLE '%s';", name))) return false;
      return !DatabaseTableExists(handle, name)
         && ResetLastErrorOnCondition(_LastError == ERR_DATABASE_NO_MORE_DATA);
      */
   }

   static bool ResetLastErrorOnCondition(const bool cond)
   {
      if(cond)
      {
         ResetLastError();
         return true;
      }
      return false;
   }

   template<typename S>
   bool insert(S &objects[], const string table = NULL)
   {
      const static string query = "INSERT INTO '%s' VALUES(%s) RETURNING rowid;";
      const string sql = StringFormat(query, StringLen(table) ? table : TYPENAME(S), qlist<S>());
      DBQuery q(handle, PRTF(sql));
      if(!q.isValid()) return false;
      DBRow *r = q.start<DBValue>();
      for(int i = 0; i < ArraySize(objects); ++i)
      {
         if(objects[i].bindAll(q))
         {
            if(r.next()) // we expect one row with single value of new rowid
            {
               objects[i].rowid(r[0].integer_value);
            }
         }
         q.reset();
      }
      
      return true;
   }

   template<typename S>
   long insert(S &object, const string table = NULL)
   {
      const static string query = "INSERT INTO '%s' VALUES(%s) RETURNING rowid;";
      const string sql = StringFormat(query, StringLen(table) ? table : TYPENAME(S), qlist<S>());
      DBQuery q(handle, PRTF(sql));
      if(!q.isValid()) return 0;
      if(object.bindAll(q))
      {
         DBRow *r = q.start<DBValue>(); // we expect one row with single value of new rowid
         if(r.next())
         {
            return object.rowid(r[0].integer_value);
         }
      }
      
      return 0;
   }

   template<typename S>
   string qlist() const
   {
      const int n = ArrayRange(DBEntity<S>::prototype, 0);
      string result = "?1";
      for(int i = 1; i < n; ++i)
      {
         const string constraint = DBEntity<S>::prototype[i][2];
         if(StringLen(constraint) > 0 && StringFind(constraint, DB_CONSTRAINT::CURRENT_TIMESTAMP) >= 0)
         // workaround for SQLite bug with not setting CURRENT_TIMESTAMP (works both on inserts and updates)
            result += ",STRFTIME('%s')"; // TODO: replace with UNIXEPOCH() after SQLite v.3.38
         else
            result += StringFormat(",?%d", (i + 1));
      }
      return result;
   }

   template<typename S>
   string namelist() const
   {
      const int n = ArrayRange(DBEntity<S>::prototype, 0);
      string result = DBEntity<S>::prototype[0][1];
      for(int i = 1; i < n; ++i)
      {
         result += "," + DBEntity<S>::prototype[i][1];
      }
      return result;
   }

   template<typename S>
   bool update(S &object, const string condition = NULL, const string table = NULL)
   {
      const static string query = "UPDATE '%s' SET (%s)=(%s) %s;";
      const string sql = StringFormat(query, StringLen(table) ? table : TYPENAME(S),
         namelist<S>(), qlist<S>(),
         StringLen(condition) ? condition : StringFormat("WHERE rowid=%ld", object.rowid()));
      DBQuery q(handle, PRTF(sql));
      if(!q.isValid()) return false;
      if(object.bindAll(q))
      {
         q.execute();
      }
      return ResetLastErrorOnCondition(_LastError == ERR_DATABASE_NO_MORE_DATA);
   }

   template<typename S>
   bool remove(S &object, const string table = NULL, const string column = "rowid")
   {
      const static string query = "DELETE FROM '%s' WHERE %s=%ld;";
      const string sql = StringFormat(query, StringLen(table) ? table : TYPENAME(S),
         column, object.rowid());
      DBQuery q(handle, PRTF(sql));
      if(!q.isValid()) return false;
      q.execute();
      return ResetLastErrorOnCondition(_LastError == ERR_DATABASE_NO_MORE_DATA);
   }

   template<typename S>
   bool read(const long rowid, S &s, const string table = NULL, const string column = "rowid")
   {
      // NB: INTEGER PRIMARY KEY column may not be present in S explicitly,
      // then selecting '*' will not return 'rowid'
      const static string query = "SELECT * FROM '%s' WHERE %s=%ld;";
      const string sql = StringFormat(query,
         StringLen(table) ? table : TYPENAME(S), column, rowid);
      DBQuery q(handle, PRTF(sql));
      if(!q.isValid()) return false;
      DBRowStruct<S> *r = q.start<S>();
      if(r.next())
      {
         s = r.get();
         return true;
      }
      return false;
   }

   bool read(const long rowid, DBRow *&r, const string table, const string column = "rowid")
   {
      // NB: INTEGER PRIMARY KEY column may not be present in S explicitly,
      // then selecting '*' will not return 'rowid'
      const static string query = "SELECT * FROM '%s' WHERE %s=%ld;";
      const string sql = StringFormat(query, table, column, rowid);
      DBQuery *q = new DBQuery(handle, PRTF(sql));
      if(q.isValid())
      {
         r = q.start<DBValue>();
         if(r.next())
         {
            PUSH(queries, q);
            return true;
         }
      }
      delete q;
      return false;
   }
   
   bool begin()
   {
      if(transaction > 0)   // already in transaction
      {
         transaction++;     // track nesting level
         return true; 
      }
      return (bool)(transaction = PRTF(DatabaseTransactionBegin(handle)));
   }
   
   bool commit()
   {
      if(transaction > 0)
      {
         if(--transaction == 0) // outermost transaction
            return PRTF(DatabaseTransactionCommit(handle));
      }
      return false;
   }

   bool rollback()
   {
      if(transaction > 0)
      {
         if(--transaction == 0)
            return PRTF(DatabaseTransactionRollback(handle));
      }
      return false;
   }
};

//+------------------------------------------------------------------+
//| DB transaction guard                                             |
//+------------------------------------------------------------------+
class DBTransaction
{
   DBSQLite *db;
   const bool autocommit;
public:
   DBTransaction(DBSQLite &owner, const bool c = false): db(&owner), autocommit(c)
   {
      if(CheckPointer(db) != POINTER_INVALID)
      {
         db.begin();
      }
   }

   ~DBTransaction()
   {
      if(CheckPointer(db) != POINTER_INVALID)
      {
         autocommit ? db.commit() : db.rollback();
      }
   }
   
   bool commit()
   {
      if(CheckPointer(db) != POINTER_INVALID)
      {
         const bool done = db.commit();
         db = NULL;
         return done;
      }
      return false;
   }
};
//+------------------------------------------------------------------+
