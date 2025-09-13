#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include <VirtualKeys.mqh>

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

#define UID_GLOBAL "Impl_CommonUIDIdentifier"
#define TIMEOUT_SECONDS 10

class CommonUID
{
   public:
      CommonUID();
      
      int ID() const {return m_ID;}
      
   private:
      int m_ID;
};

CommonUID::CommonUID()
{
   uint startTicks = GetTickCount();
   
   int hFile = FileOpen(UID_GLOBAL, FILE_READ | FILE_WRITE);
   while (hFile == INVALID_HANDLE)
   {
      uint now = GetTickCount();
      if (now < startTicks) startTicks = now;
      
      if (now - startTicks > TIMEOUT_SECONDS*1000)
      {
         Alert("CommonUID: unexpected situation -- trying to lock finished by timeout");
         return;
      }
      
      while (GetTickCount() - now < 100 || GetTickCount() < now){}
      hFile = FileOpen(UID_GLOBAL, FILE_READ | FILE_WRITE);
   }
   
   if (GlobalVariableCheck(UID_GLOBAL) == 0)
   {
      m_ID = 1;
   }
   else
   {
      m_ID = int(GlobalVariableGet(UID_GLOBAL) + 1);
   }
   
   GlobalVariableSet(UID_GLOBAL, m_ID);
   FileClose(hFile);
}

int GetUID() export
{
   CommonUID id;
   return id.ID();
}

class ReleaseTrigger
{
   public:
      ReleaseTrigger()
      {
         m_Release = false;
      }
   
      bool m_Release;
};

ReleaseTrigger ReleaseTriggerImpl;

void SetRelease(bool isRelease = true) export
{
   ReleaseTriggerImpl.m_Release = isRelease;
}

void Assert(bool condition, string message = "") export
{
   if (!condition && !ReleaseTriggerImpl.m_Release)
   {
      string msg = "Assertion failed";
      if (message != "")
      {
         msg = msg + ", assert message: " + message;
      }
      
      Alert(msg);
   }
}

void PreparePrice(string symbol, double price, string& pre, string& large, string& post)
{
   int digits = int(SymbolInfoInteger(symbol, SYMBOL_DIGITS));
   double norm = NormalizeDouble(price, digits);
   
   int shift = 2;

   double first = norm;
   while(first > 10)
   {
      first /= 10;
   }
   
   if (first < 5) shift += 1;
   
   pre = DoubleToString(norm, digits);
   
   int dotPos = StringFind(pre, ".");
   if (dotPos != -1 && dotPos < 4) shift += 1;
   
   post = StringSubstr(pre, shift);
   pre = StringSubstr(pre, 0, shift);
   large = "";
   
   int count = 0;
   int pos = 0;
   while (count < 2)
   {
      ushort c = StringGetCharacter(post, pos);
      if (c != '.') count++;
      pos++;
      
      large = large + ShortToString(c);
   }
   
   post = StringSubstr(post, pos);
}

int DoubleDigits(double value)
{
   int res = 0;
   
   double iValue = NormalizeDouble(value, 0);
   double dValue = NormalizeDouble(value, 10);

   while (dValue - iValue != 0)
   {
      dValue = NormalizeDouble(dValue*10, 10);
      iValue = NormalizeDouble(dValue, 0);
      res++;
   }
   
   return res;
}

double PriceOnDropped(int y)
{
   int height = int(ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, 0));
   int pos = y;
   if (pos >= height) pos = height - 1;
   double max = ChartGetDouble(0, CHART_PRICE_MAX, 0);
   double min = ChartGetDouble(0, CHART_PRICE_MIN, 0);
   
   if (max <= min) return max;
   if (height <= 1) return max;
   
   return min + (max - min)*(height - 1 - pos)/(height - 1);
}


enum ETimeKind
{
   ETIME_LOCAL,
   ETIME_SERVER,
   ETIME_CET,
   ETIME_MOSCOW,
   ETIME_EST,
   ETIME_GMT
};

datetime FromGMT(int offset)
{
   static int hour = 60*60;
   return TimeGMT() + offset*hour;
}

datetime GetTime(ETimeKind time)
{
   switch (time)
   {
      case ETIME_LOCAL: return TimeLocal();
      case ETIME_SERVER: return TimeCurrent();
      case ETIME_CET: return FromGMT(1);
      case ETIME_MOSCOW: return FromGMT(3);
      case ETIME_EST: return FromGMT(-5);
      case ETIME_GMT: return TimeGMT();
   }
   return 0;
}

string GetTimeBase(ETimeKind time)
{
   switch (time)
   {
      case ETIME_LOCAL: return "Local";
      case ETIME_SERVER: return "Server";
      case ETIME_CET: return "CET";
      case ETIME_MOSCOW: return "Moscow";
      case ETIME_EST: return "EST";
      case ETIME_GMT: return "GMT";
   }
   return "N/A";
}

int TimeDay(datetime time)
{
   MqlDateTime info;
   TimeToStruct(time, info);
   
   return info.day_of_week;
}

string DayToString(int day)
{
   switch (day)
   {
      case 0: return "Sun";
      case 1: return "Mon";
      case 2: return "Tue";
      case 3: return "Wed";
      case 4: return "Thu";
      case 5: return "Fri";
      case 6: return "Sat";
   }
   return "N/A";
}

string GetNearestNews()
{
   int objects = ObjectsTotal(0, 0, OBJ_EVENT);

   datetime now = TimeCurrent();
   
   string name;
   string nearest;
   datetime nearestTime = 0;
   
   for (int i = 0; i < objects; i++)
   {
      name = ObjectName(0, i, 0, OBJ_EVENT);
      
      datetime current = datetime(ObjectGetInteger(0, name, OBJPROP_TIME, 0));
      if (current > now)
      {
         if (current < nearestTime || nearestTime == 0)
         {
            nearestTime = current;
            nearest = name;
         }
      }
   }
   
   return nearest;
}

void CreateObject(int chartID, int subWnd, string name, ENUM_OBJECT type, string text, bool selectable = false)
{
   int wnd = ObjectFind(chartID, name);
   
   if (wnd >= 0 && wnd != subWnd)
   {
      ObjectDelete(chartID, name);
      ObjectCreate(chartID, name, type, subWnd, 0, 0);
   }
   else if (wnd < 0)
   {
      ObjectCreate(chartID, name, type, subWnd, 0, 0);
   }
   
   ObjectSetInteger(chartID, name, OBJPROP_SELECTABLE, selectable);
   ObjectSetString(chartID, name, OBJPROP_TEXT, text);
}

void MoveObject(int chartID, string name, int x, int y, int corner)
{
   ObjectSetInteger(chartID, name, OBJPROP_XDISTANCE, x);
   ObjectSetInteger(chartID, name, OBJPROP_YDISTANCE, y);
   ObjectSetInteger(chartID, name, OBJPROP_CORNER, corner);
}

double CorrectLot(double lot, string symbol = "")
{
   if (symbol == "") symbol = _Symbol;

   double min = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
   double max = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
   double step= SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);
   
   double lots = MathRound(lot/step)*step;
   if (lots > max) lots = max;
   if (lots < min) lots = 0;
   
   return lots;
}


#define EVENT_SETTING_CHANGED 4456

class ISettingEvents
{
   public:
      virtual void OnBtnUp(string& value)
      {}
      
      virtual void OnBtnDn(string& value)
      {}
      
      virtual void OnApply(string& value)
      {}

      virtual void Draw(int x, int y, int chartID = 0, int subWnd = 0, int corner = 0)
      {}
      
      virtual void Hide()
      {}
};

enum CallType
{
   CALL_ON_UP, // call OnBtnUp
   CALL_ON_DN, // call OnBtnDn
   CALL_ON_APPLY // call OnApply
};

class DrawSetting
   : public ISettingEvents
{
   private:
      
      void ProcessCall(CallType type);
   
   public:
      DrawSetting();
      ~DrawSetting();
      
      virtual string Value() const
      {
         Assert(false, "Pure virtual function call at line "  + string(__LINE__) + " function "  + __FUNCTION__ + " file " + __FILE__);
         return "";
      }
      
      void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
      void SetColors(color clr, color bgClr);
      void Draw(int x, int y, int chartID = 0, int subWnd = 0, int corner = 0);
      void Hide();
   
   protected:
      void Init(string settingName, int settingID, int UID, const string& value, const string& max, const string& min);

      
      string GetEditText() const;
      
      virtual void DrawMinMax();
      virtual void DrawValue();
      virtual void DrawNextPrevious();
      virtual void DrawApply();

   private:
      void CreateObjects(int chartID, int subWnd);
      
   protected:
      string m_Name;

      int m_Chart;
      int m_SubWnd;

      int m_SettingID;

      string m_ValueEdit;
      string m_Title;
      string m_MaxCaption;
      string m_MaxValue;
      string m_MinCaption;
      string m_MinValue;
      string m_ApplyBtn;
      string m_UpBtn;
      string m_DnBtn;
      
      color m_BordersColor;
      color m_BGColor;

   private:
      int m_UID;
      string m_ObjID;
      
      string m_Value;
      string m_Max;
      string m_Min;
      
      int m_X;
      int m_Y;
      int m_Corner;
      
      bool m_Inited;
      bool m_Hidden;
};

DrawSetting::DrawSetting(void)
{
   m_Inited = false;
   m_Hidden = false;
}

DrawSetting::~DrawSetting(void)
{
   Hide();
}

DrawSetting::Init(string settingName, int settingID, int UID, const string& value, const string& max, const string& min)
{
   m_UID = UID;

   m_Name = settingName;
   m_SettingID = settingID;
   m_Value = value;
   m_Max = max;
   m_Min = min;

   m_ObjID = "Setting" + string(m_UID);

   m_Title      = m_ObjID + " Title";
   m_MaxCaption = m_ObjID + " MaxCaption";
   m_MaxValue   = m_ObjID + " MaxValue";
   m_MinCaption = m_ObjID + " MinCaption";
   m_MinValue   = m_ObjID + " MinValue";
   m_ValueEdit  = m_ObjID + " ValueEdit";
   m_ApplyBtn   = m_ObjID + " ApplyBtn";
   m_UpBtn      = m_ObjID + " UpBtn";
   m_DnBtn      = m_ObjID + " DnBtn";
   
   m_Inited = true;
}

void DrawSetting::Draw(int x, int y, int chartID, int subWnd, int corner)
{
   m_Hidden = false;
   m_X = x;
   m_Y = y;
   m_Chart = chartID;
   m_SubWnd = subWnd;
   m_Corner = corner;
   
   CreateObjects(chartID, subWnd);
   
   MoveObject(chartID, m_Title,     x,       y + 20,  corner);
   MoveObject(chartID, m_MaxCaption,x,       y,       corner);
   MoveObject(chartID, m_MaxValue,  x + 120, y,       corner);
   MoveObject(chartID, m_MinCaption,x,       y + 40,  corner);
   MoveObject(chartID, m_MinValue,  x + 120, y + 40,  corner);
   MoveObject(chartID, m_ValueEdit, x + 120, y + 20,  corner);
   MoveObject(chartID, m_ApplyBtn,  x + 247, y + 22,  corner);
   MoveObject(chartID, m_UpBtn,     x + 247, y + 2,   corner);
   MoveObject(chartID, m_DnBtn,     x + 247, y + 42,  corner);
}

void DrawSetting::CreateObjects(int chartID, int subWnd)
{
   m_Chart = chartID;
   m_SubWnd = subWnd;
   
   DrawValue();
   DrawApply();
   DrawMinMax();
   DrawNextPrevious();
}

void DrawSetting::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   switch (id)
   {
      case CHARTEVENT_OBJECT_DELETE:
         {
            if (m_Hidden) return; // no need to restore object in hidden mode
            if (sparam == m_Title)
            {
               DrawValue();
               MoveObject(m_Chart, m_Title,     m_X,       m_Y + 20,  m_Corner);
            }
            else if (sparam == m_MaxCaption)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MaxCaption,m_X,       m_Y,       m_Corner);
            }
            else if (sparam == m_MaxValue)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MaxValue,  m_X + 120, m_Y,       m_Corner);
            }
            else if (sparam == m_MinCaption)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MinCaption,m_X,       m_Y + 40,  m_Corner);
            }
            else if (sparam == m_MinValue)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MinValue,  m_X + 120, m_Y + 40,  m_Corner);
            }
            else if (sparam == m_ValueEdit)
            {
               DrawValue();
               MoveObject(m_Chart, m_ValueEdit, m_X + 120, m_Y + 20,  m_Corner);
            }
            else if (sparam == m_ApplyBtn)
            {
               DrawApply();
               MoveObject(m_Chart, m_ApplyBtn,  m_X + 247, m_Y + 22,  m_Corner);
            }
            else if (sparam == m_UpBtn)
            {
               DrawNextPrevious();
               MoveObject(m_Chart, m_UpBtn,     m_X + 247, m_Y + 2,   m_Corner);
            }
            else if (sparam == m_DnBtn)
            {
               DrawNextPrevious();
               MoveObject(m_Chart, m_DnBtn,     m_X + 247, m_Y + 42,  m_Corner);
            }
         }
      break;
         
      case CHARTEVENT_KEYDOWN:
         {
            if (lparam == VK_UP) ProcessCall(CALL_ON_UP);
            else if (lparam == VK_NUMPAD8) ProcessCall(CALL_ON_UP);
            else if (lparam == VK_DOWN) ProcessCall(CALL_ON_DN);
            else if (lparam == VK_NUMPAD2) ProcessCall(CALL_ON_DN);
         }
      break;
      
      case CHARTEVENT_OBJECT_CLICK:
         {
            if (sparam == m_UpBtn)
            {
               ProcessCall(CALL_ON_UP);
               ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_STATE, false);
            }
            else if (sparam == m_DnBtn)
            {
               ProcessCall(CALL_ON_DN);
               ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_STATE, false);
            }
            else if (sparam == m_ApplyBtn)
            {
               ProcessCall(CALL_ON_APPLY);
               ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_STATE, false);
            }
         }
      break;

      case CHARTEVENT_OBJECT_ENDEDIT:
         {
            if (sparam == m_ValueEdit)
            {
               ProcessCall(CALL_ON_APPLY);
               ChartRedraw(m_Chart);
            }
         }
      break;
   }
}

