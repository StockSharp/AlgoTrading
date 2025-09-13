//+------------------------------------------------------------------+
//|                                                     ClassRow.mqh |
//|                                                 Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
//--- Объявление констант
#define  DELTA   1     // зазор между элементами по умолчанию
//--- Подключаем файлы классов
#include <ClassUnit.mqh>
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Базовый класс СТРОКА  CRow                                       |
//+------------------------------------------------------------------+
class CRow
  {
protected:
   bool              on_event;      // флаг обработки событий
public:
   string            name;          // имя строки
   WinCell           Property;      // свойства строки
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRow();
   virtual     // Метод: нарисовать строку
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
//| Конструктор класса CRow                                          |
//+------------------------------------------------------------------+
void CRow::CRow()
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
//| Метод Draw класса CRow                                           |
//+------------------------------------------------------------------+
void CRow::Draw(string m_name,
                int m_xdelta,
                int m_ydelta,
                int m_bsize)
  {
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRow                 |
//+------------------------------------------------------------------+
void CRow::OnEvent(const int id,
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
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс СТРОКА тип 1:  CRowType1                                   |
//+------------------------------------------------------------------+
class CRowType1:public CRow
  {
public:
   CCellText         Text;
   CCellButtonType   Hide,Close;
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRowType1();
   virtual     // Метод: нарисовать строку
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_type,
                          string m_text);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CRowType1                                     |
//+------------------------------------------------------------------+
void CRowType1::CRowType1()
  {
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRowType1            |
//+------------------------------------------------------------------+
void CRowType1::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Hide.OnEvent(id,lparam,dparam,sparam);
      Close.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CRowType1                                      |
//+------------------------------------------------------------------+
void CRowType1::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     int m_type,
                     string m_text)
  {
   int      X,B;
   Text.Property=Property;
   Hide.Property=Property;
   Close.Property=Property;
//--- тип 0: m_type=0
   if(m_type<=0)
     {
      name=m_name+".RowType1(0)";
      B=m_bsize-2*(Property.H+DELTA);
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
      //---
      X=m_xdelta+Property.Corn*(B+DELTA);
      Hide.Draw(name,X,m_ydelta,0);
      //---
      X=X+Property.Corn*(Property.H+DELTA);
      Close.Draw(name,X,m_ydelta,1);
     }
//--- тип 1: m_type=1
   if(m_type==1)
     {
      name=m_name+".RowType1(1)";
      B=m_bsize-(Property.H+DELTA);
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
      //---
      X=m_xdelta+Property.Corn*(B+DELTA);
      Close.Draw(name,X,m_ydelta,1);
     }
//--- тип 2: m_type=2
   if(m_type==2)
     {
      name=m_name+".RowType1(2)";
      B=m_bsize-(Property.H+DELTA);
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
      //---
      X=m_xdelta+Property.Corn*(B+DELTA);
      Hide.Draw(name,X,m_ydelta,0);
     }
//--- тип 3: m_type=3
   if(m_type>=3)
     {
      name=m_name+".RowType1(3)";
      B=m_bsize;
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
     }
//---
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс СТРОКА тип 2:  CRowType2                                   |
//+------------------------------------------------------------------+
class CRowType2:public CRow
  {
public:
   CCellText         Text;
   CCellEdit         Edit;
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRowType2();
   virtual     // Метод: нарисовать строку
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_tsize,
                          string m_text,
                          string m_edit);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CRowType2                                     |
//+------------------------------------------------------------------+
void CRowType2::CRowType2()
  {
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRowType2            |
//+------------------------------------------------------------------+
void CRowType2::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Edit.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CRowType2                                      |
//+------------------------------------------------------------------+
void CRowType2::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     int m_tsize,
                     string m_text,
                     string m_edit)
  {
   int      X,B;
   Text.Property=Property;
   Edit.Property=Property;
   name=m_name+".RowType2";
   Text.Draw(name,m_xdelta,m_ydelta,m_tsize,m_text);
//---
   B=m_bsize-m_tsize-DELTA;
   X=m_xdelta+Property.Corn*(m_tsize+DELTA);
   Edit.Draw(name,X,m_ydelta,B,m_edit,false);
//---
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс СТРОКА тип 3:  CRowType3                                   |
//+------------------------------------------------------------------+
class CRowType3:public CRow
  {
public:
   CCellText         Text;
   CCellEdit         Edit;
   CCellButtonType   Plus,Minus;
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRowType3();
   virtual     // Метод: нарисовать строку
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_tsize,
                          string m_text,
                          string m_edit);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CRowType3                                     |
//+------------------------------------------------------------------+
void CRowType3::CRowType3()
  {
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRowType3            |
//+------------------------------------------------------------------+
void CRowType3::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Edit.OnEvent(id,lparam,dparam,sparam);
      Plus.OnEvent(id,lparam,dparam,sparam);
      Minus.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CRowType3                                      |
//+------------------------------------------------------------------+
void CRowType3::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     int m_tsize,
                     string m_text,
                     string m_edit)
  {
   int      X,B;
   Text.Property=Property;
   Edit.Property=Property;
   Plus.Property=Property;
   Minus.Property=Property;
   name=m_name+".RowType3";
   Text.Draw(name,m_xdelta,m_ydelta,m_tsize,m_text);
//---
   B=m_bsize-(m_tsize+DELTA)-2*(Property.H+DELTA);
   X=m_xdelta+Property.Corn*(m_tsize+DELTA);
   Edit.Draw(name,X,m_ydelta,B,m_edit,true);
//---
   X=X+Property.Corn*(B+DELTA);
   Plus.Draw(name,X,m_ydelta,3);
//---
   X=X+Property.Corn*(Property.H+DELTA);
   Minus.Draw(name,X,m_ydelta,4);
//---
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс СТРОКА тип 4:  CRowType4                                   |
//+------------------------------------------------------------------+
class CRowType4:public CRow
  {
public:
   CCellText         Text;
   CCellEdit         Edit;
   CCellButtonType   Plus,Minus,Up,Down;
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRowType4();
   virtual     // Метод: нарисовать строку
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_tsize,
                          string m_text,
                          string m_edit);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CRowType4                                     |
//+------------------------------------------------------------------+
void CRowType4::CRowType4()
  {
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRowType4            |
//+------------------------------------------------------------------+
void CRowType4::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Edit.OnEvent(id,lparam,dparam,sparam);
      Plus.OnEvent(id,lparam,dparam,sparam);
      Minus.OnEvent(id,lparam,dparam,sparam);
      Up.OnEvent(id,lparam,dparam,sparam);
      Down.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CRowType4                                      |
//+------------------------------------------------------------------+
void CRowType4::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     int m_tsize,
                     string m_text,
                     string m_edit)
  {
   int      X,B;
   Text.Property=Property;
   Edit.Property=Property;
   Plus.Property=Property;
   Minus.Property=Property;
   Up.Property=Property;
   Down.Property=Property;
   name=m_name+".RowType4";
   Text.Draw(name,m_xdelta,m_ydelta,m_tsize,m_text);
//---
   B=m_bsize-(m_tsize+DELTA)-4*(Property.H+DELTA);
   X=m_xdelta+Property.Corn*(m_tsize+DELTA);
   Edit.Draw(name,X,m_ydelta,B,m_edit,true);
//---
   X=X+Property.Corn*(B+DELTA);
   Plus.Draw(name,X,m_ydelta,3);
//---
   X=X+Property.Corn*(Property.H+DELTA);
   Minus.Draw(name,X,m_ydelta,4);
//---
   X=X+Property.Corn*(Property.H+DELTA);
   Up.Draw(name,X,m_ydelta,5);
//---
   X=X+Property.Corn*(Property.H+DELTA);
   Down.Draw(name,X,m_ydelta,6);
//---
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс СТРОКА тип 5:  CRowType5                                   |
//+------------------------------------------------------------------+
class CRowType5:public CRow
  {
public:
   CCellText         Text;
   CCellButton       Button;
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRowType5();
   virtual     // Метод: нарисовать строку
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_csize,
                          string m_text,
                          string m_button);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CRowType5                                     |
//+------------------------------------------------------------------+
void CRowType5::CRowType5()
  {
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRowType5            |
//+------------------------------------------------------------------+
void CRowType5::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Button.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CRowType5                                      |
//+------------------------------------------------------------------+
void CRowType5::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     int m_csize,
                     string m_text,
                     string m_button)
  {
   int      X,B;
   Text.Property=Property;
   Button.Property=Property;
   name=m_name+".RowType5";
   Text.Draw(name,m_xdelta,m_ydelta,m_csize,m_text);
//---
   B=m_bsize-m_csize-DELTA;
   X=m_xdelta+Property.Corn*(m_csize+DELTA);
   Button.Draw(name,X,m_ydelta,B,m_button);
//---
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| Класс СТРОКА тип 6:  CRowType6                                   |
//+------------------------------------------------------------------+
class CRowType6:public CRow
  {
public:
   CCellButton       Button;
   //+---------------------------------------------------------------+
   // Конструктор класса
   void              CRowType6();
   virtual     // Метод: нарисовать строку
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_b1size,
                          int m_b2size,
                          string m_button1,
                          string m_button2,
                          string m_button3);
   virtual     // Метод обработки события OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| Конструктор класса CRowType6                                     |
//+------------------------------------------------------------------+
void CRowType6::CRowType6()
  {
   on_event=false;   // запрещаем обработку событий
  }
//+------------------------------------------------------------------+
//| Метод обработки события OnChartEvent класса CRowType6            |
//+------------------------------------------------------------------+
void CRowType6::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // обработка событий разрешена
     {
      Button.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| Метод Draw класса CRowType6                                      |
//+------------------------------------------------------------------+
void CRowType6::Draw(string m_name,
                     int m_xdelta,
                     int m_ydelta,
                     int m_bsize,
                     int m_b1size,
                     int m_b2size,
                     string m_button1,
                     string m_button2,
                     string m_button3
                     )
  {
   int      X,B;
   Button.Property=Property;
//---
   name=m_name+".RowType6(1)";
   B=m_b1size;
   X=m_xdelta;
   Button.Draw(name,X,m_ydelta,B,m_button1);
//---
   name=m_name+".RowType6(2)";
   B=m_b2size;
   X=X+Property.Corn*(m_b1size+DELTA);
   Button.Draw(name,X,m_ydelta,B,m_button2);
//---
   name=m_name+".RowType6(3)";
   B=m_bsize-(m_b1size+DELTA)-(m_b2size+DELTA);
   X=X+Property.Corn*(m_b2size+DELTA);
   Button.Draw(name,X,m_ydelta,B,m_button3);
//+------------------------------------------------------------------+
   on_event=true;   // разрешаем обработку событий
  }
//+------------------------------------------------------------------+
