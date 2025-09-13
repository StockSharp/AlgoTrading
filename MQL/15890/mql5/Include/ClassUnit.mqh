//+------------------------------------------------------------------+
//|                                                    ClassUnit.mqh |
//|                                                 Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
//--- ���������� ��������
#define  MAX_WIN     50    // ��� ������
#define  MIN_WIN     48    // ��� ������
#define  CLOSE_WIN   208   // ��� ������
#define  PAGE_UP     112   // ��� ������
#define  PAGE_DOWN   113   // ��� ������
#define  TIME_SLEEP  50    // "������" �� ������� �������
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ��������� ������� �������� WinCell                               |
//+------------------------------------------------------------------+
struct WinCell
  {
   color             TextColor;     // ���� ������
   color             BGColor;       // ���� ����
   color             BGEditColor;   // ���� ���� ��� ��������������
   ENUM_BASE_CORNER  Corner;        // ���� ��������
   int               H;             // ������ ������
   int               Corn;          // ����������� �������� (1;-1)
  };
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ������� ����� ������  CCell                                      |
//+------------------------------------------------------------------+
class CCell
  {
private:
protected:
   bool              on_event;      // ���� ��������� �������
   ENUM_OBJECT       type;          // ��� ������
public:
   WinCell           Property;      // �������� ������
   string            name;          // ��� ������
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CCell();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CCell                                         |
//+------------------------------------------------------------------+
void CCell::CCell()
  {
   Property.TextColor=clrWhite;
   Property.BGColor=clrSteelBlue;
   Property.BGEditColor=clrDimGray;
   Property.Corner=CORNER_LEFT_UPPER;
   Property.Corn=1;
   Property.H=18;
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CCell                                          |
//+------------------------------------------------------------------+
void CCell::Draw(string m_name,
                 int m_xdelta,
                 int m_ydelta,
                 int m_bsize)
  {
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CCell                |
//+------------------------------------------------------------------+
void CCell::OnEvent(const int id,
                    const long &lparam,
                    const double &dparam,
                    const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      //--- ������� ������
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK && StringFind(sparam,".Button",0)>0)
        {
         if(ObjectGetInteger(0,sparam,OBJPROP_STATE)==1)
           {
            //--- ���� ������ �������
            Sleep(TIME_SLEEP);
            ObjectSetInteger(0,sparam,OBJPROP_STATE,0);
            ChartRedraw();
           }
        }
     }
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������:  CCellText                                         |
//+------------------------------------------------------------------+
class CCellText:public CCell
  {
public:
   // ����������� ������
   void              CCellText();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          string m_text);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CCellText                                     |
//+------------------------------------------------------------------+
void CCellText::CCellText()
  {
   type=OBJ_EDIT;
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CCellText                                      |
//+------------------------------------------------------------------+
void CCellText::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     string m_text)
  {
//--- ������ ������ � ���������������� ������
   name=m_name+".Text";
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- �������������� �������� �������
   ObjectSetInteger(0,name,OBJPROP_COLOR,Property.TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,Property.BGColor);
   ObjectSetInteger(0,name,OBJPROP_READONLY,true);
   ObjectSetInteger(0,name,OBJPROP_CORNER,Property.Corner);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,m_xdelta);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,m_ydelta);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,m_bsize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,Property.H);
   ObjectSetString(0,name,OBJPROP_FONT,"Arial");
   ObjectSetString(0,name,OBJPROP_TEXT,m_text);
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,10);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,0);
//---
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������:  CCellEdit                                         |
//+------------------------------------------------------------------+
class CCellEdit:public CCell
  {
public:
   // ����������� ������
   void              CCellEdit();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          string m_text,
                          bool m_read);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CCellEdit                                     |