void DrawSetting::Hide()
{
   m_Hidden = true;

   ObjectDelete(m_Chart, m_Title);
   ObjectDelete(m_Chart, m_MaxCaption);
   ObjectDelete(m_Chart, m_MaxValue);
   ObjectDelete(m_Chart, m_MinCaption);
   ObjectDelete(m_Chart, m_MinValue);
   ObjectDelete(m_Chart, m_ValueEdit);
   ObjectDelete(m_Chart, m_ApplyBtn);
   ObjectDelete(m_Chart, m_DnBtn);
   ObjectDelete(m_Chart, m_UpBtn);
}

string DrawSetting::GetEditText() const
{
   if (ObjectFind(m_Chart, m_ValueEdit) >= 0)
   {
      return ObjectGetString(m_Chart, m_ValueEdit, OBJPROP_TEXT);
   }
   return "";
}

void DrawSetting::ProcessCall(CallType type)
{
   string newValue;
   switch (type)
   {
      case CALL_ON_UP: OnBtnUp(newValue); break;
      case CALL_ON_DN: OnBtnDn(newValue); break;
      case CALL_ON_APPLY: OnApply(newValue); break;
   }
   
   m_Value = newValue;
   ObjectSetString(m_Chart, m_ValueEdit, OBJPROP_TEXT, m_Value);
}

void DrawSetting::DrawMinMax()
{
   CreateObject(m_Chart, m_SubWnd, m_MaxCaption, OBJ_LABEL, "Max Value");
   ObjectSetInteger(m_Chart, m_MaxCaption, OBJPROP_COLOR, m_BordersColor);
   
   CreateObject(m_Chart, m_SubWnd, m_MaxValue, OBJ_LABEL, m_Max);
   ObjectSetInteger(m_Chart, m_MaxValue, OBJPROP_COLOR, m_BordersColor);
   
   CreateObject(m_Chart, m_SubWnd, m_MinCaption, OBJ_LABEL, "Min Value");
   ObjectSetInteger(m_Chart, m_MinCaption, OBJPROP_COLOR, m_BordersColor);
   
   CreateObject(m_Chart, m_SubWnd, m_MinValue, OBJ_LABEL, m_Min);
   ObjectSetInteger(m_Chart, m_MinValue, OBJPROP_COLOR, m_BordersColor);
}

void DrawSetting::DrawValue()
{
   CreateObject(m_Chart, m_SubWnd, m_Title, OBJ_LABEL, m_Name);
   CreateObject(m_Chart, m_SubWnd, m_ValueEdit, OBJ_EDIT, m_Value);
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_XSIZE, 125);
   ObjectSetInteger(m_Chart, m_Title, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_COLOR, m_BGColor);
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_BGCOLOR, m_BordersColor);
}

void DrawSetting::DrawNextPrevious()
{
   CreateObject(m_Chart, m_SubWnd, m_UpBtn, OBJ_BUTTON, CharToString(135));
   ObjectSetString(m_Chart, m_UpBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_XSIZE, 20);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_YSIZE, 15);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_STATE, false);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_COLOR, Black);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_BGCOLOR, PaleTurquoise);
   
   CreateObject(m_Chart, m_SubWnd, m_DnBtn, OBJ_BUTTON, CharToString(136));
   ObjectSetString(m_Chart, m_DnBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_XSIZE, 20);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_YSIZE, 15);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_STATE, false);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_COLOR, Black);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_BGCOLOR, PaleTurquoise);
}

void DrawSetting::DrawApply()
{
   CreateObject(m_Chart, m_SubWnd, m_ApplyBtn, OBJ_BUTTON, CharToString(195));
   ObjectSetString(m_Chart, m_ApplyBtn, OBJPROP_FONT, "Wingdings");
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_XSIZE, 20);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_YSIZE, 15);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_STATE, false);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_COLOR, Black);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_BGCOLOR, PaleTurquoise);
}

void DrawSetting::SetColors(color clr, color bgClr)
{
   m_BordersColor = clr;
   m_BGColor = bgClr;
}

class BoolTable
{
public:
   string GetNameByValue(bool value) const;
   bool GetValueByName(string name) const;
};

string BoolTable::GetNameByValue(bool value) const
{
   if (value == false) return "False";
   return "True";
}

bool BoolTable::GetValueByName(string name) const
{
   string tmp = name;
   StringToLower(tmp);
   
   if (tmp == "false") return false;
   return true;
}

class IntSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, int value, int step = 1, int min = INT_MIN, int max = INT_MAX);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);
      void OnApply(string& value);

   private:
      int      m_IntValue;
      int      m_IntStep;
      int      m_IntMax;
      int      m_IntMin;
};

void IntSetting::Init(string settingName, int settingID, int UID, int value, int step, int min, int max)
{
   m_IntValue = value;
   m_IntMin = min;
   m_IntMax = max;
   m_IntStep = step;
   
   if (m_IntStep <= 0) m_IntStep = 1;
   
   string strValue = string(value);
   string strMax = string(max);
   string strMin = string(min);
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string IntSetting::Value() const
{
   return string(m_IntValue);
}

void IntSetting::OnBtnUp(string& value)
{
   m_IntValue += m_IntStep;
   if (m_IntValue > m_IntMax)
   {
      m_IntValue = m_IntMax;
   }

   value = string(m_IntValue);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void IntSetting::OnBtnDn(string& value)
{
   m_IntValue -= m_IntStep;
   if (m_IntValue < m_IntMin)
   {
      m_IntValue = m_IntMin;
   }

   value = string(m_IntValue);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void IntSetting::OnApply(string& value)
{
   string text = GetEditText();
   
   if (text != "")
   {
      long candidate = StringToInteger(text);
      string revert = string(candidate);
      
      if (revert == text)
      {
         m_IntValue = int(candidate);
         value = string(m_IntValue);
         EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
      }
   }
   value = string(m_IntValue);
}

class SelectionSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, int valueID, const SelectionTable& ids);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);

   private:
      virtual void DrawMinMax();
      virtual void DrawValue();
      virtual void DrawApply();

   private:
      int      m_ValueID;
      SelectionTable m_Table;
};

void SelectionSetting::Init(string settingName, int settingID, int UID, int valueID, const SelectionTable& ids)
{
   m_ValueID = valueID;
   m_Table.CopyFrom(ids);
   
   string strValue = Value();
   string strMax = "";
   string strMin = "";
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string SelectionSetting::Value() const
{
   return m_Table.GetNameByID(m_ValueID);
}

void SelectionSetting::OnBtnUp(string& value)
{
   m_ValueID = m_Table.Next(m_ValueID);

   value = m_Table.GetNameByID(m_ValueID);
   
   string strChanged = string(m_ValueID);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, strChanged);
}

void SelectionSetting::OnBtnDn(string& value)
{
   m_ValueID = m_Table.Previous(m_ValueID);

   value = m_Table.GetNameByID(m_ValueID);
   
   string strChanged = string(m_ValueID);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, strChanged);
}

void SelectionSetting::DrawMinMax()
{
}

void SelectionSetting::DrawValue()
{
   DrawSetting::DrawValue();
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_READONLY, true);
}

void SelectionSetting::DrawApply()
{
}

class BoolSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, bool value);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);

   private:
      virtual void DrawMinMax();
      virtual void DrawValue();
      virtual void DrawApply();

   private:
      int m_BoolValue;
      BoolTable m_Table;
};

void BoolSetting::Init(string settingName,int settingID,int UID,bool value)
{
   m_BoolValue = value;
   
   string strValue = Value();
   string strMax = "";
   string strMin = "";
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string BoolSetting::Value() const
{
   return m_Table.GetNameByValue(m_BoolValue);
}

void BoolSetting::OnBtnUp(string& value)
{
   m_BoolValue = !m_BoolValue;

   value = Value();
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void BoolSetting::OnBtnDn(string& value)
{
   m_BoolValue = !m_BoolValue;

   value = Value();
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void BoolSetting::DrawMinMax()
{
}

void BoolSetting::DrawValue()
{
   DrawSetting::DrawValue();
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_READONLY, true);
}

void BoolSetting::DrawApply()
{
}

class StringSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, string value);
   
      string Value() const;
      
      void OnApply(string& value);
      
   private:
      void DrawMinMax();
      void DrawNextPrevious();

   private:
      string m_StringValue;
};

void StringSetting::Init(string settingName, int settingID, int UID, string value)
{
   m_StringValue = value;
   
   string min = "";
   string max = "";
   
   DrawSetting::Init(settingName, settingID, UID, m_StringValue, min, max);
}

string StringSetting::Value() const
{
   return string(m_StringValue);
}

void StringSetting::OnApply(string& value)
{
   m_StringValue = GetEditText();
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, m_StringValue);
   value = string(m_StringValue);
}

void StringSetting::DrawMinMax()
{
}

void StringSetting::DrawNextPrevious()
{
}

class DoubleSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, double value, double step = 1, double min = -DBL_MAX, double max = DBL_MAX);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);
      void OnApply(string& value);

   private:
      double   m_DoubleValue;
      double   m_DoubleStep;
      double   m_DoubleMax;
      double   m_DoubleMin;
};

