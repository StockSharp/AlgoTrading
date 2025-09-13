//+------------------------------------------------------------------+
//|                                            OBJ_LABEL_Example.mq4 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2013, MetaQuotes Software Corp."
#property link        "http://www.mql5.com"
#property version     "1.00"
#property description "The Expert Advisor creates and controls the Label object"
#property strict
//+------------------------------------------------------------------+
//| ���� ������                                                      |
//+------------------------------------------------------------------+
enum ENUM_BUTTON_TYPE
  {
   ANCHOR_BUTTON=1,
   CORNER_BUTTON=2,
   COORD_BUTTON=3
  };
//+------------------------------------------------------------------+
//| ��������� ��� �������� ��������                                  |
//+------------------------------------------------------------------+
struct Degrees
  {
   double            degrees;     // ���� ��������
   uchar             symbol_code; // ��� ������� (���� � Wingdings)
  };
//---
input string            InpName="OBJ_Label_1";     // ��� �����
input int               InpX=150;                  // ���������� �� ��� X
input int               InpY=250;                  // ���������� �� ��� Y
input string            InpFont="Arial";           // �����
input int               InpFontSize=20;            // ������ ������
input color             InpColor=clrLightSeaGreen; // ����
input double            InpAngle=0.0;              // ���� ������� � ��������
input ENUM_ANCHOR_POINT InpAnchor=ANCHOR_CENTER;   // ������ ��������
input bool              InpBack=false;             // ������ �� ������ �����
input bool              InpSelection=true;         // �������� ��� �����������
input bool              InpHidden=true;            // ����� � ������ ��������
input long              InpZOrder=0;               // ��������� �� ������� �����
//---
#include <ChartObjects\ChartObjectsTxtControls.mqh>
//--- ������ ���������� �������� ��������
CChartObjectButton ExtAnchorLUButton;
CChartObjectButton ExtAnchorLButton;
CChartObjectButton ExtAnchorLLButton;
CChartObjectButton ExtAnchorUCButton;
CChartObjectButton ExtAnchorCCButton;
CChartObjectButton ExtAnchorLCButton;
CChartObjectButton ExtAnchorRUButton;
CChartObjectButton ExtAnchorRButton;
CChartObjectButton ExtAnchorRLButton;
//--- ������ ���������� ����� ��������
CChartObjectButton ExtCornerLUButton;
CChartObjectButton ExtCornerLLButton;
CChartObjectButton ExtCornerRUButton;
CChartObjectButton ExtCornerRLButton;
//--- ������ +- ��� ��������� x � y
CChartObjectButton ExtCoordIncXButton;
CChartObjectButton ExtCoordDecXButton;
CChartObjectButton ExtCoordIncYButton;
CChartObjectButton ExtCoordDecYButton;
CChartObjectButton ExtCoordIncAngleButton;
//--- �������������� ����
CChartObjectEdit ExtXCoordinateInfo;
CChartObjectEdit ExtYCoordinateInfo;
CChartObjectEdit ExtAngleInfo;
CChartObjectEdit ExtCornerInfo;
CChartObjectEdit ExtAnchorInfo;
//---
bool ExtInitialized=false;
long ExtChartWidth=0;
long ExtChartHeight=0;
int ExtCurrentAngleIndex=0;
Degrees ExtAngleParameters[12];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ��������� ���������� �������� � ��������������� ����� ����� ������ Wingdings
   for(int i=0; i<12; i++)
     {
      ExtAngleParameters[i].degrees=i*(360/12);
      if(i<3) ExtAngleParameters[i].symbol_code=uchar(185-i);
      else
         ExtAngleParameters[i].symbol_code=uchar(197-i);
     }
//--- ��������� ������� ����
   if(!ChartGetInteger(0,CHART_WIDTH_IN_PIXELS,0,ExtChartWidth))
     {
      Print("�� ������� �������� ������ �������! ��� ������ = ",GetLastError());
      return(INIT_FAILED);
     }
   if(!ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS,0,ExtChartHeight))
     {
      Print("�� ������� �������� ������ �������! ��� ������ = ",GetLastError());
      return(INIT_FAILED);
     }
