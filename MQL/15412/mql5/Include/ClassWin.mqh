//+------------------------------------------------------------------+
//|                                                     ClassWin.mqh |
//|                                                 Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
//--- ќбъ€вление констант
#define  B_WIN       150   // ширина окна по умолчанию
//--- ѕодключаем файлы классов
#include <ClassRow.mqh>
//+------------------------------------------------------------------+
//| Ѕазовый класс ќ Ќќ  CWin                                         |
//+------------------------------------------------------------------+
class CWin
  {
private:
   void              SetXY(int m_corner);// ћетод расчЄта координат
protected:
   bool              on_event;   // флаг обработки событий
public:
   string            name;       // им€ окна
   int               w_corner;   // угол прив€зки
   int               w_xdelta;   // вертикальный отступ
   int               w_ydelta;   // горизонтальный отступ
   int               w_xpos;     // координата X точки прив€зки
   int               w_ypos;     // координата Y точки прив€зки
   int               w_bsize;    // ширина окна
   int               w_hsize;    // высота окна
   int               w_h_corner; // угол прив€зки HIDE режима
   WinCell           Property;   // свойства окна
   //---
   CRowType1         STR1;       // объ€вление строки класса
   CRowType2         STR2;       // объ€вление строки класса
   CRowType3         STR3;       // объ€вление строки класса
   CRowType4         STR4;       // объ€вление строки класса
   CRowType5         STR5;       // объ€вление строки класса
   CRowType6         STR6;       // объ€вление строки класса
   //+---------------------------------------------------------------+
   //  онструктор класса
   void              CWin();
   // ћетод получени€ данных
   void              SetWin(string m_name,
                            int m_xdelta,
                            int m_ydelta,
                            int m_bsize,
                            int m_corner);
   virtual     // ћетод: нарисовать окно формы
   void              Draw(int &MMint[][3],
                          string &MMstr[][3],
                          int count);
   virtual     // ћетод обработки событи€ OnEventTick
   void              OnEventTick();
   virtual     // ћетод обработки событи€ OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//|  онструктор класса CWin                                          |
//+------------------------------------------------------------------+
void CWin::CWin()
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
//| ћетод SetWin класса CWin                                         |
//+------------------------------------------------------------------+
void CWin::SetWin(string m_name,
                  int m_xdelta,
                  int m_ydelta,
                  int m_bsize,
                  int m_corner)
  {
   name=m_name;
//---
   if((ENUM_BASE_CORNER)m_corner==CORNER_LEFT_UPPER) w_corner=m_corner;
   else
      if((ENUM_BASE_CORNER)m_corner==CORNER_RIGHT_UPPER) w_corner=m_corner;
   else
      if((ENUM_BASE_CORNER)m_corner==CORNER_LEFT_LOWER) w_corner=CORNER_LEFT_UPPER;
   else
      if((ENUM_BASE_CORNER)m_corner==CORNER_RIGHT_LOWER) w_corner=CORNER_RIGHT_UPPER;
   else
     {
      Print("Error setting the anchor corner = ",m_corner);
      w_corner=CORNER_LEFT_UPPER;
     }
   if(m_xdelta>=0)w_xdelta=m_xdelta;
   else
     {
      Print("The offset error X = ",m_xdelta);
      w_xdelta=0;
     }
   if(m_ydelta>=0)w_ydelta=m_ydelta;
   else
     {
      Print("The offset error Y = ",m_ydelta);
      w_ydelta=0;
     }
   if(m_bsize>0)w_bsize=m_bsize;
   else
     {
      Print("Error setting the window width = ",m_bsize);
      w_bsize=B_WIN;
     }
   Property.Corner=(ENUM_BASE_CORNER)w_corner;
   SetXY(w_corner);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CWin::SetXY(int m_corner)
  {
   if((ENUM_BASE_CORNER)m_corner==CORNER_LEFT_UPPER)
     {
      w_xpos=w_xdelta;
      w_ypos=w_ydelta;
      Property.Corn=1;
     }
   else
   if((ENUM_BASE_CORNER)m_corner==CORNER_RIGHT_UPPER)
     {
      w_xpos=w_xdelta+w_bsize;
      w_ypos=w_ydelta;
      Property.Corn=-1;
     }
   else
   if((ENUM_BASE_CORNER)m_corner==CORNER_LEFT_LOWER)
     {
      w_xpos=w_xdelta;
      w_ypos=w_ydelta+w_hsize+Property.H;
      Property.Corn=1;
     }
   else
   if((ENUM_BASE_CORNER)m_corner==CORNER_RIGHT_LOWER)
     {
      w_xpos=w_xdelta+w_bsize;
      w_ypos=w_ydelta+w_hsize+Property.H;
      Property.Corn=-1;
     }
   else
     {
      Print("Error setting the anchor corner = ",m_corner);
      w_corner=CORNER_LEFT_UPPER;
      w_xpos=0;
      w_ypos=0;
      Property.Corn=1;
     }
//---
   if((ENUM_BASE_CORNER)w_corner==CORNER_LEFT_UPPER) w_h_corner=CORNER_LEFT_LOWER;
   if((ENUM_BASE_CORNER)w_corner==CORNER_LEFT_LOWER) w_h_corner=CORNER_LEFT_LOWER;
   if((ENUM_BASE_CORNER)w_corner==CORNER_RIGHT_UPPER) w_h_corner=CORNER_RIGHT_LOWER;
   if((ENUM_BASE_CORNER)w_corner==CORNER_RIGHT_LOWER) w_h_corner=CORNER_RIGHT_LOWER;
//---
  }
//+------------------------------------------------------------------+
//| ћетод обработки событи€ OnEventTick класса CWin                  |
//+------------------------------------------------------------------+
void CWin::OnEventTick()
  {
//---
  }
//+------------------------------------------------------------------+
//| ћетод обработки событи€ OnChartEvent класса CWin(переопределить!)|
//+------------------------------------------------------------------+
void CWin::OnEvent(const int id,
                   const long &lparam,
                   const double &dparam,
                   const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      //---
     }
  }
//+------------------------------------------------------------------+
//| ћетод Draw класса CWin                                           |
//+------------------------------------------------------------------+
void CWin::Draw(int &MMint[][3],
                string &MMstr[][3],
                int count)
  {
   STR1.Property=Property;
   STR2.Property=Property;
   STR3.Property=Property;
   STR4.Property=Property;
   STR5.Property=Property;
   STR6.Property=Property;
//---
   int X,Y,B;
   string   strname;
   X=w_xpos;
   Y=w_ypos;
   B=w_bsize;
   for(int i=0; i<=count; i++)
     {
      strname=".STR"+(string)i;
      if(MMint[i][0]==1) STR1.Draw(name+strname,X,Y,B,
         MMint[i][1],MMstr[i][0]);
      if(MMint[i][0]==2) STR2.Draw(name+strname,X,Y,B,
         MMint[i][1],MMstr[i][0],MMstr[i][1]);
      if(MMint[i][0]==3) STR3.Draw(name+strname,X,Y,B,
         MMint[i][1],MMstr[i][0],MMstr[i][1]);
      if(MMint[i][0]==4) STR4.Draw(name+strname,X,Y,B,
         MMint[i][1],MMstr[i][0],MMstr[i][1]);
      if(MMint[i][0]==5) STR5.Draw(name+strname,X,Y,B,
         MMint[i][1],MMstr[i][0],MMstr[i][1]);
      if(MMint[i][0]==6) STR6.Draw(name+strname,X,Y,B,
         MMint[i][1],MMint[i][2],MMstr[i][0],MMstr[i][1],MMstr[i][2]);
      Y=Y+Property.H+DELTA;
     }
//---
   ChartRedraw();
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