void DoubleSetting::Init(string settingName, int settingID, int UID, double value, double step, double min, double max)
{
   m_DoubleValue = value;
   m_DoubleMin = min;
   m_DoubleMax = max;
   m_DoubleStep = step;
   
   if (m_DoubleStep <= 0) m_DoubleStep = 1;
   
   string strValue = string(value);
   string strMax = string(max);
   string strMin = string(min);
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string DoubleSetting::Value() const
{
   return string(m_DoubleValue);
}

void DoubleSetting::OnBtnUp(string& value)
{
   m_DoubleValue += m_DoubleStep;
   if (m_DoubleValue > m_DoubleMax)
   {
      m_DoubleValue = m_DoubleMax;
   }

   value = string(m_DoubleValue); 
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void DoubleSetting::OnBtnDn(string& value)
{
   m_DoubleValue -= m_DoubleStep;
   if (m_DoubleValue < m_DoubleMin)
   {
      m_DoubleValue = m_DoubleMin;
   }

   value = string(m_DoubleValue);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void DoubleSetting::OnApply(string& value)
{
   string text = GetEditText();
   
   if (text != "")
   {
      double candidate = StringToDouble(text);
      
      if (candidate != 0 || StringFind(text, "0") == 0)
      {
         m_DoubleValue = candidate;
         
         value = string(m_DoubleValue);
         EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
      }
   }
   value = string(m_DoubleValue);
}

class CommentsStore
{
public:
   CommentsStore();

   void SetLines(int newLines);
   int Lines() const;
   
   void SetLength(int newLength);
   int Length() const;

   void SetTabSize(int newSize);
   int TabSize() const;
   
   void AddComment(string newComment);
   string CommentsToStr() const;
   
   void Clear();
   
   bool CommentsStore::GetLine(int i, string& prefix, string& line);

private:
   string ReplaceTabs(string s);
   void AddLine(string line, bool isStart);
   void AddSizedLine(string line, bool isStart);

private:
   string m_Comments[];
   string m_Times[];
   int m_Lines;
   int m_Length;
   int m_TabSize;
   int m_Pos;
};

CommentsStore::CommentsStore(void)
{
   m_Lines = 10;
   m_Length = 60;
   m_TabSize = 8;
   m_Pos = 0;

   ArrayResize(m_Comments, m_Lines);
   ArrayResize(m_Times, m_Lines);
}

void CommentsStore::SetLines(int newLines)
{
   Clear();
   ArrayResize(m_Times, newLines);
   ArrayResize(m_Comments, newLines);
   m_Lines = newLines;
}

int CommentsStore::Lines() const
{
   return m_Lines;
}

void CommentsStore::SetLength(int newLength)
{
   Clear();
   m_Length = newLength;
}

int CommentsStore::Length() const
{
   return m_Length;
}

void CommentsStore::SetTabSize(int newSize)
{
   m_TabSize = newSize;
}

int CommentsStore::TabSize() const
{
   return m_TabSize;
}

void CommentsStore::AddComment(string newComment)
{
   string s = newComment;
   int pos = StringFind(s, "\n");
   bool isStart = true;
   
   while(pos > 0)
   {
      AddLine(StringSubstr(s, 0, pos), isStart);
      s = StringSubstr(s, pos + 1);
      
      pos = StringFind(s, "\n");
      isStart = false;
   }

   AddLine(s, isStart);
}

string CommentsStore::ReplaceTabs(string s)
{
   static string spaces = "                                                     "; // for tabs filling
   
   string result = s;
   
   int pos = StringFind(result, "\t");
   while(pos >= 0)
   {
      int size = (pos - 1)/m_TabSize*m_TabSize + m_TabSize - pos;
      
      if (size > 0)
      {
         result = 
            StringSubstr(result, 0, pos) +
            StringSubstr(spaces, 0, size) +
            StringSubstr(result, pos + 1);
      }
      else
      {
         result = 
            StringSubstr(result, 0, pos) +
            StringSubstr(result, pos + 1);
      }
         
      pos = StringFind(result, "\t");
   }
   return (result);
}

void CommentsStore::AddLine(string line, bool isStart)
{
   string s = ReplaceTabs(line);
   
   int size = StringLen(s);
   bool start = isStart;
   
   if (size == 0)
   {
      AddSizedLine(s, start);
      return;
   }
   
   while (size > 0)
   {
      AddSizedLine(StringSubstr(s, 0, m_Length), start);
      s = StringSubstr(s, m_Length);
      
      size -= m_Length;
      start = false;
   }
}

void CommentsStore::AddSizedLine(string line, bool isStart)
{
   string prefix = TimeToString(TimeTradeServer(), TIME_SECONDS);
   if (!isStart)
   {
      prefix = StringSubstr("                       ", 0, StringLen(prefix));
   }
   
   prefix = prefix + " | ";
   
   if (m_Pos < m_Lines)
   {
      m_Comments[m_Pos] = line;
      m_Times[m_Pos] = prefix;
      m_Pos++;
   }
   else
   {
      for(int i = 1; i < m_Lines; i++)
      {
         m_Comments[i - 1] = m_Comments[i];
         m_Times[i - 1] = m_Times[i];
      }
      m_Comments[m_Pos - 1] = line;
      m_Times[m_Pos - 1] = prefix;
   }
}

string CommentsStore::CommentsToStr() const
{
   string res;
   for (int i = 0; i < m_Pos; i++)
   {
      res = res + m_Times[i] + m_Comments[i] + "\n";
   }
   return res;
}

void CommentsStore::Clear()
{
   for (int i = 0; i < m_Lines; i++)
   {
      m_Comments[i] = "";
      m_Times[i] = "";
   }
   m_Pos = 0;
}

bool CommentsStore::GetLine(int i, string& prefix, string& line)
{
   if (i >= 0 && i < m_Lines)
   {
      prefix = m_Times[i];
      line = m_Comments[i];
      return true;
   }
   return false;
}

CommentsStore Store;

void Comment_(string s)
{
   Print("Comment: " + s);
   Store.AddComment(s);
}

bool SetLength(int length)
{
   if (length <= 0 || length >= 63) return false;

   Store.SetLength(length);
   Comment_("Log lines length changed to " + string(length));

   return true;
}

bool SetLines(int lines)
{
   if (lines <= 0 || lines >= 20) return false;

   Store.SetLines(lines);
   Comment_("Log lines count changed to " + string(lines));
   
   return true;
}

bool SetTabSize(int tabSize)
{
   if (tabSize <= 0 || tabSize >= 20) return false;
   
   Store.SetTabSize(tabSize);
   return true;
}

int GetLength()
{
   return Store.Length();
}

int GetLines()
{
   return Store.Lines();
}

int GetTabSize()
{
   return Store.TabSize();
}

bool GetLine(int pos, string& prefix, string& line)
{
   return Store.GetLine(pos, prefix, line);
}

#include <Trade/Trade.mqh>
#include <Trade/AccountInfo.mqh>

class MyTrade
{
public:
   MyTrade();
   void SetSlippage(int slipPts);
   void SetFilling(ENUM_ORDER_TYPE_FILLING filling);
   void SetComment(string newComment);
   
   bool SetStop(double stop);
   bool SetTP(double stop);
   bool SetSL(double stop);

   long Buy(double lots);
   long BuyTP(double lots, double TP);
   long BuySL(double lots, double SL);
   
   long Sell(double lots);
   long SellTP(double lots, double TP);
   long SellSL(double lots, double SL);
   
   long OpenBuyLimit(double lots, double price);
   long OpenBuyStop(double lots, double price);

   long OpenSellLimit(double lots, double price);
   long OpenSellStop(double lots, double price);

   bool Close();
   bool Reverse();

   bool GetCurrentPos(ENUM_POSITION_TYPE& type, double& lot);
   bool GetCurrentSLTP(double& SL, double& TP);
   
   bool CanTrade();

private: 
   CTrade m_Trade;
   CPositionInfo m_Position;
   CAccountInfo m_Account;
   string m_Comment;
   int m_LotDigits;
   int m_Slippage;
};

MyTrade::MyTrade()
{
   m_Comment = "Trade Xpert";
   m_LotDigits = DoubleDigits(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP));
   SetSlippage(10);
}

bool MyTrade::CanTrade()
{
   if (!m_Account.TradeAllowed()) 
   {
      Comment_("Trade is not allowed");
      return false;
   }
   
   if (!m_Account.TradeExpert())
   {
      Comment_("Tradind by experts is disabled. Enable this option and try again");
      return false;
   }
   
   return true;
}


bool MyTrade::GetCurrentPos(ENUM_POSITION_TYPE& type, double& lot)
{
   if (!m_Position.Select(_Symbol))
   {
      return false;
   }
   
   type = ENUM_POSITION_TYPE(m_Position.Type());
   lot = m_Position.Volume();
   
   return true;
}

bool MyTrade::GetCurrentSLTP(double& SL, double& TP)
{
   if (!m_Position.Select(_Symbol))
   {
      return false;
   }
   
   SL = m_Position.StopLoss();
   TP = m_Position.TakeProfit();

   return true;
}



bool MyTrade::SetStop(double stop)
{
   if (!CanTrade()) return false;
   
   ENUM_POSITION_TYPE type;
   double lot;
   if (!GetCurrentPos(type, lot))
   {
      Comment_("Can not set stops without position available");
      return false;
   }
   
   if (type == POSITION_TYPE_BUY)
   {
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      if (stop > bid)
      {
         return SetTP(stop);
      }

      return SetSL(stop);
   }
   else if (type == POSITION_TYPE_SELL)
   {
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      if (stop > bid)
      {
         return SetSL(stop);
      }

      return SetTP(stop);
   }
   return false;
}

bool MyTrade::SetTP(double stop)
{
   if (!CanTrade()) return false;
   
   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);

   if (m_Trade.PositionModify(_Symbol, sl, stop))
   {
      string str = "TP set to ";
      str = str + DoubleToString(stop) + " successfully";
      Comment_(str);
      
      return true;
   }
   
   string str = "Set TP to ";
   str = str + DoubleToString(stop) + " failed, error #";
   str = str + string(m_Trade.ResultRetcode()) + " (";
   str = str + m_Trade.ResultRetcodeDescription() + ")";
   Comment_(str);
   
   return false;
}

bool MyTrade::SetSL(double stop)
{
   if (!CanTrade()) return false;

   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);

   if (m_Trade.PositionModify(_Symbol, stop, tp))
   {
      string str = "SL set to ";
      str = str + DoubleToString(stop) + " successfully";
      Comment_(str);
      
      return true;
   }
   
   string str = "Set SL to ";
   str = str + DoubleToString(stop) + " failed, error #";
   str = str + string(m_Trade.ResultRetcode()) + " (";
   str = str + m_Trade.ResultRetcodeDescription() + ")";
   Comment_(str);
   
   return false;
}

void MyTrade::SetSlippage(int slipPts)
{
   m_Trade.SetDeviationInPoints(slipPts);
   m_Slippage = slipPts;
}

void MyTrade::SetFilling(ENUM_ORDER_TYPE_FILLING filling)
{
   m_Trade.SetTypeFilling(filling);
}

void MyTrade::SetComment(string newComment)
{
   m_Comment = newComment;
}

