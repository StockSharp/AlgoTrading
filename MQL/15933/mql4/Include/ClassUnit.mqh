//+------------------------------------------------------------------+
//|                                                    ClassUnit.mqh |
//|                                                 Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property strict
#property copyright     "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
#property version       "1.00"
#property description   "MasterWindows Library"
//--- Объявление констант
#define  MAX_WIN     50    // код кнопки
#define  MIN_WIN     48    // код кнопки
#define  CLOSE_WIN   208   // код кнопки
#define  PAGE_UP     112   // код кнопки
#define  PAGE_DOWN   113   // код кнопки
#define  TIME_SLEEP  50    // "тормоз" на реакцию события
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Структура свойств объектов WinCell                               |
//+------------------------------------------------------------------+
struct WinCell
  {
   color             TextColor;     // цвет текста
   color             BGColor;       // цвет фона
   color             BGEditColor;   // цвет фона при редактировании
   ENUM_BASE_CORNER  Corner;        // угол привязки
   int               H;             // высота ячейки
   int               Corn;          // направление смещения (1;-1)
  };
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Базовый класс ЯЧЕЙКА  CCell                                      |
//+------------------------------------------------------------------+
class CCell
  {
private:
protected:
   bool              on_event;      // флаг обработки событий
   ENUM_OBJECT       type;          // тип ячейки
public:
   WinCell           Property;      // свойства ячейки
   string            name;          // имя ячейки
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CCell();
   virtual     // Метод: нарисовать объект
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CCell                                         |
//+------------------------------------------------------------------+
void CCell::CCell()
  {
   Property.TextColor=clrWhite;
   Property.BGColor=clrSteelBlue;
   Property.BGEditColor=clrDimGray;
   Property.Corner=CORNER_LEFT_UPPER;
   Property.Corn=1;
   Property.H=18;
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CCell                                          |
//+------------------------------------------------------------------+
void CCell::Draw(string m_name,
                 int m_xdelta,
                 int m_ydelta,
                 int m_bsize)
  {
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CCell                |
//+------------------------------------------------------------------+
void CCell::OnEvent(const int id,
                    const long &lparam,
                    const double &dparam,
                    const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      //--- нажатие кнопки
      if((ENUM_CHART_EVENT)id==CHARTEVENT_OBJECT_CLICK && StringFind(sparam,".Button",0)>0)
        {
         if(ObjectGetInteger(0,sparam,OBJPROP_STATE)==1)
           {
            //--- если кнопка залипла
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
//| Класс ЯЧЕЙКА:  CCellText                                         |
//+------------------------------------------------------------------+
class CCellText:public CCell
  {
public:
   // Конструктор класса
   void              CCellText();
   virtual     // Метод: нарисовать объект
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          string m_text);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CCellText                                     |
//+------------------------------------------------------------------+
void CCellText::CCellText()
  {
   type=OBJ_EDIT;
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CCellText                                      |
//+------------------------------------------------------------------+
void CCellText::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     string m_text)
  {
//--- создаём объект с модифицированным именем
   name=m_name+".Text";
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- инициализируем свойства объекта
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
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс ЯЧЕЙКА:  CCellEdit                                         |
//+------------------------------------------------------------------+
class CCellEdit:public CCell
  {
public:
   // Конструктор класса
   void              CCellEdit();
   virtual     // Метод: нарисовать объект
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          string m_text,
                          bool m_read);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CCellEdit                                     |
//+------------------------------------------------------------------+
void CCellEdit::CCellEdit()
  {
   type=OBJ_EDIT;
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CCellEdit                                      |
//+------------------------------------------------------------------+
void CCellEdit::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     string m_text,
                     bool m_read)
  {
//--- создаём объект с модифицированным именем
   name=m_name+".Edit";
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- инициализируем свойства объекта
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
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс ЯЧЕЙКА:  CCellButton                                       |
//+------------------------------------------------------------------+
class CCellButton:public CCell
  {
public:
   // Конструктор класса
   void              CCellButton();
   virtual     // Метод: нарисовать объект
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          string m_button);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CCellButton                                   |
//+------------------------------------------------------------------+
void CCellButton::CCellButton()
  {
   type=OBJ_BUTTON;
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CCellButton                                    |
//+------------------------------------------------------------------+
void CCellButton::Draw(string m_name,
                       int m_xdelta,
                       int m_ydelta,
                       int m_bsize,
                       string m_button)
  {
//--- создаём объект с модифицированным именем
   name=m_name+".Button";
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- инициализируем свойства объекта
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
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс ЯЧЕЙКА:  CCellButtonType                                   |
//+------------------------------------------------------------------+
class CCellButtonType:public CCell
  {
public:
   // Конструктор класса
   void              CCellButtonType();
   virtual     // Метод: нарисовать объект
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_type);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CCellButtonType                               |
//+------------------------------------------------------------------+
void CCellButtonType::CCellButtonType()
  {
   type=OBJ_BUTTON;
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CCellButtonType                                |
//+------------------------------------------------------------------+
void CCellButtonType::Draw(string m_name,
                           int m_xdelta,
                           int m_ydelta,
                           int m_type)
  {
//--- создаём объект с модифицированным именем
   if(m_type<=0) m_type=0;
   name=m_name+".Button"+(string)m_type;
   if(ObjectCreate(0,name,type,0,0,0,0,0)==false)
      Print("Function ",__FUNCTION__," error ",GetLastError());
//--- инициализируем свойства объекта
   ObjectSetInteger(0,name,OBJPROP_COLOR,Property.TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,Property.BGColor);
   ObjectSetInteger(0,name,OBJPROP_CORNER,Property.Corner);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,m_xdelta);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,m_ydelta);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,Property.H);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,Property.H);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,0);
   if(m_type==0) // Кнопка Hide
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(MIN_WIN));
      ObjectSetString(0,name,OBJPROP_FONT,"Webdings");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,12);
     }
   if(m_type==1) // Кнопка Close
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(CLOSE_WIN));
      ObjectSetString(0,name,OBJPROP_FONT,"Wingdings 2");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,8);
     }
   if(m_type==2) // Кнопка Return
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(MAX_WIN));
      ObjectSetString(0,name,OBJPROP_FONT,"Webdings");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,12);
     }
   if(m_type==3) // Кнопка Plus
     {
      ObjectSetString(0,name,OBJPROP_TEXT,"+");
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,10);
     }
   if(m_type==4) // Кнопка Minus
     {
      ObjectSetString(0,name,OBJPROP_TEXT,"-");
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,13);
     }
   if(m_type==5) // Кнопка PageUp
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(PAGE_UP));
      ObjectSetString(0,name,OBJPROP_FONT,"Wingdings 3");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,8);
     }
   if(m_type==6) // Кнопка PageDown
     {
      ObjectSetString(0,name,OBJPROP_TEXT,CharToString(PAGE_DOWN));
      ObjectSetString(0,name,OBJPROP_FONT,"Wingdings 3");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,8);
     }
   if(m_type>6) // Кнопка пустая
     {
      ObjectSetString(0,name,OBJPROP_TEXT,"");
      ObjectSetString(0,name,OBJPROP_FONT,"Arial");
      ObjectSetInteger(0,name,OBJPROP_FONTSIZE,13);
     }
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
