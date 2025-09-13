//+------------------------------------------------------------------+
//|                                             OBJ_EDIT_Example.mq4 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2013, MetaQuotes Software Corp."
#property link        "http://www.mql5.com"
#property version     "1.00"
#property description "The Expert Advisor creates and controls the OBJ_EDIT object"
#property strict
//+------------------------------------------------------------------+
//| ���� ������                                                      |
//+------------------------------------------------------------------+
enum ENUM_BUTTON_TYPE
  {
   ALIGN_BUTTON=1,
   CORNER_BUTTON=2,
   COORD_BUTTON=3
  };
//--- ������� ��������� 
input string           InpName="OBJ_Edit_1";        // ��� �������
input int              InpX=150;                    // ���������� �� ��� X
input int              InpY=250;                    // ���������� �� ��� Y
input string           InpText="Text";              // ����� �������
input string           InpFont="Arial";             // �����
input int              InpFontSize=14;              // ������ ������
input ENUM_ALIGN_MODE  InpAlign=ALIGN_CENTER;       // ������ ������������ ������
input bool             InpReadOnly=false;           // ����������� �������������
input ENUM_BASE_CORNER InpCorner=CORNER_LEFT_UPPER; // ���� ������� ��� ��������
input color            InpColor=clrNavy;            // ���� ������
input color            InpBackColor=clrIvory;       // ���� ����
input color            InpBorderColor=clrOrangeRed; // ���� �������
input bool             InpBack=false;               // ������ �� ������ �����
input bool             InpSelection=false;          // �������� ��� �����������
input bool             InpHidden=false;             // ����� � ������ ��������
input long             InpZOrder=0;                 // ��������� �� ������� �����
//---
#include <ChartObjects\ChartObjectsTxtControls.mqh>
//--- ������ ReadOnly
CChartObjectButton ExtReadOnlyButton;
//--- ������ Selectable
CChartObjectButton ExtSelectableButton;
//--- ������ ���������� �������� ������������
CChartObjectButton ExtAlignLButton;
CChartObjectButton ExtAlignCButton;
CChartObjectButton ExtAlignRButton;
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
//--- �������������� ����
CChartObjectEdit ExtXCoordinateInfo;
CChartObjectEdit ExtYCoordinateInfo;
CChartObjectEdit ExtCornerInfo;
CChartObjectEdit ExtAlignInfo;
CChartObjectEdit ExtReadOnlyInfo;
CChartObjectEdit ExtSelectableInfo;
//---
bool ExtInitialized=false;
long ExtChartWidth=0;
long ExtChartHeight=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
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
//--- �������� ������ OBJ_EDIT �� �������      
   if(!EditCreate(0,InpName,0,InpX,InpY,250,20,InpText,InpFont,InpFontSize,InpAlign,InpReadOnly,
      InpCorner,InpColor,InpBackColor,InpBorderColor,InpBack,InpSelection,InpHidden,InpZOrder))
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
//--- ������� ������ OBJ_EDIT � �������
   ObjectDelete(0,InpName);
//--- ������� �������������� ����
   ObjectDelete(0,"edt_xcoord");
   ObjectDelete(0,"edt_ycoord");
   ObjectDelete(0,"edt_corner");
   ObjectDelete(0,"edt_align");
   ObjectDelete(0,"edt_read_only");
   ObjectDelete(0,"edt_selectable");
//--- ������� ������
   ObjectDelete(0,"btn_read_only");
   ObjectDelete(0,"btn_selectable");
//--- ������� ������ ���������� ����� ��������
   ObjectDelete(0,"btn_corner_left_upper");
   ObjectDelete(0,"btn_corner_left_lower");
   ObjectDelete(0,"btn_corner_right_upper");
   ObjectDelete(0,"btn_corner_right_lower");
//--- ������� ������ ���������� �������������
   ObjectDelete(0,"btn_align_left");
   ObjectDelete(0,"btn_align_center");
   ObjectDelete(0,"btn_align_right");