long MyTrade::Buy(double lots)
{
   if (!CanTrade()) return -1;
   
   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);
   
   ENUM_POSITION_TYPE type;
   double lot;
   
   if (GetCurrentPos(type, lot))
   {
      if (type == POSITION_TYPE_SELL)
      {
         double tmp = sl;
         sl = tp;
         tp = tmp;
      }
   }

   if(m_Trade.Buy(lots, _Symbol, 0, sl, tp, m_Comment))
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
      str = str + " Buy at ";
      str = str + DoubleToString(m_Trade.ResultPrice(), _Digits) + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy failed, error code is #";
      str = str + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);

      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::BuyTP(double lots, double TP)
{
   if (!CanTrade()) return -1;

   double normTP = NormalizeDouble(TP, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (TP < ask)
   {
      Comment_("Inconsistent TP value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(TP)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);

      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_SELL)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Buy(lots, _Symbol, 0, sl, normTP, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Buy at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with TP " + DoubleToString(normTP, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Buy with TP failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::BuySL(double lots, double SL)
{
   if (!CanTrade()) return -1;

   double normSL = NormalizeDouble(SL, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (SL > ask)
   {
      Comment_("Inconsistent SL value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(SL)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);

      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_SELL)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Buy(lots, _Symbol, 0, normSL, tp, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Buy at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with SL " + DoubleToString(normSL, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Buy with SL failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::Sell(double lots)
{
   if (!CanTrade()) return -1;

   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);

   ENUM_POSITION_TYPE type;
   double lot;
   
   if (GetCurrentPos(type, lot))
   {
      if (type == POSITION_TYPE_BUY)
      {
         double tmp = sl;
         sl = tp;
         tp = tmp;
      }
   }

   if(m_Trade.Sell(lots, _Symbol, 0, sl, tp, m_Comment))
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
      str = str + " Sell at ";
      str = str + DoubleToString(m_Trade.ResultPrice(), _Digits) + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell failed, error code is #";
      str = str + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);

      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::SellTP(double lots, double TP)
{
   if (!CanTrade()) return -1;

   double normTP = NormalizeDouble(TP, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (TP > bid)
   {
      Comment_("Inconsistent TP value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(TP)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);
   
      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_BUY)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Sell(lots, _Symbol, 0, sl, normTP, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Sell at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with TP " + DoubleToString(normTP, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Sell with TP failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::SellSL(double lots, double SL)
{
   if (!CanTrade()) return -1;

   double normSL = NormalizeDouble(SL, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (SL < bid)
   {
      Comment_("Inconsistent SL value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(SL)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);
   
      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_BUY)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Sell(lots, _Symbol, 0, normSL, tp, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Sell at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with SL " + DoubleToString(normSL, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Sell with SL failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::OpenBuyLimit(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (normPrice > ask)
   {
      Comment_("Inconsistent price for Buy Limit, stopping command");
      return (-1);
   }
   
   if (m_Trade.BuyLimit(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::OpenBuyStop(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (normPrice < ask)
   {
      Comment_("Inconsistent price for Buy Stop, stopping command");
      return (-1);
   }
   
   if (m_Trade.BuyStop(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::OpenSellLimit(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (normPrice < bid)
   {
      Comment_("Inconsistent price for Sell Limit, stopping command");
      return (-1);
   }
   
   if (m_Trade.SellLimit(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::OpenSellStop(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (normPrice > bid)
   {
      Comment_("Inconsistent price for Sell Stop, stopping command");
      return (-1);
   }
   
   if (m_Trade.SellStop(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

bool MyTrade::Close()
{
   if (!CanTrade()) return -1;
   
   ENUM_POSITION_TYPE type;
   double lot;
      
   if (!GetCurrentPos(type, lot))
   {
      Comment_("No position to close");
      return false;
   }

   if (m_Trade.PositionClose(_Symbol, m_Slippage))
   {
      Comment_("Position closed successfully");
      return true;
   }
   else
   {
      string str = "Close Position failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return false;
   }
}

bool MyTrade::Reverse()
{
   if (!CanTrade()) return -1;
   
   ENUM_POSITION_TYPE type;
   double lot;
   
   if (!GetCurrentPos(type, lot))
   {
      Comment_("Can not reverse zero position");
      return false;
   }
   
   if (type == POSITION_TYPE_BUY)
   {
      if (m_Trade.Sell(2*lot, _Symbol, 0, 0, 0, m_Comment))
      {
         m_Trade.PositionModify(_Symbol, 0, 0);
         Comment_(DoubleToString(lot, m_LotDigits) + " Buy reversed successfully");
         
         return true;
      }
      else
      {
         string str = "Reverse Position failed, error#" + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);

         return false;
      }
   }
   else if (type == POSITION_TYPE_SELL)
   {
      if (m_Trade.Buy(2*lot, _Symbol, 0, 0, 0, m_Comment))
      {
         m_Trade.PositionModify(_Symbol, 0, 0);
         Comment_(DoubleToString(lot, m_LotDigits) + " Sell reversed successfully");
         
         return true;
      }
      else
      {
         Comment_("Reverse position failed");
         return false;
      }
   }
   return false;
}

#define SETTING_USE_SLIDING_ID     1
#define SETTING_COLOR_SCHEME_ID    2
#define SETTING_COMMENT_ID         3

#define SETTING_LOG_FONT_SIZE      4
#define SETTING_LOG_FONT_COLOR     5
#define SETTING_LOG_FONT_LINES     6
#define SETTING_LOG_FONT_LENGTH    7
#define SETTING_LOG_FONT_TABSIZE   8

#define SETTING_TIME               9
#define SETTING_SPREAD_BARS        10

#define SETTING_TRADE_SLIPPAGE     11
#define SETTING_TRADE_FILLING      12

class PaneSettings
{
public:
   PaneSettings();
   ~PaneSettings();
   
   void AddIntSetting(string settingName, int settingID, int value, int step = 1, int min = INT_MIN, int max = INT_MAX);
   void AddDoubleSetting(string settingName, int settingID, double value, double step = 1, double min = -DBL_MAX, double max = DBL_MAX);
   void AddStringSetting(string settingName, int settingID, string value);
   void AddSelectionSetting(string settingName, int settingID, int valueID, const SelectionTable& ids);
   void AddBoolSetting(string settingName, int settingID, bool value);
   
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   
   void Show(bool needShow);
   void SetColors(color clr, color bgClr);
   void UseSliding(bool use);
   
   void DrawBar();

private:
   void Hide();
   void DrawNextButton();
   void DrawPrevButton();
   void DrawBarArea();
   void DrawCurrentSetting();
   void HideCurrentSetting();
   void Slide(bool needShow);

private:
   DrawSetting* m_Settings[];
   
   string m_NextButton;
   string m_PrevButton;
   
   string m_Border;
   string m_ShowBtn;
   string m_HideBtn;
   
   string m_Area;

   int m_UID;
   
   int m_CurrentIndex;
   
   int m_Hidden;
   
   bool m_UseSliding;
   
   color m_BordersColor;
   color m_BGColor;
   
   int m_BaseX;
   int m_BaseY;
};

PaneSettings::PaneSettings(void)
{
   m_UID = GetUID();
   m_NextButton = "NextSetting" + string(m_UID);
   m_PrevButton = "PrevSetting" + string(m_UID);
   
   m_Border = "Border" + string(m_UID);
   m_ShowBtn = "ShowBtn" + string(m_UID);
   m_HideBtn = "HideBtn" + string(m_UID);
   
   m_Area = "Area" + string(m_UID);
   
   m_CurrentIndex = 0;

   m_UseSliding = true;
   m_Hidden = true;
   
   m_BordersColor = LightGray;
   m_BGColor = Black;
   
   m_BaseX = 0;
   m_BaseY = 0;
}

void PaneSettings::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   if (id == CHARTEVENT_OBJECT_DELETE)
   {
      if (sparam == m_NextButton && !m_Hidden)
      {
         DrawNextButton();
      }
      else if (sparam == m_PrevButton && !m_Hidden)
      {
         DrawPrevButton();
      }
      else if (
                  (sparam == m_Border) ||
                  (sparam == m_ShowBtn && m_Hidden) ||
                  (sparam == m_HideBtn && !m_Hidden)
              )
      {
         DrawBarArea();
      }
   }

   if (id == CHARTEVENT_OBJECT_CLICK)
   {
      if (sparam == m_NextButton)
      {
         HideCurrentSetting();
         
         m_CurrentIndex++;
         if (m_CurrentIndex >= ArraySize(m_Settings))
         {
            m_CurrentIndex = 0;
         }
         DrawBar();
      }
      else if (sparam == m_PrevButton)
      {
         HideCurrentSetting();

         m_CurrentIndex--;
         if (m_CurrentIndex < 0)
         {
            m_CurrentIndex = int(MathMax(0, ArraySize(m_Settings) - 1));
         }
         DrawBar();
      }
      else if (sparam == m_ShowBtn)
      {
         Show(true);
      }
      else if (sparam == m_HideBtn)
      {
         Show(false);
      }
   }
   
   int size = ArraySize(m_Settings);
   if (size > 0)
   {
      if (m_Settings[m_CurrentIndex] != NULL)
      {
         m_Settings[m_CurrentIndex].OnChartEvent(id, lparam, dparam, sparam);
      }
   }
}

PaneSettings::DrawBar()
{
   DrawBarArea();

   if (!m_Hidden)
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 79, CORNER_LEFT_UPPER);

      DrawNextButton();
      DrawPrevButton();
      DrawCurrentSetting();      
   }
   else
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 29, CORNER_LEFT_UPPER);

      Hide();   
   }
}

void PaneSettings::Hide()
{
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_NextButton);
   ObjectDelete(0, m_PrevButton);
   ObjectDelete(0, m_Area);

   HideCurrentSetting();
}

PaneSettings::~PaneSettings()
{
   ObjectDelete(0, m_NextButton);
   ObjectDelete(0, m_PrevButton);
   ObjectDelete(0, m_Border);
   ObjectDelete(0, m_ShowBtn);
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_Area);
   
   int size = ArraySize(m_Settings);
   for (int i = 0; i < size; i++)
   {
      if (m_Settings[i] != NULL)
      {
         delete m_Settings[i];
      }
   }
}

void PaneSettings::DrawNextButton()
{
   CreateObject(0, 0, m_NextButton, OBJ_BUTTON, CharToString(135));
   MoveObject(0, m_NextButton, m_BaseX + 5, m_BaseY + 20, CORNER_LEFT_UPPER);
   ObjectSetString(0, m_NextButton, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_NextButton, OBJPROP_XSIZE, 20);
   ObjectSetInteger(0, m_NextButton, OBJPROP_YSIZE, 20);
   ObjectSetInteger(0, m_NextButton, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_NextButton, OBJPROP_COLOR, Black);
   ObjectSetInteger(0, m_NextButton, OBJPROP_BGCOLOR, PaleTurquoise);
   
}

void PaneSettings::DrawPrevButton()
{
   CreateObject(0, 0, m_PrevButton, OBJ_BUTTON, CharToString(136));
   MoveObject(0, m_PrevButton, m_BaseX + 5, m_BaseY + 50, CORNER_LEFT_UPPER);
   ObjectSetString(0, m_PrevButton, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_PrevButton, OBJPROP_XSIZE, 20);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_YSIZE, 20);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_COLOR, Black);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_BGCOLOR, PaleTurquoise);
}

void PaneSettings::DrawBarArea()
{
   CreateObject(0, 0, m_Border, OBJ_EDIT, "");
   ObjectSetInteger(0, m_Border, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_COLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_Border, OBJPROP_XSIZE, 297);
   ObjectSetInteger(0, m_Border, OBJPROP_YSIZE, 5);
   
   if (m_Hidden)
   {
      CreateObject(0, 0, m_ShowBtn, OBJ_BUTTON, CharToString(192));
      MoveObject(0, m_ShowBtn, m_BaseX + 120, m_BaseY + 39, CORNER_LEFT_UPPER);
      ObjectSetString(0, m_ShowBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_COLOR, m_BGColor);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_BGCOLOR, m_BordersColor);
   }
   else
   {
      CreateObject(0, 0, m_HideBtn, OBJ_BUTTON, CharToString(191));
      MoveObject(0, m_HideBtn, m_BaseX + 120, m_BaseY + 89, CORNER_LEFT_UPPER);
      ObjectSetString(0, m_HideBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_HideBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_COLOR, m_BGColor);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_BGCOLOR, m_BordersColor);

      CreateObject(0, 0, m_Area, OBJ_EDIT, "");
      MoveObject(0, m_Area, m_BaseX + 4, m_BaseY + 15, CORNER_LEFT_UPPER);
      ObjectSetInteger(0, m_Area, OBJPROP_XSIZE, 295);
      ObjectSetInteger(0, m_Area, OBJPROP_YSIZE, 59);
      ObjectSetInteger(0, m_Area, OBJPROP_BGCOLOR, m_BordersColor);
      ObjectSetInteger(0, m_Area, OBJPROP_READONLY, true);
   }
}

void PaneSettings::AddIntSetting(string settingName, int settingID, int value, int step = 1, int min = INT_MIN, int max = INT_MAX)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   IntSetting* newSetting = new IntSetting;
   newSetting.Init(settingName, settingID, GetUID(), value, step, min, max);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;
   
   if (size == 0) DrawBar();
}

void PaneSettings::AddDoubleSetting(string settingName, int settingID, double value, double step = 1, double min = -DBL_MAX, double max = DBL_MAX)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   DoubleSetting* newSetting = new DoubleSetting;
   newSetting.Init(settingName, settingID, GetUID(), value, step, min, max);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::AddStringSetting(string settingName, int settingID, string value)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   StringSetting* newSetting = new StringSetting;
   newSetting.Init(settingName, settingID, GetUID(), value);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::AddSelectionSetting(string settingName, int settingID, int valueID, const SelectionTable& ids)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   SelectionSetting* newSetting = new SelectionSetting;
   newSetting.Init(settingName, settingID, GetUID(), valueID, ids);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::AddBoolSetting(string settingName, int settingID, bool value)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   BoolSetting* newSetting = new BoolSetting;
   newSetting.Init(settingName, settingID, GetUID(), value);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::DrawCurrentSetting(void)
{
   int size = ArraySize(m_Settings);
   if (size > 0)
   {
      if (m_Settings[m_CurrentIndex] != NULL)
      {
         m_Settings[m_CurrentIndex].Draw(30, 15, 0, 0);
      }
   }
}

void PaneSettings::HideCurrentSetting(void)
{
   int size = ArraySize(m_Settings);
   if (size > 0)
   {
      if (m_Settings[m_CurrentIndex] != NULL)
      {
         m_Settings[m_CurrentIndex].Hide();
      }
   }
}

void PaneSettings::Show(bool needShow)
{
   if (needShow)
   {
      ObjectDelete(0, m_ShowBtn);
      
      if (m_Hidden) Slide(true);
      m_Hidden = false;
   }
   else
   {
      Hide();
      
      if (!m_Hidden) Slide(false);
      m_Hidden = true;
   }
   DrawBar();
}

void PaneSettings::Slide(bool needShow)
{
   if (!m_UseSliding) return;
   
   #define SETTINGS_SLIDE_SHIFTS_SIZE 14
   static const int SlideShifts[SETTINGS_SLIDE_SHIFTS_SIZE] = {0, 1, 3, 6, 10, 15, 21, 29, 35, 40, 44, 47, 49, 50};

   if (needShow)
   {
      for (int i = 0; i < SETTINGS_SLIDE_SHIFTS_SIZE; ++i)
      {
         MoveObject(0, m_Border, m_BaseX, SlideShifts[i] + 29 + m_BaseY, CORNER_LEFT_UPPER);
         ChartRedraw();
         Sleep(8);
      }
   }
   else
   {
      for (int i = SETTINGS_SLIDE_SHIFTS_SIZE - 1; i >= 0; --i)
      {
         MoveObject(0, m_Border, m_BaseX, SlideShifts[i] + 29 + m_BaseY, CORNER_LEFT_UPPER);
         ChartRedraw();
         Sleep(8);
      }
   }
}

void PaneSettings::SetColors(color clr, color bgClr)
{
   m_BordersColor = clr;
   m_BGColor = bgClr;
   
   int size = ArraySize(m_Settings);
   for (int i = 0; i < size; i++)
   {
      m_Settings[i].SetColors(m_BGColor, m_BordersColor);
   }
}

void PaneSettings::UseSliding(bool use)
{
   m_UseSliding = use;
}


class InfoPane
{
public:
   InfoPane();
   ~InfoPane();
   
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   void OnTick();
   void OnTimer();
   
   void Show(bool needShow);
   void UseSliding(bool use);
   
private:
   void Draw();
   void Hide();
   void DrawBarArea();
   void DrawSpreadBar();
   void DrawDaily();
   void DrawOpen();
   void DrawSwap();
   void DrawTime();
   void DrawLevels();
   void DrawNews();
   
   void Slide(bool needShow);
   
   void OnSettingChanged(int id, string value);

private:
   string m_Border;
   string m_ShowBtn;
   string m_HideBtn;
   string m_Area;
   
   string m_SpreadUpBorder;
   string m_SpreadDnBorder;
   string m_SpreadArea;
   string m_SpreadProgress;
   string m_SpreadMax;
   string m_SpreadMin;
   string m_SpreadActual;
   
   string m_DailyOpen;
   string m_DailyDirection;
   string m_DailyPercent;
   
   string m_OpenLot;
   string m_OpenDirection;
   string m_OpenEquity;
   
   string m_TimeDay;
   string m_TimeClock;
   
   string m_Levels;
   
   string m_NewsName;
   string m_NewsTime;
   
   string m_Swap;
   string m_CopyRight;

   int m_UID;
   
   int m_Hidden;
   
   bool m_UseSliding;
   
   color m_BordersColor;
   color m_TextColor;
   color m_BuyColor;
   color m_SellColor;
   color m_UpColor;
   color m_DnColor;
   
   int m_BaseX;
   int m_BaseY;
   
   int m_SpreadInfoHandle;
   int m_SpreadInfoPeriod;
   
   int m_LotsDigits;
   
   ETimeKind m_TimeSettings;
};

InfoPane::InfoPane(void)
{
   m_UID = GetUID();
   string sUID = string(m_UID);
   
   m_Border = "Border" + sUID;
   m_ShowBtn = "ShowBtn" + sUID;
   m_HideBtn = "HideBtn" + sUID;
   m_Area = "Area" + sUID;
   
   m_SpreadUpBorder = "SpreadUpBorder" + sUID;
   m_SpreadDnBorder = "SpreadDnBorder" + sUID;
   m_SpreadArea = "SpreadArea" + sUID;
   m_SpreadProgress = "SpreadProgress" + sUID;
   m_SpreadMax = "SpreadMax" + sUID;
   m_SpreadMin = "SpreadMin" + sUID;
   m_SpreadActual = "SpreadActual" + sUID;
   
   m_DailyOpen = "DailyOpen" + sUID;
   m_DailyDirection = "DailyDirection" + sUID;
   m_DailyPercent = "DailyPercent" + sUID;
   
   m_OpenLot = "OpenLot" + sUID;
   m_OpenDirection = "OpenDirection" + sUID;
   m_OpenEquity = "OpenEquity" + sUID;
   
   m_TimeDay = "TimeDay" + sUID;
   m_TimeClock = "TimeClock" + sUID;
   m_Levels = "Levels" + sUID;
   
   m_Swap = "Swap" + sUID;
   
   m_NewsName = "NewsName" + sUID;
   m_NewsTime = "NewsTime" + sUID;
   
   m_CopyRight = "CopyRight" + sUID;
   
   m_UseSliding = true;
   m_Hidden = true;
   
   m_BordersColor = Black;
   m_TextColor = LightGray;
   m_BuyColor = DodgerBlue;
   m_SellColor = Tomato;
   m_UpColor = SeaGreen;
   m_DnColor = Tomato;
   
   m_BaseX = 0;
   m_BaseY = 0;
   
   m_SpreadInfoHandle = iCustom(_Symbol, PERIOD_CURRENT, "SpreadInfo");
   m_SpreadInfoPeriod = 5;
   
   m_LotsDigits = DoubleDigits(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP));
   m_TimeSettings = ETIME_SERVER;
}

void InfoPane::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   if (id == CHARTEVENT_OBJECT_DELETE)
   {
      if (!m_Hidden)
      {
         if (
               sparam == m_Border ||
               sparam == m_HideBtn ||
               sparam == m_Area ||

               sparam == m_SpreadUpBorder ||
               sparam == m_SpreadDnBorder ||
               sparam == m_SpreadArea ||
               sparam == m_SpreadProgress ||
               sparam == m_SpreadMax ||
               sparam == m_SpreadMin ||
               sparam == m_SpreadActual ||

               sparam == m_DailyOpen ||
               sparam == m_DailyDirection ||
               sparam == m_DailyPercent ||

               sparam == m_OpenLot ||
               sparam == m_OpenDirection ||
               sparam == m_OpenEquity ||

               sparam == m_TimeDay ||
               sparam == m_TimeClock ||

               sparam == m_Levels ||

               sparam == m_NewsName ||
               sparam == m_NewsTime ||
               sparam == m_Swap
            )
         {
            Draw();
         }       
      }
      
      if (
                  (sparam == m_Border) ||
                  (sparam == m_ShowBtn && m_Hidden) ||
                  (sparam == m_HideBtn && !m_Hidden)
              )
      {
         DrawBarArea();
      }
   }

   if (id == CHARTEVENT_OBJECT_CLICK)
   {
      if (sparam == m_ShowBtn)
      {
         Show(true);
      }
      else if (sparam == m_HideBtn)
      {
         Show(false);
      }
   }
   
   if (id == CHARTEVENT_CUSTOM + EVENT_SETTING_CHANGED)
   {
      OnSettingChanged(int(lparam), sparam);
   }

}

InfoPane::Draw()
{
   DrawBarArea();
   DrawSpreadBar();
   DrawDaily();
   DrawOpen();
   DrawSwap();
   DrawTime();
   DrawLevels();
   DrawNews();

   if (!m_Hidden)
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 161, CORNER_LEFT_LOWER);
   }
   else
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY +  11, CORNER_LEFT_LOWER);
      Hide();   
   }
}

void InfoPane::Hide()
{
   ObjectDelete(0, m_HideBtn);

   ObjectDelete(0, m_SpreadUpBorder);
   ObjectDelete(0, m_SpreadDnBorder);
   ObjectDelete(0, m_SpreadArea);
   ObjectDelete(0, m_SpreadProgress);
   ObjectDelete(0, m_SpreadMax);
   ObjectDelete(0, m_SpreadMin);
   ObjectDelete(0, m_SpreadActual);

   ObjectDelete(0, m_DailyOpen);
   ObjectDelete(0, m_DailyDirection);
   ObjectDelete(0, m_DailyPercent);
   
   ObjectDelete(0, m_OpenLot);
   ObjectDelete(0, m_OpenDirection);
   ObjectDelete(0, m_OpenEquity);
   
   ObjectDelete(0, m_Swap);
   ObjectDelete(0, m_TimeDay);
   ObjectDelete(0, m_TimeClock);
   ObjectDelete(0, m_Levels);
   
   ObjectDelete(0, m_NewsName);
   ObjectDelete(0, m_NewsTime);
   ObjectDelete(0, m_Area);

   ObjectDelete(0, m_CopyRight);
}

InfoPane::~InfoPane()
{
   Hide();
   ObjectDelete(0, m_Border);
   ObjectDelete(0, m_ShowBtn);
}

void InfoPane::DrawBarArea()
{
   CreateObject(0, 0, m_Border, OBJ_EDIT, "");
   ObjectSetInteger(0, m_Border, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_COLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_Border, OBJPROP_XSIZE, 297);
   ObjectSetInteger(0, m_Border, OBJPROP_YSIZE, 5);
   
   if (m_Hidden)
   {
      CreateObject(0, 0, m_ShowBtn, OBJ_BUTTON, CharToString(191));
      MoveObject(0, m_ShowBtn, m_BaseX + 120, m_BaseY + 32, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_ShowBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_BGCOLOR, m_BordersColor);
   }
   else
   {
      CreateObject(0, 0, m_HideBtn, OBJ_BUTTON, CharToString(192));
      MoveObject(0, m_HideBtn, m_BaseX + 120, m_BaseY + 182, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_HideBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_HideBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_BGCOLOR, m_BordersColor);
      
      CreateObject(0, 0, m_Area, OBJ_EDIT, "");
      MoveObject(0, m_Area, m_BaseX + 4, m_BaseY + 150, CORNER_LEFT_LOWER);
      ObjectSetInteger(0, m_Area, OBJPROP_XSIZE, 295);
      ObjectSetInteger(0, m_Area, OBJPROP_YSIZE, 145);
      ObjectSetInteger(0, m_Area, OBJPROP_BGCOLOR, m_BordersColor);
      ObjectSetInteger(0, m_Area, OBJPROP_READONLY, true);
      
      CreateObject(0, 0, m_CopyRight, OBJ_LABEL, "Copyright (c) 2010, TheXpert");
      MoveObject(0, m_CopyRight, m_BaseX + 60, m_BaseY + 135, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_CopyRight, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_CopyRight, OBJPROP_FONTSIZE, 8);
      ObjectSetInteger(0, m_CopyRight, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_CopyRight, OBJPROP_ANCHOR, ANCHOR_LEFT_LOWER);
   }
}

void InfoPane::Show(bool needShow)
{
   if (needShow)
   {
      ObjectDelete(0, m_ShowBtn);
      
      if (m_Hidden) Slide(true);
      m_Hidden = false;
   }
   else
   {
      Hide();
      
      if (!m_Hidden) Slide(false);
      m_Hidden = true;
   }
   Draw();
}

void InfoPane::Slide(bool needShow)
{
   if (!m_UseSliding) return;
   
   #define INFO_SLIDE_SHIFTS_SIZE 24
   static const int SlideShifts[INFO_SLIDE_SHIFTS_SIZE] = {0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 56, 68, 82, 94, 105, 114, 122, 129, 135, 140, 144, 147, 149, 150};

   if (!needShow)
   {
      for (int i = 0; i < INFO_SLIDE_SHIFTS_SIZE; ++i)
      {
         MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 161 - SlideShifts[i], CORNER_LEFT_LOWER);
         ChartRedraw();
         Sleep(8);
      }
   }
   else
   {
      for (int i = INFO_SLIDE_SHIFTS_SIZE - 1; i >= 0; --i)
      {
         MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 161 - SlideShifts[i], CORNER_LEFT_LOWER);
         ChartRedraw();
         Sleep(8);
      }
   }
}

void InfoPane::UseSliding(bool use)
{
   m_UseSliding = use;
}

void InfoPane::OnTick()
{
   DrawSpreadBar();
   DrawDaily();
   DrawOpen();
   DrawLevels();
   DrawNews();
}

void InfoPane::DrawSpreadBar()
{
      if (m_Hidden) return;
      
      double maxs[];
      ArrayResize(maxs, m_SpreadInfoPeriod);
      CopyBuffer(m_SpreadInfoHandle, 0, 0, m_SpreadInfoPeriod, maxs);
      int max = int(maxs[ArrayMaximum(maxs)]);
      
      double mins[];
      ArrayResize(mins, m_SpreadInfoPeriod);
      CopyBuffer(m_SpreadInfoHandle, 1, 0, m_SpreadInfoPeriod, mins);
      int min = int(mins[ArrayMinimum(mins)]);
      
      double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      
      int current = int(MathRound((ask - bid)/_Point));
      
      int ratio = 50;
      if (max != min)
      {
         ratio = int(MathRound(100.0*(current - min)/(max - min)));
      }
      
      CreateObject(0, 0, m_SpreadUpBorder, OBJ_LABEL, CharToString(239));
      MoveObject(0, m_SpreadUpBorder, m_BaseX + 229, m_BaseY + 131, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadUpBorder, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_SpreadUpBorder, OBJPROP_FONTSIZE, 40);
      ObjectSetInteger(0, m_SpreadUpBorder, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadUpBorder, OBJPROP_ANCHOR, ANCHOR_LEFT_LOWER);

      CreateObject(0, 0, m_SpreadDnBorder, OBJ_LABEL, CharToString(240));
      MoveObject(0, m_SpreadDnBorder, m_BaseX + 229, m_BaseY + 32, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadDnBorder, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_SpreadDnBorder, OBJPROP_FONTSIZE, 40);
      ObjectSetInteger(0, m_SpreadDnBorder, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadDnBorder, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);

      CreateObject(0, 0, m_SpreadArea, OBJ_EDIT, "");
      MoveObject(0, m_SpreadArea, m_BaseX + 230, m_BaseY + 129, CORNER_LEFT_LOWER);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_READONLY, true);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_XSIZE, 30);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_YSIZE, 102);

      CreateObject(0, 0, m_SpreadProgress, OBJ_EDIT, "");
      MoveObject(0, m_SpreadProgress, m_BaseX + 231, m_BaseY + 28 + ratio, CORNER_LEFT_LOWER);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_COLOR, m_UpColor);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_BGCOLOR, m_UpColor);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_READONLY, true);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_XSIZE, 28);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_YSIZE, ratio);

      CreateObject(0, 0, m_SpreadMax, OBJ_LABEL, string(max));
      MoveObject(0, m_SpreadMax, m_BaseX + 260, m_BaseY + 131, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadMax, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_SpreadMax, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadMax, OBJPROP_ANCHOR, ANCHOR_LEFT_LOWER);
      ObjectSetInteger(0, m_SpreadMax, OBJPROP_FONTSIZE, 10);

      CreateObject(0, 0, m_SpreadMin, OBJ_LABEL, string(min));
      MoveObject(0, m_SpreadMin, m_BaseX + 260, m_BaseY + 26, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadMin, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_SpreadMin, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadMin, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_SpreadMin, OBJPROP_FONTSIZE, 10);

      CreateObject(0, 0, m_SpreadActual, OBJ_LABEL, string(current));
      MoveObject(0, m_SpreadActual, m_BaseX + 260, m_BaseY + 26 + int(MathMax(ratio, 15)), CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadActual, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_SpreadActual, OBJPROP_COLOR, m_UpColor);
      ObjectSetInteger(0, m_SpreadActual, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_SpreadActual, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawDaily()
{
      if (m_Hidden) return;
      
      double data[1];
      CopyOpen(_Symbol, PERIOD_D1, 0, 1, data);
      double open = data[0];
      double actual = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      double percent = 100.0*(actual - open)/actual;
      
      CreateObject(0, 0, m_DailyOpen, OBJ_LABEL, "Day Open: " + DoubleToString(open, _Digits));
      MoveObject(0, m_DailyOpen, m_BaseX + 150, m_BaseY + 115, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_DailyOpen, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_DailyOpen, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_DailyOpen, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
      ObjectSetInteger(0, m_DailyOpen, OBJPROP_FONTSIZE, 10);

      char arrowID = (percent < 0) ? 113 : 112;
      color clr = (percent < 0) ? m_DnColor : m_UpColor;
      
      CreateObject(0, 0, m_DailyDirection, OBJ_LABEL, CharToString(arrowID));
      MoveObject(0, m_DailyDirection, m_BaseX + 150, m_BaseY + 115, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_DailyDirection, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_DailyDirection, OBJPROP_COLOR, clr);
      ObjectSetInteger(0, m_DailyDirection, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_DailyDirection, OBJPROP_FONTSIZE, 10);
      
      string strPercent = (percent < 0) ? " - " : " + ";
      strPercent = strPercent + DoubleToString(MathAbs(percent), 2) + "%";

      CreateObject(0, 0, m_DailyPercent, OBJ_LABEL, strPercent);
      MoveObject(0, m_DailyPercent, m_BaseX + 160, m_BaseY + 115, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_DailyPercent, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_DailyPercent, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_DailyPercent, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_DailyPercent, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawOpen()
{
   if (m_Hidden) return;
   
   double lots = 0;
   double equity = 0;

   bool available = PositionSelect(_Symbol);
   if (available)
   {
      lots = PositionGetDouble(POSITION_VOLUME);
      
      if (lots > 0)
      {
         if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL) lots = -lots;
      }
      equity = 
         PositionGetDouble(POSITION_PROFIT) + 
         PositionGetDouble(POSITION_COMMISSION) + 
         PositionGetDouble(POSITION_SWAP);
   }
   
   string strLots = DoubleToString(MathAbs(lots), m_LotsDigits);
   if (lots == 0) strLots = "None";
   
   
   CreateObject(0, 0, m_OpenLot, OBJ_LABEL, strLots);
   MoveObject(0, m_OpenLot, m_BaseX + 15, m_BaseY + 95, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_OpenLot, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_OpenLot, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_OpenLot, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_OpenLot, OBJPROP_FONTSIZE, 10);

   string pos = " ";
   color clr = m_TextColor;
   
   if (lots > 0) 
   {
      pos = " Buy";
      clr = m_BuyColor;
   }
   else if (lots < 0)
   {
      pos = " Sell";
      clr = m_SellColor;
   }
   
   CreateObject(0, 0, m_OpenDirection, OBJ_LABEL, pos);
   MoveObject(0, m_OpenDirection, m_BaseX + 70, m_BaseY + 95, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_OpenDirection, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_OpenDirection, OBJPROP_COLOR, clr);
   ObjectSetInteger(0, m_OpenDirection, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_OpenDirection, OBJPROP_FONTSIZE, 10);
   
   string strEquity = (equity < 0) ? " - " : " + ";
   strEquity = strEquity + DoubleToString(MathAbs(equity), 2);

   clr = m_TextColor;
   
   if (equity > 0) 
   {
      clr = m_UpColor;
   }
   else if (equity < 0)
   {
      clr = m_DnColor;
   }

   CreateObject(0, 0, m_OpenEquity, OBJ_LABEL, strEquity);
   MoveObject(0, m_OpenEquity, m_BaseX + 110, m_BaseY + 95, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_OpenEquity, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_OpenEquity, OBJPROP_COLOR, clr);
   ObjectSetInteger(0, m_OpenEquity, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_OpenEquity, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawSwap()
{
   if (m_Hidden) return;
   
   double buySwap = SymbolInfoDouble(_Symbol, SYMBOL_SWAP_LONG);
   double sellSwap = SymbolInfoDouble(_Symbol, SYMBOL_SWAP_SHORT);
   
   string strSwap = "Swap long/short: " + DoubleToString(buySwap, 2) + "/" + DoubleToString(sellSwap, 2);
   
   CreateObject(0, 0, m_Swap, OBJ_LABEL, strSwap);
   MoveObject(0, m_Swap, m_BaseX + 15, m_BaseY + 75, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_Swap, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_Swap, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_Swap, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_Swap, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawTime()
{
   if (m_Hidden) return;

   datetime time = GetTime(m_TimeSettings);
   
   string day = DayToString(TimeDay(time));
   
   CreateObject(0, 0, m_TimeDay, OBJ_LABEL, day);
   MoveObject(0, m_TimeDay, m_BaseX + 15, m_BaseY + 145, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_TimeDay, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_TimeDay, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_TimeDay, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_TimeDay, OBJPROP_FONTSIZE, 15);

   CreateObject(0, 0, m_TimeClock, OBJ_LABEL, TimeToString(time, TIME_SECONDS) + " (" + GetTimeBase(m_TimeSettings) + ")");
   MoveObject(0, m_TimeClock, m_BaseX + 80, m_BaseY + 135, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_TimeClock, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_TimeClock, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_TimeClock, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_TimeClock, OBJPROP_FONTSIZE, 10);
}

void InfoPane::OnTimer()
{
   DrawTime();
}

void InfoPane::DrawLevels()
{
   if (m_Hidden) return;
   
   string levels = "Stop/Freeze Level: " +
      string(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL)) + "/" + 
      string(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_FREEZE_LEVEL));

   CreateObject(0, 0, m_Levels, OBJ_LABEL, levels);
   MoveObject(0, m_Levels, m_BaseX + 15, m_BaseY + 55, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_Levels, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_Levels, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_Levels, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_Levels, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawNews()
{
   if (m_Hidden) return;
   
   string news = GetNearestNews();
   
   string newsHeader = "None";
   string newsTime = " ";
   
   if (ObjectFind(0, news) >= 0)
   {
      newsHeader = StringSubstr(ObjectGetString(0, news, OBJPROP_NAME), 17);
      datetime time = datetime(ObjectGetInteger(0, news, OBJPROP_TIME, 0));
      
      newsTime = TimeToString(time, TIME_MINUTES);
   }
   
   CreateObject(0, 0, m_NewsName, OBJ_EDIT, newsHeader);
   MoveObject(0, m_NewsName, m_BaseX + 65, m_BaseY + 35, CORNER_LEFT_LOWER);
   ObjectSetInteger(0, m_NewsName, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_NewsName, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_NewsName, OBJPROP_FONTSIZE, 8);
   ObjectSetInteger(0, m_NewsName, OBJPROP_XSIZE, 160);
   ObjectSetInteger(0, m_NewsName, OBJPROP_YSIZE, 15);

   CreateObject(0, 0, m_NewsTime, OBJ_LABEL, newsTime);
   MoveObject(0, m_NewsTime, m_BaseX + 15, m_BaseY + 35, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_NewsTime, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_NewsTime, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_NewsTime, OBJPROP_FONTSIZE, 10);
}

void InfoPane::OnSettingChanged(int id, string value)
{
   switch (id)
   {
      case SETTING_USE_SLIDING_ID:
      {
         BoolTable table;
         m_UseSliding = table.GetValueByName(value);
      }
      break;
      
      case SETTING_COLOR_SCHEME_ID:
      {
         if (value == "1")
         {
            m_BordersColor = LightGray;
            m_TextColor = Black;
         }
         else if (value == "2")
         {
            m_BordersColor = Black;
            m_TextColor = LightGray;
         }
         
         Draw();
      }
      break;
      
      case SETTING_SPREAD_BARS:
      {
         m_SpreadInfoPeriod = int(value);
      }
      break;

      case SETTING_TIME:
      {
         m_TimeSettings = ETimeKind(value);
         DrawTime();
      }
      break;
   }
}

#include <Trade/SymbolInfo.mqh>

class TradePane
{
public:
   TradePane();
   ~TradePane();

   void Init();
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   void OnTick();
   void Show(bool needShow = true);
   
   bool Hidden() const;

private:
   void Draw();
   void Slide(bool needShow);
   void Hide();
   
   void DrawHideBorder();
   void DrawShowBtn();
   void DrawHideBtn();
   
   void DrawBuyWithTP();
   void DrawBuyWithSL();
   void DrawBuyStop();
   void DrawBuyLimit();

   void DrawSellWithTP();
   void DrawSellWithSL();
   void DrawSellStop();
   void DrawSellLimit();
   
   void DrawIncreaseLotBtn();
   void DrawDecreaseLotBtn();
   void DrawLots();
   
   void DrawAsk();
   void DrawBid();
   
   void DrawBuy();
   void DrawSell();
   void DrawClose();
   void DrawReverse();
   
   void DrawArea();

   void OnSettingChanged(int id, string value);
   
   void OnBuySL();
   void OnBuyTP();
   void OnBuyLimit();
   void OnBuyStop();
   void OnSellSL();
   void OnSellTP();
   void OnSellLimit();
   void OnSellStop();   

   void OnBuy();
   void OnSell();
   void OnClose();
   void OnReverse();
   
   bool CheckDropArea(string name);

private:
   bool m_UseSliding;
   bool m_Hidden;
   bool m_Inited;
   
   int m_UID;
   
   string m_HideBorder;

   string m_BuyBtn;
   string m_SellBtn;
   string m_ShowBtn;
   string m_HideBtn;

   string m_BuyWithTP;
   string m_BuyWithSL;
   string m_BuyStop;
   string m_BuyLimit;

   string m_SellWithTP;
   string m_SellWithSL;
   string m_SellStop;
   string m_SellLimit;
   
   string m_IncreaseLotBtn;
   string m_DecreaseLotBtn;
   string m_LotsEdit;
   
   string m_PreAsk;
   string m_LargeAsk;
   string m_PostAsk;
   string m_PreBid;
   string m_LargeBid;
   string m_PostBid;
   
   string m_CloseBtn;
   string m_ReverseBtn;
   string m_PaneArea;
   
   string m_BuyBtn_;
   string m_SellBtn_;
   string m_CloseBtn_;
   string m_ReverseBtn_;

   double m_LotsSize;
   int m_LotsDigits;
   
   color m_BordersColor;
   color m_TextColor;
   color m_BuyColor;
   color m_SellColor;
   color m_UpColor;
   color m_DnColor;
   
   CSymbolInfo m_SymbolInfo;
   
   PaneSettings m_Settings;
   
   string m_Comment;
   
   int m_SpreadInfoHandle;
   
   int m_BaseX;
   int m_BaseY;
   
   MyTrade m_Trade;
};

TradePane::TradePane(void)
{
   m_LotsSize = 0;
   m_LotsDigits = 0;
   m_BaseX = 0;
   m_BaseY = 0;
   
   m_UseSliding = true;
   m_Hidden = true;
   m_Inited = false;
   
   m_UID = GetUID();
   string sUID = string(m_UID);
   
   m_HideBorder = "HideBorder" + sUID;

   m_BuyBtn = "BuyBtn" + sUID;
   m_SellBtn = "SellBtn" + sUID;
   m_ShowBtn = "ShowBtn" + sUID;
   m_HideBtn = "HideBtn" + sUID;

   m_BuyWithTP = "BuyWithTP" + sUID;
   m_BuyWithSL = "BuyWithSL" + sUID;
   m_BuyStop = "BuyStop" + sUID;
   m_BuyLimit = "BuyLimit" + sUID;

   m_SellWithTP = "SellWithTP" + sUID;
   m_SellWithSL = "SellWithSL" + sUID;
   m_SellStop = "SellStop" + sUID;
   m_SellLimit = "SellLimit" + sUID;
   
   m_IncreaseLotBtn = "IncreaseLotBtn" + sUID;
   m_DecreaseLotBtn = "DecreaseLotBtn" + sUID;
   m_LotsEdit = "Lots" + sUID;
   
   m_PreAsk = "PreAsk" + sUID;
   m_LargeAsk = "LargeAsk" + sUID;
   m_PostAsk = "PostAsk" + sUID;
   
   m_PreBid = "PreBid" + sUID;
   m_LargeBid = "LargeBid" + sUID;
   m_PostBid = "PostBid" + sUID;
   
   m_CloseBtn = "CloseBtn" + sUID;
   m_ReverseBtn = "ReverseBtn" + sUID;
   
   m_PaneArea = "PaneArea" + sUID;
   
   m_BuyBtn_ = "BuyBtn_" + sUID;
   m_SellBtn_ = "SellBtn_" + sUID;
   m_CloseBtn_ = "CloseBtn_" + sUID;
   m_ReverseBtn_ = "ReverseBtn_" + sUID;
   
   m_BordersColor = Black;
   m_TextColor = LightGray;
   m_BuyColor = DodgerBlue;
   m_SellColor = Tomato;
   m_UpColor = SeaGreen;
   m_DnColor = Tomato;
   
   m_Comment = "Trade Xpert";
   
   m_Settings.AddBoolSetting("Use Sliding", SETTING_USE_SLIDING_ID, true);
   
   SelectionTable colors;
   colors.AddSelection(1, "Light\\Dark");
   colors.AddSelection(2, "Dark\\Light");
   
   m_Settings.AddSelectionSetting("Color scheme", SETTING_COLOR_SCHEME_ID, 2, colors);
   m_Settings.AddStringSetting("Orders Comment", SETTING_COMMENT_ID, m_Comment);
   
   m_Settings.AddIntSetting("Log Font Size", SETTING_LOG_FONT_SIZE,8, 1, 5, 20);
   
   SelectionTable logColors;
   logColors.AddSelection(LightGreen, "LightGreen");
   logColors.AddSelection(LightGray, "LightGray");
   logColors.AddSelection(White, "White");
   logColors.AddSelection(Black, "Black");
   logColors.AddSelection(Salmon, "Salmon");
   logColors.AddSelection(PowderBlue, "PowderBlue");
   logColors.AddSelection(DarkGray, "DarkGray");
   logColors.AddSelection(MidnightBlue, "MidnightBlue");

   m_Settings.AddSelectionSetting("Log Font Color", SETTING_LOG_FONT_COLOR, LightGray, logColors);

   m_Settings.AddIntSetting("Log Lines", SETTING_LOG_FONT_LINES, 10, 1, 1, 20);

   m_Settings.AddIntSetting("Log Line Length", SETTING_LOG_FONT_LENGTH, 60, 1, 20, 62);

   m_Settings.AddIntSetting("Log Tab Size", SETTING_LOG_FONT_TABSIZE, 8, 1, 2, 20);

   SelectionTable time;
   time.AddSelection(ETIME_CET, "CET");
   time.AddSelection(ETIME_EST, "EST");
   time.AddSelection(ETIME_GMT, "GMT");
   time.AddSelection(ETIME_LOCAL, "Local");
   time.AddSelection(ETIME_MOSCOW, "Moscow");
   time.AddSelection(ETIME_SERVER, "Server");

   m_Settings.AddSelectionSetting("Time to show", SETTING_TIME, ETIME_SERVER, time);
   
   m_Settings.AddIntSetting("Spread Bars", SETTING_SPREAD_BARS, 5, 1, 1, 50);

   m_Settings.AddIntSetting("Slippage", SETTING_TRADE_SLIPPAGE, 5, 1, 1, 1000);
   
   SelectionTable fillings;
   fillings.AddSelection(ORDER_FILLING_FOK, "Fill or Kill");
   fillings.AddSelection(ORDER_FILLING_IOC, "Available");
   fillings.AddSelection(ORDER_FILLING_RETURN, "Available+");

   m_Settings.AddSelectionSetting("Order filling", SETTING_TRADE_FILLING, ORDER_FILLING_FOK, fillings);

   m_Settings.SetColors(m_BordersColor, m_TextColor);
   
   m_SpreadInfoHandle = iCustom(_Symbol, PERIOD_CURRENT, "SpreadInfo");
   
}

void TradePane::Init()
{
   m_SymbolInfo.Name(_Symbol);
   m_LotsDigits = DoubleDigits(m_SymbolInfo.LotsStep());
   
   Draw();
   m_Settings.Show(false);
}

void TradePane::Hide()
{
   ObjectDelete(0, m_BuyBtn);
   ObjectDelete(0, m_SellBtn);
   ObjectDelete(0, m_ShowBtn);
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_BuyWithTP);
   ObjectDelete(0, m_BuyWithSL);
   ObjectDelete(0, m_BuyStop);
   ObjectDelete(0, m_BuyLimit);
   ObjectDelete(0, m_SellWithTP);
   ObjectDelete(0, m_SellWithSL);
   ObjectDelete(0, m_SellStop);
   ObjectDelete(0, m_SellLimit);

   ObjectDelete(0, m_IncreaseLotBtn);
   ObjectDelete(0, m_DecreaseLotBtn);
   ObjectDelete(0, m_LotsEdit);
   
   ObjectDelete(0, m_PreAsk);
   ObjectDelete(0, m_LargeAsk);
   ObjectDelete(0, m_PostAsk);
   ObjectDelete(0, m_PreBid);
   ObjectDelete(0, m_LargeBid);
   ObjectDelete(0, m_PostBid);
   
   ObjectDelete(0, m_CloseBtn);
   ObjectDelete(0, m_ReverseBtn);

   ObjectDelete(0, m_PaneArea);
   
   ObjectDelete(0, m_BuyBtn_);
   ObjectDelete(0, m_SellBtn_);
   ObjectDelete(0, m_CloseBtn_);
   ObjectDelete(0, m_ReverseBtn_);
}

void TradePane::Show(bool needShow)
{
   if (needShow && m_Hidden)
   {
      m_Hidden = false;
      ObjectDelete(0, m_ShowBtn);
      Slide(true);
   }
   else if (!needShow && !m_Hidden)
   {
      m_Hidden = true;
      Hide();
      Slide(false);
   }
   
   Draw();
}

void TradePane::Draw()
{
   DrawHideBorder();

   // 210 is Y center point
   if (m_Hidden)
   {
      DrawShowBtn();
      
      MoveObject(0, m_HideBorder, m_BaseX + 13, m_BaseY + 35, CORNER_RIGHT_UPPER);
   }
   else
   {
      DrawArea();

      DrawHideBtn();
      
      DrawBuyWithTP();
      DrawBuyWithSL();
      DrawBuyLimit();
      DrawBuyStop();
      
      DrawSellWithTP();
      DrawSellWithSL();
      DrawSellLimit();
      DrawSellStop();
      
      DrawLots();
      DrawAsk();
      DrawBid();

      DrawIncreaseLotBtn();
      DrawDecreaseLotBtn();
      
      DrawBuy();
      DrawSell();
      DrawClose();
      DrawReverse();
      
      MoveObject(0, m_HideBorder, m_BaseX + 163, m_BaseY + 35, CORNER_RIGHT_UPPER);
   }
}

void TradePane::Slide(bool needShow)
{
   if (!m_UseSliding) return;
   
   #define PANE_SLIDE_SHIFTS_SIZE 24
   static const int SlideShifts[PANE_SLIDE_SHIFTS_SIZE] = {0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 56, 68, 82, 94, 105, 114, 122, 129, 135, 140, 144, 147, 149, 150};

   if (needShow)
   {
      for (int i = 0; i < PANE_SLIDE_SHIFTS_SIZE; ++i)
      {
         MoveObject(0, m_HideBorder, m_BaseX + SlideShifts[i] + 13, m_BaseY + 35, CORNER_RIGHT_UPPER);
         ChartRedraw();
         Sleep(10);
      }
   }
   else
   {
      for (int i = PANE_SLIDE_SHIFTS_SIZE - 1; i >= 0; --i)
      {
         MoveObject(0, m_HideBorder, m_BaseX + SlideShifts[i] - 81, m_BaseY + 30, CORNER_RIGHT_UPPER);
         ChartRedraw();
         Sleep(10);
      }
   }
}

void TradePane::DrawHideBorder()
{
   CreateObject(0, 0, m_HideBorder, OBJ_EDIT, "");
   ObjectSetInteger(0, m_HideBorder, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_COLOR, PaleTurquoise);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_XSIZE, 5);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_YSIZE, 350);
}

void TradePane::DrawShowBtn()
{
   CreateObject(0, 0, m_ShowBtn, OBJ_BUTTON, CharToString(189));
   MoveObject(0, m_ShowBtn, m_BaseX + 35, m_BaseY + 180, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_ShowBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_XSIZE, 15);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_YSIZE, 60);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_COLOR, m_TextColor);
}

void TradePane::DrawHideBtn()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_HideBtn, OBJ_BUTTON, CharToString(190));
   MoveObject(0, m_HideBtn, m_BaseX + 185, m_BaseY + 180, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_HideBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_HideBtn, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_XSIZE, 15);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_YSIZE, 60);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_COLOR, m_TextColor);
}