//--- �������� ��������� ����� �� �������
   if(!LabelCreate(0,InpName,0,InpX,InpY,CORNER_LEFT_UPPER,"Simple text",InpFont,InpFontSize,
      InpColor,InpAngle,ANCHOR_CENTER,InpBack,InpSelection,InpHidden,InpZOrder))
     {
      return(INIT_FAILED);
     }
//--- �������������� ������
   PrepareButtons();
   ExtInitialized=true;
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- ������� ����� � �������
   ObjectDelete(0,InpName);
//--- ������� �������������� ����
   ObjectDelete(0,"edt_xcoord");
   ObjectDelete(0,"edt_ycoord");
   ObjectDelete(0,"edt_corner");
   ObjectDelete(0,"edt_anchor");
   ObjectDelete(0,"edt_angle");
//--- ������� ������ ���������� �������� ��������
   ObjectDelete(0,"btn_anchor_left_upper");
   ObjectDelete(0,"btn_anchor_left");
   ObjectDelete(0,"btn_anchor_left_lower");
   ObjectDelete(0,"btn_anchor_upper");
   ObjectDelete(0,"btn_anchor_center");
   ObjectDelete(0,"btn_anchor_lower");
   ObjectDelete(0,"btn_anchor_right_upper");
   ObjectDelete(0,"btn_anchor_right");
   ObjectDelete(0,"btn_anchor_right_lower");
//--- ������� ������ ���������� ����� ��������
   ObjectDelete(0,"btn_corner_left_upper");
   ObjectDelete(0,"btn_corner_left_lower");
   ObjectDelete(0,"btn_corner_right_upper");
   ObjectDelete(0,"btn_corner_right_lower");
//--- ������� ������ ���������� ������������
   ObjectDelete(0,"btn_dec_y");
   ObjectDelete(0,"btn_inc_y");
   ObjectDelete(0,"btn_inc_x");
   ObjectDelete(0,"btn_dec_x");
   ObjectDelete(0,"btn_inc_angle");
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   int x=0,y=0;
   bool res=true;
//---
   if(!ExtInitialized)
      return;
//---
   int current_corner=(int)ObjectGetInteger(0,InpName,OBJPROP_CORNER);
   bool inv_coord_corner_mode_x=false;
   bool inv_coord_corner_mode_y=false;
//--- ������ ���������� ��������� ��� �������� ���� ��������
   switch(current_corner)
     {
      case CORNER_LEFT_LOWER:  {inv_coord_corner_mode_y=true; break;}
      case CORNER_RIGHT_UPPER: {inv_coord_corner_mode_x=true; break;}
      case CORNER_RIGHT_LOWER: {inv_coord_corner_mode_x=true; 
                                inv_coord_corner_mode_y=true; break;}
     }
//--- ������� �������� ������
   ResetLastError();
