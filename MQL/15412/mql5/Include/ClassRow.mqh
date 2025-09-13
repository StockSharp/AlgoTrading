//+------------------------------------------------------------------+
//|                                                     ClassRow.mqh |
//|                                                 Copyright DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright     "Copyright 2010-2016, DC2008"
#property link          "http://www.mql5.com/ru/users/dc2008"
//--- ���������� ��������
#define  DELTA   1     // ����� ����� ���������� �� ���������
//--- ���������� ����� �������
#include <ClassUnit.mqh>
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ������� ����� ������  CRow                                       |
//+------------------------------------------------------------------+
class CRow
  {
protected:
   bool              on_event;      // ���� ��������� �������
public:
   string            name;          // ��� ������
   WinCell           Property;      // �������� ������
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRow();
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
//| ����������� ������ CRow                                          |
//+------------------------------------------------------------------+
void CRow::CRow()
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
//| ����� Draw ������ CRow                                           |
//+------------------------------------------------------------------+
void CRow::Draw(string m_name,
                int m_xdelta,
                int m_ydelta,
                int m_bsize)
  {
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRow                 |
//+------------------------------------------------------------------+
void CRow::OnEvent(const int id,
                   const long &lparam,
                   const double &dparam,
                   const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      //---
     }
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������ ��� 1:  CRowType1                                   |
//+------------------------------------------------------------------+
class CRowType1:public CRow
  {
public:
   CCellText         Text;
   CCellButtonType   Hide,Close;
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRowType1();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_type,
                          string m_text);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CRowType1                                     |
//+------------------------------------------------------------------+
void CRowType1::CRowType1()
  {
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRowType1            |
//+------------------------------------------------------------------+
void CRowType1::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Hide.OnEvent(id,lparam,dparam,sparam);
      Close.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CRowType1                                      |
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
//--- ��� 0: m_type=0
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
//--- ��� 1: m_type=1
   if(m_type==1)
     {
      name=m_name+".RowType1(1)";
      B=m_bsize-(Property.H+DELTA);
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
      //---
      X=m_xdelta+Property.Corn*(B+DELTA);
      Close.Draw(name,X,m_ydelta,1);
     }
//--- ��� 2: m_type=2
   if(m_type==2)
     {
      name=m_name+".RowType1(2)";
      B=m_bsize-(Property.H+DELTA);
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
      //---
      X=m_xdelta+Property.Corn*(B+DELTA);
      Hide.Draw(name,X,m_ydelta,0);
     }
//--- ��� 3: m_type=3
   if(m_type>=3)
     {
      name=m_name+".RowType1(3)";
      B=m_bsize;
      Text.Draw(name,m_xdelta,m_ydelta,B,m_text);
     }
//---
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������ ��� 2:  CRowType2                                   |
//+------------------------------------------------------------------+
class CRowType2:public CRow
  {
public:
   CCellText         Text;
   CCellEdit         Edit;
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRowType2();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_tsize,
                          string m_text,
                          string m_edit);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CRowType2                                     |
//+------------------------------------------------------------------+
void CRowType2::CRowType2()
  {
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRowType2            |
//+------------------------------------------------------------------+
void CRowType2::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Edit.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CRowType2                                      |
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
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������ ��� 3:  CRowType3                                   |
//+------------------------------------------------------------------+
class CRowType3:public CRow
  {
public:
   CCellText         Text;
   CCellEdit         Edit;
   CCellButtonType   Plus,Minus;
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRowType3();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_tsize,
                          string m_text,
                          string m_edit);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CRowType3                                     |
//+------------------------------------------------------------------+
void CRowType3::CRowType3()
  {
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRowType3            |
//+------------------------------------------------------------------+
void CRowType3::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Edit.OnEvent(id,lparam,dparam,sparam);
      Plus.OnEvent(id,lparam,dparam,sparam);
      Minus.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CRowType3                                      |
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
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������ ��� 4:  CRowType4                                   |
//+------------------------------------------------------------------+
class CRowType4:public CRow
  {
public:
   CCellText         Text;
   CCellEdit         Edit;
   CCellButtonType   Plus,Minus,Up,Down;
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRowType4();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_tsize,
                          string m_text,
                          string m_edit);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CRowType4                                     |
//+------------------------------------------------------------------+
void CRowType4::CRowType4()
  {
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRowType4            |
//+------------------------------------------------------------------+
void CRowType4::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
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
//| ����� Draw ������ CRowType4                                      |
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
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������ ��� 5:  CRowType5                                   |
//+------------------------------------------------------------------+
class CRowType5:public CRow
  {
public:
   CCellText         Text;
   CCellButton       Button;
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRowType5();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_csize,
                          string m_text,
                          string m_button);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CRowType5                                     |
//+------------------------------------------------------------------+
void CRowType5::CRowType5()
  {
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRowType5            |
//+------------------------------------------------------------------+
void CRowType5::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      Text.OnEvent(id,lparam,dparam,sparam);
      Button.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CRowType5                                      |
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
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//////////////////////////////////////////////////////////////////////
//+------------------------------------------------------------------+
//| ����� ������ ��� 6:  CRowType6                                   |
//+------------------------------------------------------------------+
class CRowType6:public CRow
  {
public:
   CCellButton       Button;
   //+---------------------------------------------------------------+
   // ����������� ������
   void              CRowType6();
   virtual     // �����: ���������� ������
   void              Draw(string m_name,
                          int m_xdelta,
                          int m_ydelta,
                          int m_bsize,
                          int m_b1size,
                          int m_b2size,
                          string m_button1,
                          string m_button2,
                          string m_button3);
   virtual     // ����� ��������� ������� OnChartEvent
   void              OnEvent(const int id,
                             const long &lparam,
                             const double &dparam,
                             const string &sparam);
  };
//+------------------------------------------------------------------+
//| ����������� ������ CRowType6                                     |
//+------------------------------------------------------------------+
void CRowType6::CRowType6()
  {
   on_event=false;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
//| ����� ��������� ������� OnChartEvent ������ CRowType6            |
//+------------------------------------------------------------------+
void CRowType6::OnEvent(const int id,
                        const long &lparam,
                        const double &dparam,
                        const string &sparam)
  {
   if(on_event) // ��������� ������� ���������
     {
      Button.OnEvent(id,lparam,dparam,sparam);
     }
  }
//+------------------------------------------------------------------+
//| ����� Draw ������ CRowType6                                      |
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
   on_event=true;   // ��������� ��������� �������
  }
//+------------------------------------------------------------------+