void TradePane::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   m_Settings.OnChartEvent(id, lparam, dparam, sparam);
   
   if (id == CHARTEVENT_OBJECT_CLICK)
   {
      if (sparam == m_ShowBtn)
      {
         Show(true);
         ObjectSetInteger(0, m_ShowBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_HideBtn)
      {
         Show(false);
         ObjectSetInteger(0, m_HideBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_DecreaseLotBtn)
      {
         m_LotsSize = 
            StringToDouble(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));

         m_LotsSize = CorrectLot(m_LotsSize - m_SymbolInfo.LotsStep());
         DrawLots();

         ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_IncreaseLotBtn)
      {
         m_LotsSize = 
            StringToDouble(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));

         if (m_LotsSize == 0) m_LotsSize = m_SymbolInfo.LotsMin();
         else
         {
            m_LotsSize = CorrectLot(m_LotsSize + m_SymbolInfo.LotsStep());
         }
         DrawLots();

         ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_BuyBtn || sparam == m_BuyBtn_)
      {
         OnBuy();
      }
      else if (sparam == m_SellBtn || sparam == m_SellBtn_)
      {
         OnSell();
      }
      else if (sparam == m_CloseBtn || sparam == m_CloseBtn_)
      {
         OnClose();
      }
      else if (sparam == m_ReverseBtn || sparam == m_ReverseBtn_)
      {
         OnReverse();
      }
   }

   if (id == CHARTEVENT_OBJECT_ENDEDIT)
   {
      if (sparam == m_LotsEdit)
      {
         m_LotsSize = CorrectLot
            (
               StringToDouble(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT))
            );
         DrawLots();
      }
   }
   
   if (id == CHARTEVENT_CUSTOM + EVENT_SETTING_CHANGED)
   {
      OnSettingChanged(int(lparam), sparam);
   }

   if (id == CHARTEVENT_OBJECT_DRAG)
   {
      if (sparam == m_BuyWithSL)
      {
         OnBuySL();
      }
      else if (sparam == m_BuyWithTP)
      {
         OnBuyTP();
      }
      else if (sparam == m_BuyLimit)
      {
         OnBuyLimit();
      }
      else if (sparam == m_BuyStop)
      {
         OnBuyStop();
      }
      else if (sparam == m_SellWithSL)
      {
         OnSellSL();
      }
      else if (sparam == m_SellWithTP)
      {
         OnSellTP();
      }
      else if (sparam == m_SellLimit)
      {
         OnSellLimit();
      }
      else if (sparam == m_SellStop)
      {
         OnSellStop();
      }
   }
}