//--- �������� ������� ������� �� ������ ����
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      Comment("CHARTEVENT_CLICK: "+sparam);

      //--- ������ ���������� ����� ������� Label
      if(sparam=="btn_inc_angle")
        {
         ButtonPressed(ExtCoordIncAngleButton,COORD_BUTTON);
         //--- ����������� ������ (���� ��������)
         ExtCurrentAngleIndex++;
         if(ExtCurrentAngleIndex>ArraySize(ExtAngleParameters)-1) ExtCurrentAngleIndex=0;
         //--- ������������� ���� ��������
         double angle=ExtAngleParameters[ExtCurrentAngleIndex].degrees;
         ExtCoordIncAngleButton.Description(CharToString(ExtAngleParameters[ExtCurrentAngleIndex].symbol_code));
         //---
         res=ObjectSetDouble(0,InpName,OBJPROP_ANGLE,angle);
         UnSelectButton(ExtCoordIncAngleButton);
        }
      //--- ������ ���������� ������������ ������� Label
      if(sparam=="btn_inc_x")
        {
         ButtonPressed(ExtCoordIncXButton,COORD_BUTTON);
         x=(int)ObjectGetInteger(0,InpName,OBJPROP_XDISTANCE);
         if(!inv_coord_corner_mode_x) x+=20; else x-=20;
         if(x>ExtChartWidth) x=(int)ExtChartWidth;
         if(x<0) x=0;
         res=ObjectSetInteger(0,InpName,OBJPROP_XDISTANCE,x);
         UnSelectButton(ExtCoordIncXButton);
        }
      if(sparam=="btn_dec_x")
        {
         ButtonPressed(ExtCoordDecXButton,COORD_BUTTON);
         x=(int)ObjectGetInteger(0,InpName,OBJPROP_XDISTANCE);
         if(!inv_coord_corner_mode_x) x-=20; else x+=20;
         if(x<0) x=0;
         res=ObjectSetInteger(0,InpName,OBJPROP_XDISTANCE,x);
         UnSelectButton(ExtCoordDecXButton);
        }
      if(sparam=="btn_inc_y")
        {
         ButtonPressed(ExtCoordIncYButton,COORD_BUTTON);
         y=(int)ObjectGetInteger(0,InpName,OBJPROP_YDISTANCE);
         if(!inv_coord_corner_mode_y) y+=20; else y-=20;
         if(y<0) y=0;
         if(y>ExtChartHeight) y=(int)ExtChartHeight;
         res=ObjectSetInteger(0,InpName,OBJPROP_YDISTANCE,y);
         UnSelectButton(ExtCoordIncYButton);
        }
      if(sparam=="btn_dec_y")
        {
         ButtonPressed(ExtCoordDecYButton,COORD_BUTTON);
         y=(int)ObjectGetInteger(0,InpName,OBJPROP_YDISTANCE);
         //         y-=20;
         if(!inv_coord_corner_mode_y) y-=20; else y+=20;
         if(y<0) y=0;
         if(y>ExtChartHeight) y=(int)ExtChartHeight;
         res=ObjectSetInteger(0,InpName,OBJPROP_YDISTANCE,y);
         UnSelectButton(ExtCoordDecYButton);
        }
      //--- ������ ���������� �������� �������� ����� (OBJPROP_ANCHOR)
      if(sparam=="btn_anchor_left_upper")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_LEFT_UPPER);
        }
      if(sparam=="btn_anchor_left")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_LEFT);
        }
      if(sparam=="btn_anchor_left_lower")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_LEFT_LOWER);
        }
      if(sparam=="btn_anchor_upper")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_UPPER);
        }
      if(sparam=="btn_anchor_center")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_CENTER);
        }
      if(sparam=="btn_anchor_lower")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_LOWER);
        }
      if(sparam=="btn_anchor_right_upper")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_RIGHT_UPPER);
        }
      if(sparam=="btn_anchor_right")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_RIGHT);
        }
      if(sparam=="btn_anchor_right_lower")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ANCHOR,ANCHOR_RIGHT_LOWER);
        }
      //--- ������ ���������� ����� ������� ��� �������� ����� (OBJPROP_CORNER)
      if(sparam=="btn_corner_left_upper")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_CORNER,CORNER_LEFT_UPPER);
        }
      if(sparam=="btn_corner_left_lower")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_CORNER,CORNER_LEFT_LOWER);
        }
      if(sparam=="btn_corner_right_upper")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_CORNER,CORNER_RIGHT_UPPER);
        }
      if(sparam=="btn_corner_right_lower")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_CORNER,CORNER_RIGHT_LOWER);
        }
      if(!res)
         Print("�� ������� �������� ��������! ��� ������ = ",GetLastError());
      else
        {
         ShowLabelInfo(0,InpName);
         ChartRedraw();
        }
     }
  }