//--- ������� ������ ���������� ������������
   ObjectDelete(0,"btn_dec_y");
   ObjectDelete(0,"btn_inc_y");
   ObjectDelete(0,"btn_inc_x");
   ObjectDelete(0,"btn_dec_x");
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

      //--- ������ ���������� ��������� readonly
      if(sparam=="btn_read_only")
        {
         bool read_only=(bool)ObjectGetInteger(0,InpName,OBJPROP_READONLY);
         read_only=!read_only;
         res=ObjectSetInteger(0,InpName,OBJPROP_READONLY,read_only);
        }
      //--- ������ ���������� ��������� selectable
      if(sparam=="btn_selectable")
        {
         bool selectable=(bool)ObjectGetInteger(0,InpName,OBJPROP_SELECTABLE);
         selectable=!selectable;
         res=ObjectSetInteger(0,InpName,OBJPROP_SELECTABLE,selectable);
        }

      //--- ������ ���������� ��������� ALIGN ������� Edit
      if(sparam=="btn_align_left")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ALIGN,ALIGN_LEFT);
        }
      if(sparam=="btn_align_center")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ALIGN,ALIGN_CENTER);
        }
      if(sparam=="btn_align_right")
        {
         res=ObjectSetInteger(0,InpName,OBJPROP_ALIGN,ALIGN_RIGHT);
        }

      //--- ������ ���������� ������������ ������� Edit
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

      //--- ������ ���������� ����� ������� ��� �������� (OBJPROP_CORNER)
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
         ShowInfo(0,InpName);
         ChartRedraw();
        }
     }
  }
//+------------------------------------------------------------------+
//| ������� ������ "���� �����"                                      |
//+------------------------------------------------------------------+
bool EditCreate(const long             chart_ID=0,               // ID �������
                const string           name="Edit",              // ��� �������
                const int              sub_window=0,             // ����� �������
                const int              x=0,                      // ���������� �� ��� X
                const int              y=0,                      // ���������� �� ��� Y
                const int              width=50,                 // ������
                const int              height=18,                // ������
                const string           text="Text",              // �����
                const string           font="Arial",             // �����
                const int              font_size=10,             // ������ ������
                const ENUM_ALIGN_MODE  align=ALIGN_CENTER,       // ������ ������������
                const bool             read_only=false,          // ����������� �������������
                const ENUM_BASE_CORNER corner=CORNER_LEFT_UPPER, // ���� ������� ��� ��������
                const color            clr=clrBlack,             // ���� ������
                const color            back_clr=clrWhite,        // ���� ����
                const color            border_clr=clrNONE,       // ���� �������
                const bool             back=false,               // �� ������ �����
                const bool             selection=false,          // �������� ��� �����������
                const bool             hidden=true,              // ����� � ������ ��������
                const long             z_order=0)                // ��������� �� ������� �����
  {
//--- ������� �������� ������
   ResetLastError();
//--- �������� ���� �����
   if(!ObjectCreate(chart_ID,name,OBJ_EDIT,sub_window,0,0))
     {
      Print(__FUNCTION__,
            ": �� ������� ������� ������ \"���� �����\"! ��� ������ = ",GetLastError());
      return(false);
     }
//--- ��������� ���������� �������
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
//--- ��������� ������� �������
   ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height);
//--- ��������� �����
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
//--- ��������� ����� ������
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
//--- ��������� ������ ������
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
//--- ��������� ������ ������������ ������ � �������
   ObjectSetInteger(chart_ID,name,OBJPROP_ALIGN,align);
//--- ��������� (true) ��� ������� (false) ����� ������ ��� ������
   ObjectSetInteger(chart_ID,name,OBJPROP_READONLY,read_only);
//--- ��������� ���� �������, ������������ �������� ����� ������������ ���������� �������
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
//--- ��������� ���� ������
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
//--- ��������� ���� ����
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr);
//--- ��������� ���� �������
   ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_COLOR,border_clr);
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
   CreateEdit("corner",ExtCornerInfo,x0+8,y0+54,140,20);
   CreateEdit("align",ExtAlignInfo,x0+8,y0+74,140,20);

   CreateEdit("read_only",ExtReadOnlyInfo,x0+8+140,y0+54,100,20);
   CreateEdit("selectable",ExtSelectableInfo,x0+8+140,y0+74,100,20);

   CreateButton("read_only",ExtReadOnlyButton,x0+250,y0+55,80,20);
   CreateButton("selectable",ExtSelectableButton,x0+250,y0+75,80,20);