TradePane::~TradePane(void)
{
   ObjectDelete(0, m_HideBorder);

   ObjectDelete(0, m_BuyBtn);
   ObjectDelete(0, m_SellBtn);
   ObjectDelete(0, m_ShowBtn);
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_BuyWithTP);
   ObjectDelete(0, m_BuyWithSL);
   ObjectDelete(0, m_BuyStop);
   ObjectDelete(0, m_BuyLimit);
   ObjectDelete(0, m_SellWithTP);
   ObjectDelete(0, m_SellWithSL);
   ObjectDelete(0, m_SellStop);
   ObjectDelete(0, m_SellLimit);
   
   ObjectDelete(0, m_IncreaseLotBtn);
   ObjectDelete(0, m_DecreaseLotBtn);
   ObjectDelete(0, m_LotsEdit);
   
   ObjectDelete(0, m_PreAsk);
   ObjectDelete(0, m_LargeAsk);
   ObjectDelete(0, m_PostAsk);
   ObjectDelete(0, m_PreBid);
   ObjectDelete(0, m_LargeBid);
   ObjectDelete(0, m_PostBid);
   
   ObjectDelete(0, m_CloseBtn);
   ObjectDelete(0, m_ReverseBtn);
   
   ObjectDelete(0, m_PaneArea);

   ObjectDelete(0, m_BuyBtn_);
   ObjectDelete(0, m_SellBtn_);
   ObjectDelete(0, m_CloseBtn_);
   ObjectDelete(0, m_ReverseBtn_);
}