//+------------------------------------------------------------------+
//| ������� ��������� �����                                          |
//+------------------------------------------------------------------+
bool LabelCreate(const long              chart_ID=0,               // ID �������
                 const string            name="Label",             // ��� �����
                 const int               sub_window=0,             // ����� �������
                 const int               x=0,                      // ���������� �� ��� X
                 const int               y=0,                      // ���������� �� ��� Y
                 const ENUM_BASE_CORNER  corner=CORNER_LEFT_UPPER, // ���� ������� ��� ��������
                 const string            text="Label",             // �����
                 const string            font="Arial",             // �����
                 const int               font_size=10,             // ������ ������
                 const color             clr=clrRed,               // ����
                 const double            angle=0.0,                // ������ ������
                 const ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER, // ������ ��������
                 const bool              back=false,               // �� ������ �����
                 const bool              selection=false,          // �������� ��� �����������
                 const bool              hidden=true,              // ����� � ������ ��������
                 const long              z_order=0)                // ��������� �� ������� �����
  {
//--- ������� �������� ������
   ResetLastError();
//--- �������� ��������� �����
   if(!ObjectCreate(chart_ID,name,OBJ_LABEL,sub_window,0,0))
     {
      Print(__FUNCTION__,": �� ������� ������� ��������� �����! ��� ������ = ",GetLastError());
      return(false);
     }
//--- ��������� ���������� �����
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
//--- ��������� ���� �������, ������������ �������� ����� ������������ ���������� �����
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
//--- ��������� �����
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
//--- ��������� ����� ������
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
//--- ��������� ������ ������
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
//--- ��������� ���� ������� ������
   ObjectSetDouble(chart_ID,name,OBJPROP_ANGLE,angle);
//--- ��������� ������ ��������
   ObjectSetInteger(chart_ID,name,OBJPROP_ANCHOR,anchor);
//--- ��������� ����
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
//--- ��������� �� �������� (false) ��� ������ (true) �����
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
//--- ������� (true) ��� �������� (false) ����� ����������� ����� �����
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
//--- ������ (true) ��� ��������� (false) ��� ������������ ������� � ������ ��������
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
//--- ��������� ��������� �� ��������� ������� ������� ���� �� �������
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
//--- �������� ����������
   return(true);
  }
//+------------------------------------------------------------------+
//| ������� ������                                                   |
//+------------------------------------------------------------------+
void PrepareButtons()
  {
//---
   int x0=0;
   int y0=0;
//---
   CreateEdit("xcoord",ExtXCoordinateInfo,x0+8,y0+34,70,20);
   CreateEdit("ycoord",ExtYCoordinateInfo,x0+78,y0+34,70,20);
   CreateEdit("corner",ExtCornerInfo,x0+8,y0+54,200,20);
   CreateEdit("anchor",ExtAnchorInfo,x0+8,y0+74,200,20);
   CreateEdit("angle",ExtAngleInfo,x0+148,y0+34,60,20);
//---
   CreateButton("anchor_left_upper",ExtAnchorLUButton,x0+10,y0+150,150,20);
   CreateButton("anchor_left",ExtAnchorLButton,x0+10,y0+173,150,20);
   CreateButton("anchor_left_lower",ExtAnchorLLButton,x0+10,y0+196,150,20);
//---
   CreateButton("anchor_upper",ExtAnchorUCButton,x0+163,y0+150,150,20);
   CreateButton("anchor_center",ExtAnchorCCButton,x0+163,y0+173,150,20);
   CreateButton("anchor_lower",ExtAnchorLCButton,x0+163,y0+196,150,20);
//---
   CreateButton("anchor_right_upper",ExtAnchorRUButton,+x0+16+2*150,y0+150,150,20);
   CreateButton("anchor_right",ExtAnchorRButton,+x0+316,y0+173,150,20);
   CreateButton("anchor_right_lower",ExtAnchorRLButton,+x0+316,y0+196,150,20);
//---
   CreateButton("corner_left_upper",ExtCornerLUButton,x0+10,y0+100,150,20);
   CreateButton("corner_left_lower",ExtCornerLLButton,x0+10,y0+123,150,20);
   CreateButton("corner_right_upper",ExtCornerRUButton,x0+163,y0+100,150,20);
   CreateButton("corner_right_lower",ExtCornerRLButton,x0+163,y0+123,150,20);
//---
   CreateButton("dec_y",ExtCoordDecYButton,x0+413,y0+36,25,25);
   CreateButton("inc_y",ExtCoordIncYButton,x0+413,y0+92,25,25);
   CreateButton("inc_x",ExtCoordIncXButton,x0+441,y0+64,25,25);
   CreateButton("dec_x",ExtCoordDecXButton,x0+385,y0+64,25,25);
   CreateButton("inc_angle",ExtCoordIncAngleButton,x0+413,y0+64,25,25);
//---
   ExtCoordIncXButton.FontSize(15);
   ExtCoordDecXButton.FontSize(15);
   ExtCoordIncYButton.FontSize(15);
   ExtCoordDecYButton.FontSize(15);
//---
   ExtCoordIncXButton.Font("Wingdings");
   ExtCoordDecXButton.Font("Wingdings");
   ExtCoordIncYButton.Font("Wingdings");
   ExtCoordDecYButton.Font("Wingdings");
   ExtCoordIncAngleButton.Font("Wingdings");
   ExtCoordIncAngleButton.FontSize(20);
//---
   ExtCoordIncXButton.Description(CharToString(240));
   ExtCoordDecXButton.Description(CharToString(239));
   ExtCoordIncYButton.Description(CharToString(242));
   ExtCoordDecYButton.Description(CharToString(241));
//---
   ExtCoordIncAngleButton.Description(CharToString(ExtAngleParameters[ExtCurrentAngleIndex].symbol_code));
//--- ���������� ������� �������� ����� � ������ InpName
   ShowLabelInfo(0,InpName);
  }
