//+------------------------------------------------------------------+
//|                                               SelectionTable.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"


class SelectionTable
{
public:
   SelectionTable();

   void AddSelection(int id, string name);
   string GetNameByID(int id) const;
   int GetIDByName(string id) const;
   int Next(int id) const;
   int Previous(int id) const;
   
   void SetWrongID(int wrongID);
   void SetWrongName(string wrongName);
   
   void CopyFrom(const SelectionTable& other);

private:
   int Pos(int id) const;
   
private:
   int m_IDs[];
   string m_Names[];
   int m_WrongID;
   string m_WrongName;
};

SelectionTable::SelectionTable(void)
{
   m_WrongID = -1;
   m_WrongName = "";
}

void SelectionTable::AddSelection(int id, string name)
{
   int size = ArraySize(m_IDs);
   
   ArrayResize(m_IDs, size + 1);
   ArrayResize(m_Names, size + 1);
   
   m_IDs[size] = id;
   m_Names[size] = name;
}

string SelectionTable::GetNameByID(int id)  const
{
   int size = ArraySize(m_IDs);
   for (int i = 0; i < size; i++)
   {
      if (id == m_IDs[i]) return m_Names[i];
   }
   return m_WrongName;
}

int SelectionTable::GetIDByName(string id)  const
{
   int size = ArraySize(m_Names);
   for (int i = 0; i < size; i++)
   {
      if (id == m_Names[i]) return m_IDs[i];
   }
   return m_WrongID;
}

int SelectionTable::Pos(int id) const
{
   int size = ArraySize(m_IDs);
   for (int i = 0; i < size; i++)
   {
      if (id == m_IDs[i]) return i;
   }
   return -1;
}

int SelectionTable::Next(int id) const
{
   int pos = Pos(id);
   if (pos == -1) return m_WrongID;
   
   if (pos == ArraySize(m_IDs) - 1) pos = 0;
   else pos++;
   
   return m_IDs[pos];
}

int SelectionTable::Previous(int id) const
{
   int pos = Pos(id);
   if (pos == -1) return m_WrongID;

   if (pos == 0) pos = ArraySize(m_IDs) - 1;
   else pos--;
   
   return m_IDs[pos];
}

void SelectionTable::SetWrongID(int wrongID)
{
   m_WrongID = wrongID;
}

void SelectionTable::SetWrongName(string wrongName)
{
   m_WrongName = wrongName;
}

void SelectionTable::CopyFrom(const SelectionTable& other)
{
   ArrayCopy(m_IDs, other.m_IDs);
   ArrayCopy(m_Names, other.m_Names);
   m_WrongID = other.m_WrongID;
   m_WrongName = other.m_WrongName;
}