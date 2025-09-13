//+------------------------------------------------------------------+
//|                                       DBquickStartWithStruct.mq5 |
//|                             Copyright 2019-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2019-2022, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.0"

//+------------------------------------------------------------------+
//| Data structure for single record in database                     |
//+------------------------------------------------------------------+
struct Person
{
   int     id;
   string  name;
   int     age;
   string  address;
   double  salary;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string filename = "company.sqlite";
   // create or open database
   const int db = DatabaseOpen(filename, DATABASE_OPEN_READWRITE | DATABASE_OPEN_CREATE);
   if(db == INVALID_HANDLE)
   {
      Print("DB: ", filename, " open failed with code ", _LastError);
      return;
   }

   // if the table COMPANY exists, delete it
   if(DatabaseTableExists(db, "COMPANY"))
   {
      if(!DatabaseExecute(db, "DROP TABLE COMPANY"))
      {
         Print("Failed to drop table COMPANY with code ", _LastError);
         DatabaseClose(db);
         return;
      }
   }

   // create the table COMPANY (empty) with 5 columns
   if(!DatabaseExecute(db,
      "CREATE TABLE COMPANY("
      "ID      INT       PRIMARY KEY     NOT NULL,"
      "NAME    TEXT                      NOT NULL,"
      "AGE     INT                       NOT NULL,"
      "ADDRESS CHAR(50),"
      "SALARY  REAL);"))
   {
      Print("DB: ", filename, " create table failed with code ", _LastError);
      DatabaseClose(db);
      return;
   }

   // add some data (4 records) in the table 
   if(!DatabaseExecute(db,
      "INSERT INTO COMPANY (ID,NAME,AGE,ADDRESS,SALARY) VALUES (1,'Paul',32,'California',25000.00); "
      "INSERT INTO COMPANY (ID,NAME,AGE,ADDRESS,SALARY) VALUES (2,'Allen',25,'Texas',15000.00); "
      "INSERT INTO COMPANY (ID,NAME,AGE,ADDRESS,SALARY) VALUES (3,'Teddy',23,'Norway',20000.00);"
      "INSERT INTO COMPANY (ID,NAME,AGE,ADDRESS,SALARY) VALUES (4,'Mark',25,'Rich-Mond',65000.00);"))
   {
      Print("DB: ", filename, " insert failed with code ", _LastError);
      DatabaseClose(db);
      return;
   }

   // prepare SQL query and get a handle for it
   int request = DatabasePrepare(db, "SELECT * FROM COMPANY WHERE SALARY>15000");
   if(request == INVALID_HANDLE)
   {
      Print("DB: ", filename, " request failed with code ", _LastError);
      DatabaseClose(db);
      return;
   }

   // print all records with salary larger than 15000
   Person person;
   Print("Persons with salary > 15000:");
   // keep reading while DatabaseReadBind returns true and fills the struct person with data
   for(int i = 0; DatabaseReadBind(request, person); i++)
   {
      Print(i, ":  ", person.id, " ", person.name, " ", person.age, " ", person.address, " ", person.salary);
   }
   // release query handle after use
   DatabaseFinalize(request);

   Print("Some statistics:");
   // prepare new query for sum of salaries
   request = DatabasePrepare(db, "SELECT SUM(SALARY) FROM COMPANY");
   if(request == INVALID_HANDLE)
   {
      Print("DB: ", filename, " request failed with code ", _LastError);
      DatabaseClose(db);
      return;
   }
   // this should be a single "record" with a single value
   if(DatabaseRead(request))
   {
      double sum;
      DatabaseColumnDouble(request, 0, sum);
      Print("Total salary=", sum);
   }
   else
   {
      Print("DB: DatabaseRead() failed with code ", _LastError);
   }
   // release the handle after use
   DatabaseFinalize(request);

   // prepare another query for average salary
   request = DatabasePrepare(db, "SELECT AVG(SALARY) FROM COMPANY");
   if(request == INVALID_HANDLE)
   {
      Print("DB: ", filename, " request failed with code ", _LastError);
      DatabaseClose(db);
      return;
   }
   if(DatabaseRead(request))
   {
      double average;
      DatabaseColumnDouble(request, 0, average);
      Print("Average salary=", average);
   }
   else
   {
      Print("DB: DatabaseRead() failed with code ", _LastError);
   }
   DatabaseFinalize(request);

   // close the database
   DatabaseClose(db);
}
//+------------------------------------------------------------------+
/*
   example output:
   Persons with salary > 15000:
   0:  1 Paul 32 California 25000.0
   1:  3 Teddy 23 Norway 20000.0
   2:  4 Mark 25 Rich-Mond  65000.0
   Some statistics:
   Total salary=125000.0
   Average salary=31250.0
*/
//+------------------------------------------------------------------+