//---
   CreateButton("corner_left_upper",ExtCornerLUButton,x0+10,y0+100,150,20);
   CreateButton("corner_left_lower",ExtCornerLLButton,x0+10,y0+123,150,20);
   CreateButton("corner_right_upper",ExtCornerRUButton,x0+163,y0+100,150,20);
   CreateButton("corner_right_lower",ExtCornerRLButton,x0+163,y0+123,150,20);
//---   
   CreateButton("align_left",ExtAlignLButton,x0+10,y0+150,150,20);
   CreateButton("align_center",ExtAlignCButton,x0+163,y0+150,150,20);
   CreateButton("align_right",ExtAlignRButton,+x0+16+2*150,y0+150,150,20);
//---
   CreateButton("dec_y",ExtCoordDecYButton,x0+413,y0+36,25,25);
   CreateButton("inc_y",ExtCoordIncYButton,x0+413,y0+92,25,25);
   CreateButton("inc_x",ExtCoordIncXButton,x0+441,y0+64,25,25);
   CreateButton("dec_x",ExtCoordDecXButton,x0+385,y0+64,25,25);
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
//---
   ExtCoordIncXButton.Description(CharToString(240));
   ExtCoordDecXButton.Description(CharToString(239));
   ExtCoordIncYButton.Description(CharToString(242));
   ExtCoordDecYButton.Description(CharToString(241));
//--- ���������� ������� �������� � ������ InpName
   ShowInfo(0,InpName);
  }
//+------------------------------------------------------------------+
//| ���������� ���������� � ����������� � ��������� ��������         |
//+------------------------------------------------------------------+
void ShowInfo(const long chart_ID,const string name)
  {
   int current_corner=(int)ObjectGetInteger(chart_ID,name,OBJPROP_CORNER);
   int current_align=(int)ObjectGetInteger(chart_ID,name,OBJPROP_ALIGN);
   bool read_only=(bool)ObjectGetInteger(chart_ID,name,OBJPROP_READONLY);
   bool selectable=(bool)ObjectGetInteger(chart_ID,name,OBJPROP_SELECTABLE);
//---
   string sel="selectable=";
   if(selectable) sel+="true"; else sel+="false";
   ExtSelectableButton.State(selectable);
   ExtSelectableInfo.Description(sel);
//---
   string ro="read_only=";
   if(read_only) ro+="true"; else ro+="false";
   ExtReadOnlyButton.State(read_only);
   ExtReadOnlyInfo.Description(ro);
//---
   switch(current_corner)
     {
      case CORNER_LEFT_UPPER:  ButtonPressed(ExtCornerLUButton,CORNER_BUTTON); break;
      case CORNER_LEFT_LOWER:  ButtonPressed(ExtCornerLLButton,CORNER_BUTTON); break;
      case CORNER_RIGHT_LOWER: ButtonPressed(ExtCornerRLButton,CORNER_BUTTON); break;
      case CORNER_RIGHT_UPPER: ButtonPressed(ExtCornerRUButton,CORNER_BUTTON); break;
     }
//---
   switch(current_align)
     {
      case ALIGN_LEFT:        ButtonPressed(ExtAlignLButton,ALIGN_BUTTON); break;
      case ALIGN_CENTER:      ButtonPressed(ExtAlignCButton,ALIGN_BUTTON); break;
      case ALIGN_RIGHT:       ButtonPressed(ExtAlignRButton,ALIGN_BUTTON); break;
     }
//---
   int x=(int)ObjectGetInteger(chart_ID,name,OBJPROP_XDISTANCE);
   int y=(int)ObjectGetInteger(chart_ID,name,OBJPROP_YDISTANCE);
//---
   ExtXCoordinateInfo.Description(IntegerToString(x));
   ExtYCoordinateInfo.Description(IntegerToString(y));
//---
   ExtCornerInfo.Description(EnumToString(ENUM_BASE_CORNER(current_corner)));
   ExtAlignInfo.Description(EnumToString(ENUM_ALIGN_MODE(current_align)));
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
      case ALIGN_BUTTON:
        {
         UnSelectButton(ExtAlignLButton);
         UnSelectButton(ExtAlignCButton);
         UnSelectButton(ExtAlignRButton);
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