//+------------------------------------------------------------------+
void CCellEdit::CCellEdit()
  {
   type=OBJ_EDIT;
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CCellEdit                                      |
//+------------------------------------------------------------------+
void CCellEdit::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     string m_text,
                     bool m_read)
  {
//--- ������ ������ � ���������������� ������
   name=m_name+".Edit";
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- �������������� �������� �������
   ObjectSetInteger(0,name,OBJPROP_COLOR,Property.TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,Property.BGEditColor);
   ObjectSetInteger(0,name,OBJPROP_READONLY,m_read);
   ObjectSetInteger(0,name,OBJPROP_CORNER,Property.Corner);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,m_xdelta);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,m_ydelta);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,m_bsize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,Property.H);
   ObjectSetString(0,name,OBJPROP_FONT,"Arial");
   ObjectSetString(0,name,OBJPROP_TEXT,m_text);
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,10);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,0);
//---
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������:  CCellButton                                       |
//+------------------------------------------------------------------+
class CCellButton:public CCell
  {
public:
   // ����������� ������
   void              CCellButton();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          string m_button);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CCellButton                                   |
//+------------------------------------------------------------------+
void CCellButton::CCellButton()
  {
   type=OBJ_BUTTON;
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CCellButton                                    |
//+------------------------------------------------------------------+
void CCellButton::Draw(string m_name,
                       int m_xdelta,
                       int m_ydelta,
                       int m_bsize,
                       string m_button)
  {
//--- ������ ������ � ���������������� ������
   name=m_name+".Button";
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- �������������� �������� �������
   ObjectSetInteger(0,name,OBJPROP_COLOR,Property.TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,Property.BGColor);
   ObjectSetInteger(0,name,OBJPROP_CORNER,Property.Corner);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,m_xdelta);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,m_ydelta);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,m_bsize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,Property.H);
   ObjectSetString(0,name,OBJPROP_FONT,"Arial");
   ObjectSetString(0,name,OBJPROP_TEXT,m_button);
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,10);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,0);
//---
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������:  CCellButtonType                                   |
//+------------------------------------------------------------------+
class CCellButtonType:public CCell
  {
public:
   // ����������� ������
   void              CCellButtonType();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_type);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CCellButtonType                               |
//+------------------------------------------------------------------+
void CCellButtonType::CCellButtonType()
  {
   type=OBJ_BUTTON;
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CCellButtonType                                |
//+------------------------------------------------------------------+
void CCellButtonType::Draw(string m_name,
                           int m_xdelta,
                           int m_ydelta,
                           int m_type)
  {
//--- ������ ������ � ���������������� ������
   if(m_type<=0) m_type=0;
   name=m_name+".Button"+(string)m_type;
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- �������������� �������� �������
   ObjectSetInteger(0,name,OBJPROP_COLOR,Property.TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,Property.BGColor);
   ObjectSetInteger(0,name,OBJPROP_CORNER,Property.Corner);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,m_xdelta);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,m_ydelta);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,Property.H);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,Property.H);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,0);
   if(m_type==0) // ������ Hide
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(MIN_WIN));
      ObjectSetString(0,name,OBJPROP_FONT,"Webdings");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,12);
     }
   if(m_type==1) // ������ Close
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(CLOSE_WIN));
      ObjectSetString(0,name,OBJPROP_FONT,"Wingdings 2");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,8);
     }
   if(m_type==2) // ������ Return
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(MAX_WIN));
      ObjectSetString(0,name,OBJPROP_FONT,"Webdings");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,12);
     }
   if(m_type==3) // ������ Plus
     {
      ObjectSetString(0,name,OBJPROP_TEXT,"+");
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,10);
     }
   if(m_type==4) // ������ Minus
     {
      ObjectSetString(0,name,OBJPROP_TEXT,"-");
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,13);
     }
   if(m_type==5) // ������ PageUp
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(PAGE_UP));
      ObjectSetString(0,name,OBJPROP_FONT,"Wingdings 3");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,8);
     }
   if(m_type==6) // ������ PageDown
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(PAGE_DOWN));
      ObjectSetString(0,name,OBJPROP_FONT,"Wingdings 3");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,8);
     }
   if(m_type>6) // ������ ������
     {
      ObjectSetString(0,name,OBJPROP_TEXT,"");
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,13);
     }
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
