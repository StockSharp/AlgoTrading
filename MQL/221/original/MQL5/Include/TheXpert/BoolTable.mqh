//+------------------------------------------------------------------+
//|                                                    BoolTable.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

class BoolTable
{
public:
   string GetNameByValue(bool value) const;
   bool GetValueByName(string name);
};

string BoolTable::GetNameByValue(bool value) const
{
   if (value == false) return "False";
   return "True";
}

bool BoolTable::GetValueByName(string name)
{
   string tmp = name;
   StringToLower(tmp);
   
   if (tmp == "false") return false;
   return true;
}