void TradePane::DrawBuyWithTP()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyWithTP, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyWithTP, m_BaseX + 130, m_BaseY + 50, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyWithTP, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyWithTP, OBJPROP_BMPFILE, 0, "BuyTP" + light + ".bmp");
   ObjectSetString(0, m_BuyWithTP, OBJPROP_BMPFILE, 1, "BuyTP" + light + ".bmp");
}

void TradePane::DrawBuyWithSL()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyWithSL, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyWithSL, m_BaseX + 70, m_BaseY + 285, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyWithSL, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyWithSL, OBJPROP_BMPFILE, 0, "BuySL" + light + ".bmp");
   ObjectSetString(0, m_BuyWithSL, OBJPROP_BMPFILE, 1, "BuySL" + light + ".bmp");
}

void TradePane::DrawBuyStop()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyStop, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyStop, m_BaseX + 70, m_BaseY + 50, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyStop, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyStop, OBJPROP_BMPFILE, 0, "BuyStop" + light + ".bmp");
   ObjectSetString(0, m_BuyStop, OBJPROP_BMPFILE, 1, "BuyStop" + light + ".bmp");
}

void TradePane::DrawBuyLimit()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyLimit, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyLimit, m_BaseX + 130, m_BaseY + 285, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyLimit, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyLimit, OBJPROP_BMPFILE, 0, "BuyLimit" + light + ".bmp");
   ObjectSetString(0, m_BuyLimit, OBJPROP_BMPFILE, 1, "BuyLimit" + light + ".bmp");
}

void TradePane::DrawSellWithTP()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellWithTP, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellWithTP, m_BaseX + 130, m_BaseY + 335, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellWithTP, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellWithTP, OBJPROP_BMPFILE, 0, "SellTP" + light + ".bmp");
   ObjectSetString(0, m_SellWithTP, OBJPROP_BMPFILE, 1, "SellTP" + light + ".bmp");
}

void TradePane::DrawSellWithSL()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellWithSL, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellWithSL, m_BaseX + 70, m_BaseY + 100, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellWithSL, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellWithSL, OBJPROP_BMPFILE, 0, "SellSL" + light + ".bmp");
   ObjectSetString(0, m_SellWithSL, OBJPROP_BMPFILE, 1, "SellSL" + light + ".bmp");
}

void TradePane::DrawSellStop()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellStop, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellStop, m_BaseX + 70, m_BaseY + 335, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellStop, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellStop, OBJPROP_BMPFILE, 0, "SellStop" + light + ".bmp");
   ObjectSetString(0, m_SellStop, OBJPROP_BMPFILE, 1, "SellStop" + light + ".bmp");
}

void TradePane::DrawSellLimit()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellLimit, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellLimit, m_BaseX + 130, m_BaseY + 100, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellLimit, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellLimit, OBJPROP_BMPFILE, 0, "SellLimit" + light + ".bmp");
   ObjectSetString(0, m_SellLimit, OBJPROP_BMPFILE, 1, "SellLimit" + light + ".bmp");
}

void TradePane::DrawIncreaseLotBtn()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_IncreaseLotBtn, OBJ_BUTTON, CharToString(129));
   MoveObject(0, m_IncreaseLotBtn, m_BaseX + 102, m_BaseY + 185, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_IncreaseLotBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_FONTSIZE, 6);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_XSIZE, 50);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_YSIZE, 10);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_COLOR, Black);
}

void TradePane::DrawDecreaseLotBtn()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_DecreaseLotBtn, OBJ_BUTTON, CharToString(130));
   MoveObject(0, m_DecreaseLotBtn, m_BaseX + 102, m_BaseY + 225, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_DecreaseLotBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_FONTSIZE, 6);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_XSIZE, 50);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_YSIZE, 10);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_COLOR, Black);
}

void TradePane::DrawLots()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_LotsEdit, OBJ_EDIT, DoubleToString(m_LotsSize, m_LotsDigits));
   MoveObject(0, m_LotsEdit, m_BaseX + 102, m_BaseY + 195, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_FONTSIZE, 15);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_XSIZE, 50);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_YSIZE, 30);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_BGCOLOR, m_TextColor);
}