//+------------------------------------------------------------------+
//| ���������� ���������� � ����������� � ��������� �������� �����   |
//+------------------------------------------------------------------+
void ShowLabelInfo(const long chart_ID,const string name)
  {
//---
   int current_corner=(int)ObjectGetInteger(chart_ID,name,OBJPROP_CORNER);
   int current_anchor=(int)ObjectGetInteger(chart_ID,name,OBJPROP_ANCHOR);
//---
   switch(current_corner)
     {
      case CORNER_LEFT_UPPER:  ButtonPressed(ExtCornerLUButton,CORNER_BUTTON); break;
      case CORNER_LEFT_LOWER:  ButtonPressed(ExtCornerLLButton,CORNER_BUTTON); break;
      case CORNER_RIGHT_LOWER: ButtonPressed(ExtCornerRLButton,CORNER_BUTTON); break;
      case CORNER_RIGHT_UPPER: ButtonPressed(ExtCornerRUButton,CORNER_BUTTON); break;
     }
//---
   switch(current_anchor)
     {
      case ANCHOR_LEFT_UPPER:  ButtonPressed(ExtAnchorLUButton,ANCHOR_BUTTON); break;
      case ANCHOR_LEFT:        ButtonPressed(ExtAnchorLButton,ANCHOR_BUTTON);  break;
      case ANCHOR_LEFT_LOWER:  ButtonPressed(ExtAnchorLLButton,ANCHOR_BUTTON); break;
      case ANCHOR_UPPER:       ButtonPressed(ExtAnchorUCButton,ANCHOR_BUTTON); break;
      case ANCHOR_CENTER:      ButtonPressed(ExtAnchorCCButton,ANCHOR_BUTTON); break;
      case ANCHOR_LOWER:       ButtonPressed(ExtAnchorLCButton,ANCHOR_BUTTON); break;
      case ANCHOR_RIGHT_UPPER: ButtonPressed(ExtAnchorRUButton,ANCHOR_BUTTON); break;
      case ANCHOR_RIGHT:       ButtonPressed(ExtAnchorRButton,ANCHOR_BUTTON);  break;
      case ANCHOR_RIGHT_LOWER: ButtonPressed(ExtAnchorRLButton,ANCHOR_BUTTON); break;
     }
//---
   int x=(int)ObjectGetInteger(chart_ID,name,OBJPROP_XDISTANCE);
   int y=(int)ObjectGetInteger(chart_ID,name,OBJPROP_YDISTANCE);
   double angle=ObjectGetDouble(chart_ID,name,OBJPROP_ANGLE);
//---
   ExtXCoordinateInfo.Description(IntegerToString(x));
   ExtYCoordinateInfo.Description(IntegerToString(y));
   ExtAngleInfo.Description(DoubleToString(angle,2));
//---
   ExtCornerInfo.Description(EnumToString(ENUM_BASE_CORNER(current_corner)));
   ExtAnchorInfo.Description(EnumToString(ENUM_ANCHOR_POINT(current_anchor)));
  }