void TradePane::DrawAsk()
{
   if (m_Hidden) return;
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   
   string pre, large, post;
   PreparePrice(_Symbol, ask, pre, large, post);
   
   int len = StringLen(large);
   for (int i = 0; i < len; i++)
   {
      pre = pre + "       ";
   }

   CreateObject(0, 0, m_PreAsk, OBJ_LABEL, string(pre));
   MoveObject(0, m_PreAsk, m_BaseX + 50, m_BaseY + 145, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PreAsk, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PreAsk, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_PreAsk, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PreAsk, OBJPROP_COLOR, m_TextColor);
   
   double values[1];
   CopyBuffer(m_SpreadInfoHandle, 2, 0, 1, values);
   
   double value = values[0];
   color clr = m_TextColor;
   
   if (value != EMPTY_VALUE)
   {
      if (value == 1) clr = m_UpColor;
      if (value == -1) clr = m_DnColor;
   }

   CreateObject(0, 0, m_LargeAsk, OBJ_LABEL, string(large));
   MoveObject(0, m_LargeAsk, m_BaseX + 50, m_BaseY + 135, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_LargeAsk, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_LargeAsk, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_LargeAsk, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_LargeAsk, OBJPROP_COLOR, clr);

   CreateObject(0, 0, m_PostAsk, OBJ_LABEL, string(post));
   MoveObject(0, m_PostAsk, m_BaseX + 50, m_BaseY + 145, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PostAsk, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PostAsk, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetString(0, m_PostAsk, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PostAsk, OBJPROP_COLOR, m_TextColor);
}

void TradePane::DrawBid()
{
   if (m_Hidden) return;
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   
   string pre, large, post;
   PreparePrice(_Symbol, bid, pre, large, post);
   
   int len = StringLen(large);
   for (int i = 0; i < len; i++)
   {
      pre = pre + "       ";
   }

   CreateObject(0, 0, m_PreBid, OBJ_LABEL, string(pre));
   MoveObject(0, m_PreBid, m_BaseX + 50, m_BaseY + 255, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PreBid, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PreBid, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_PreBid, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PreBid, OBJPROP_COLOR, m_TextColor);

   double values[1];
   CopyBuffer(m_SpreadInfoHandle, 3, 0, 1, values);
   
   double value = values[0];
   color clr = m_TextColor;
   
   if (value != EMPTY_VALUE)
   {
      if (value == 1) clr = m_UpColor;
      if (value == -1) clr = m_DnColor;
   }

   CreateObject(0, 0, m_LargeBid, OBJ_LABEL, string(large));
   MoveObject(0, m_LargeBid, m_BaseX + 50, m_BaseY + 225, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_LargeBid, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_LargeBid, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_LargeBid, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_LargeBid, OBJPROP_COLOR, clr);

   CreateObject(0, 0, m_PostBid, OBJ_LABEL, string(post));
   MoveObject(0, m_PostBid, m_BaseX + 50, m_BaseY + 255, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PostBid, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PostBid, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetString(0, m_PostBid, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PostBid, OBJPROP_COLOR, m_TextColor);
}

void TradePane::DrawBuy()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyBtn_, OBJ_EDIT, "");
   MoveObject(0, m_BuyBtn_, m_BaseX + 140, m_BaseY + 175, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_YSIZE, 33);
   
   CreateObject(0, 0, m_BuyBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_BuyBtn, m_BaseX + 140, m_BaseY + 175, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyBtn, OBJPROP_BMPFILE, 0, "Buy" + light + ".bmp");
   ObjectSetString(0, m_BuyBtn, OBJPROP_BMPFILE, 1, "Buy" + light + ".bmp");
}

void TradePane::DrawSell()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellBtn_, OBJ_EDIT, "");
   MoveObject(0, m_SellBtn_, m_BaseX + 140, m_BaseY + 215, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_YSIZE, 33);

   CreateObject(0, 0, m_SellBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_SellBtn, m_BaseX + 140, m_BaseY + 215, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellBtn, OBJPROP_BMPFILE, 0, "Sell" + light + ".bmp");
   ObjectSetString(0, m_SellBtn, OBJPROP_BMPFILE, 1, "Sell" + light + ".bmp");
}

void TradePane::DrawClose()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_CloseBtn_, OBJ_EDIT, "");
   MoveObject(0, m_CloseBtn_, m_BaseX + 47, m_BaseY + 175, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_YSIZE, 33);

   CreateObject(0, 0, m_CloseBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_CloseBtn, m_BaseX + 47, m_BaseY + 175, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_CloseBtn, OBJPROP_BMPFILE, 0, "Close" + light + ".bmp");
   ObjectSetString(0, m_CloseBtn, OBJPROP_BMPFILE, 1, "Close" + light + ".bmp");
}

void TradePane::DrawReverse()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_ReverseBtn_, OBJ_EDIT, "");
   MoveObject(0, m_ReverseBtn_, m_BaseX + 47, m_BaseY + 215, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_YSIZE, 33);

   CreateObject(0, 0, m_ReverseBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_ReverseBtn, m_BaseX + 47, m_BaseY + 215, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_ReverseBtn, OBJPROP_BMPFILE, 0, "Reverse" + light + ".bmp");
   ObjectSetString(0, m_ReverseBtn, OBJPROP_BMPFILE, 1, "Reverse" + light + ".bmp");
}

void TradePane::DrawArea()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_PaneArea, OBJ_EDIT, "");
   MoveObject(0, m_PaneArea, m_BaseX + 150, m_BaseY + 35, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_XSIZE, 140);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_YSIZE, 350);
}

void TradePane::OnSettingChanged(int id, string value)
{
   switch (id)
   {
      case SETTING_USE_SLIDING_ID:
      {
         BoolTable table;
         m_UseSliding = table.GetValueByName(value);
         m_Settings.UseSliding(m_UseSliding);
      }
      break;
      
      case SETTING_COLOR_SCHEME_ID:
      {
         if (value == "1")
         {
            m_BordersColor = LightGray;
            m_TextColor = Black;
         }
         else if (value == "2")
         {
            m_BordersColor = Black;
            m_TextColor = LightGray;
         }
         
         Draw();

         m_Settings.SetColors(m_BordersColor, m_TextColor);
         m_Settings.DrawBar();
      }
      break;
      
      case SETTING_COMMENT_ID:
      {
         Comment_("Orders comment changed to: " + value);
         m_Comment = value;
      }
      break;
      
      case SETTING_TRADE_SLIPPAGE:
      {
         Comment_("Deviation changed to: " + value);
         m_Trade.SetSlippage(int(value));
      }
      break;

      case SETTING_TRADE_FILLING:
      {
         SelectionTable fillings;
         fillings.AddSelection(ORDER_FILLING_FOK, "Fill or Kill");
         fillings.AddSelection(ORDER_FILLING_IOC, "Available");
         fillings.AddSelection(ORDER_FILLING_RETURN, "Available+");
         
         ENUM_ORDER_TYPE_FILLING filling = ENUM_ORDER_TYPE_FILLING(value);
         
         Comment_("Orders filling changed to: " + fillings.GetNameByID(filling));

         m_Trade.SetFilling(filling);
      }
      break;
   }
}

void TradePane::OnTick()
{
   DrawAsk();
   DrawBid();
}

bool TradePane::CheckDropArea(string name)
{
   int x = int(ObjectGetInteger(0, name, OBJPROP_XDISTANCE));
   
   if (x < m_BaseX + 170)
   {
      Comment_("To perform the action please drop the pointer outside the Pane area");
      return false;
   }
   return true;
}


void TradePane::OnBuySL()
{
   if (!CheckDropArea(m_BuyWithSL))
   {
      DrawBuyWithSL();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyWithSL, OBJPROP_YDISTANCE));
   DrawBuyWithSL();
   ChartRedraw();
   
   Comment_("Trying to Buy with SL");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.BuySL(lot, price);
}

void TradePane::OnBuyTP()
{
   if (!CheckDropArea(m_BuyWithTP))
   {
      DrawBuyWithTP();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyWithTP, OBJPROP_YDISTANCE));
   DrawBuyWithTP();
   ChartRedraw();
   
   Comment_("Trying to Buy with TP");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.BuyTP(lot, price);
}

void TradePane::OnBuyLimit()
{
   if (!CheckDropArea(m_BuyLimit))
   {
      DrawBuyLimit();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyLimit, OBJPROP_YDISTANCE));
   DrawBuyLimit();
   ChartRedraw();

   Comment_("Trying to place Buy Limit");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.OpenBuyLimit(lot, price);
}

void TradePane::OnBuyStop()
{
   if (!CheckDropArea(m_BuyStop))
   {
      DrawBuyStop();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyStop, OBJPROP_YDISTANCE));
   DrawBuyStop();
   ChartRedraw();

   Comment_("Trying to place Buy Stop");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }

   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.OpenBuyStop(lot, price);
}

void TradePane::OnSellSL()
{
   if (!CheckDropArea(m_SellWithSL))
   {
      DrawSellWithSL();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellWithSL, OBJPROP_YDISTANCE));
   DrawSellWithSL();
   ChartRedraw();
   
   Comment_("Trying to Sell with SL");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.SellSL(lot, price);
}

void TradePane::OnSellTP()
{
   if (!CheckDropArea(m_SellWithTP))
   {
      DrawSellWithTP();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellWithTP, OBJPROP_YDISTANCE));
   DrawSellWithTP();
   ChartRedraw();
   
   Comment_("Trying to Sell with TP");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.SellTP(lot, price);
}

void TradePane::OnSellLimit()
{
   if (!CheckDropArea(m_SellLimit))
   {
      DrawSellLimit();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellLimit, OBJPROP_YDISTANCE));
   DrawSellLimit();
   ChartRedraw();
   
   Comment_("Trying to place Sell Limit");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }

   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   Comment_("Sell Limit price = " + DoubleToString(price, _Digits));
   m_Trade.OpenSellLimit(lot, price);
}

void TradePane::OnSellStop()   
{
   if (!CheckDropArea(m_SellStop))
   {
      DrawSellStop();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellStop, OBJPROP_YDISTANCE));
   DrawSellStop();
   ChartRedraw();
   
   Comment_("Trying to place Sell Stop");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }

   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.OpenSellStop(lot, price);
}

void TradePane::OnBuy()
{
   Comment_("Trying to Buy");
   ChartRedraw();
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }
   m_Trade.Buy(lot);
}

void TradePane::OnSell()
{
   Comment_("Trying to Sell");
   ChartRedraw();
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }
   m_Trade.Sell(lot);
}

void TradePane::OnClose()
{
   Comment_("Trying to Close position");
   ChartRedraw();

   m_Trade.Close();
}

void TradePane::OnReverse()
{
   Comment_("Trying to Reverse position");
   ChartRedraw();

   m_Trade.Reverse();
}

#define COMMENTS_INDIE_TRIGGER_NAME "Logs for Trade Xpert"

class Commenter
{
public:
   Commenter();
   ~Commenter();
   
   int Window() const;
   void Draw();
   void Hide();
   
   void OnTick();
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   
private:
   void OnSettingChanged(int id, string value);

private:

   int m_UID;
   color m_TextColor;
   int m_FontSize;
};

Commenter::Commenter(void)
{
   m_UID = GetUID();
   m_TextColor = LightGray;
   m_FontSize = 8;
}

int Commenter::Window() const
{
   int windows = int(ChartGetInteger(0, CHART_WINDOWS_TOTAL));
   
   for (int i = 0; i < windows; i++)
   {
      int indies = ChartIndicatorsTotal(0, i);
      
      for (int j = 0; j < indies; j++)
      {
         if (ChartIndicatorName(0, i, j) == COMMENTS_INDIE_TRIGGER_NAME)
         {
            return i;
         }
      }
   }
   return -1;
}

void Commenter::Draw()
{
   int wnd = Window();
   if (wnd == -1) return;
   
   int lines = GetLines();
   
   for (int i = 0; i < lines; i++)
   {
      string pre = "Prefix" + string(m_UID)+ " " + string(i);
      string text = "Text" + string(m_UID)+ " " + string(i);
      
      string preValue;
      string textValue;
      
      if (GetLine(i, preValue, textValue))
      {
         if (StringLen(preValue) == 0) preValue = " ";
         if (StringLen(textValue) == 0) textValue= " ";
      
         CreateObject(0, wnd, pre, OBJ_LABEL, preValue);
         ObjectSetString(0, pre, OBJPROP_FONT, "Lucida Console");
         ObjectSetInteger(0, pre, OBJPROP_FONTSIZE, m_FontSize);
         ObjectSetInteger(0, pre, OBJPROP_XDISTANCE, 10);
         ObjectSetInteger(0, pre, OBJPROP_YDISTANCE, 20 + i*int(1.5*m_FontSize));
         ObjectSetInteger(0, pre, OBJPROP_COLOR, m_TextColor);
         
         CreateObject(0, wnd, text, OBJ_LABEL, textValue);
         ObjectSetString(0, text, OBJPROP_FONT, "Lucida Console");
         ObjectSetInteger(0, text, OBJPROP_FONTSIZE, m_FontSize);
         ObjectSetInteger(0, text, OBJPROP_XDISTANCE, 10 + 10*m_FontSize);
         ObjectSetInteger(0, text, OBJPROP_YDISTANCE, 20 + i*int(1.5*m_FontSize));
         ObjectSetInteger(0, text, OBJPROP_COLOR, m_TextColor);
      }
      else break;
   }
}

void Commenter::Hide()
{
   int wnd = Window();
   if (wnd == -1) return;

   int total = ObjectsTotal(0, wnd, OBJ_LABEL);
   
   for (int i = total - 1; i >= 0; i--)
   {
      string name = ObjectName(0, i, wnd, OBJ_LABEL);
      if (
            StringFind(name, "Prefix" + string(m_UID)+ " ") == 0 ||
            StringFind(name, "Text" + string(m_UID)+ " ") == 0
         )
      {
         ObjectDelete(0, name);
      }
   }
}

void Commenter::OnTick()
{
   Draw();
}

Commenter::~Commenter()
{
   Hide();
}


void Commenter::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   if (id == CHARTEVENT_CUSTOM + EVENT_SETTING_CHANGED)
   {
      OnSettingChanged(int(lparam), sparam);
   }
   Draw();
}

void Commenter::OnSettingChanged(int id, string value)
{
   switch (id)
   {
      case SETTING_LOG_FONT_SIZE:
      {
         m_FontSize = int(value);
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_LINES:
      {
         SetLines(int(value));
         Hide();
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_LENGTH:
      {
         SetLength(int(value));
         Hide();
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_TABSIZE:
      {
         SetTabSize(int(value));
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_COLOR:
      {
         m_TextColor = color(value);
         Draw();
      }
      break;
      
   }
}