//+------------------------------------------------------------------+
//| UnSelectButton                                                   |
//+------------------------------------------------------------------+
void UnSelectButton(CChartObjectButton &btn)
  {
   btn.State(false);
   btn.BackColor(clrAliceBlue);
  }
//+------------------------------------------------------------------+
//| C������ ������ (������ ���� CChartObjectButton)                  |
//+------------------------------------------------------------------+
bool CreateButton(string text,CChartObjectButton &btn,int x0,int y0,int width,int height)
  {
   if(!btn.Create(0,"btn_"+text,0,x0,y0,width,height))
      return(false);
   btn.Font("Verdana");
   btn.FontSize(7);
   StringToUpper(text);
   btn.Description(text);
   btn.State(false);
   UnSelectButton(btn);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| C������ ������ ���� CChartObjectEdit                             |
//+------------------------------------------------------------------+
bool CreateEdit(string name,CChartObjectEdit &edit,int x0,int y0,int width,int height)
  {
   if(!edit.Create(0,"edt_"+name,0,x0,y0,width,height))
      return(false);
   edit.Font("Verdana");
   edit.FontSize(7);
   edit.BackColor(clrIvory);
   edit.Description("");
   edit.ReadOnly(true);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| UnselectButtons                                                  |
//+------------------------------------------------------------------+
void UnselectButtons(ENUM_BUTTON_TYPE buttontype)
  {
   switch(buttontype)
     {
      case ANCHOR_BUTTON:
        {
         UnSelectButton(ExtAnchorLUButton);
         UnSelectButton(ExtAnchorLButton);
         UnSelectButton(ExtAnchorLLButton);
         //---
         UnSelectButton(ExtAnchorUCButton);
         UnSelectButton(ExtAnchorCCButton);
         UnSelectButton(ExtAnchorLCButton);
         //---
         UnSelectButton(ExtAnchorRUButton);
         UnSelectButton(ExtAnchorRButton);
         UnSelectButton(ExtAnchorRLButton);
         break;
        }
      case CORNER_BUTTON:
        {
         UnSelectButton(ExtCornerLUButton);
         UnSelectButton(ExtCornerLLButton);
         UnSelectButton(ExtCornerRUButton);
         UnSelectButton(ExtCornerRLButton);
         break;
        }
      case COORD_BUTTON:
        {
         UnSelectButton(ExtCoordIncXButton);
         UnSelectButton(ExtCoordDecXButton);
         UnSelectButton(ExtCoordIncYButton);
         UnSelectButton(ExtCoordDecYButton);
         UnSelectButton(ExtCoordIncAngleButton);
         break;
        }
     }
  }
//+------------------------------------------------------------------+
//| ButtonPressed                                                    |
//+------------------------------------------------------------------+
bool ButtonPressed(CChartObjectButton &btn,ENUM_BUTTON_TYPE buttontype)
  {
   UnselectButtons(buttontype);
//---
   bool state=!btn.State();
   btn.State(state);
   if(state)
      btn.BackColor(clrHoneydew);
   else
      btn.BackColor(clrAliceBlue);
//---     
   return(true);
  }
//+------------------------------------------------------------------+
