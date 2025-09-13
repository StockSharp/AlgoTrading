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
//| Типы кнопок                                                      |
//+------------------------------------------------------------------+
enum ENUM_BUTTON_TYPE
  {
   ANCHOR_BUTTON=1,
   CORNER_BUTTON=2,
   COORD_BUTTON=3
  };
//+------------------------------------------------------------------+
//| Структура для описания поворота                                  |
//+------------------------------------------------------------------+
struct Degrees
  {
   double            degrees;     // угол поворота
   uchar             symbol_code; // код символа (часы в Wingdings)
  };
//---
input string            InpName="OBJ_Label_1";     // Имя метки
input int               InpX=150;                  // Расстояние по оси X
input int               InpY=250;                  // Расстояние по оси Y
input string            InpFont="Arial";           // Шрифт
input int               InpFontSize=20;            // Размер шрифта
input color             InpColor=clrLightSeaGreen; // Цвет
input double            InpAngle=0.0;              // Угол наклона в градусах
input ENUM_ANCHOR_POINT InpAnchor=ANCHOR_CENTER;   // Способ привязки
input bool              InpBack=false;             // Объект на заднем плане
input bool              InpSelection=true;         // Выделить для перемещений
input bool              InpHidden=true;            // Скрыт в списке объектов
input long              InpZOrder=0;               // Приоритет на нажатие мышью
//---
#include <ChartObjects\ChartObjectsTxtControls.mqh>
//--- кнопки управления способом привязки
CChartObjectButton ExtAnchorLUButton;
CChartObjectButton ExtAnchorLButton;
CChartObjectButton ExtAnchorLLButton;
CChartObjectButton ExtAnchorUCButton;
CChartObjectButton ExtAnchorCCButton;
CChartObjectButton ExtAnchorLCButton;
CChartObjectButton ExtAnchorRUButton;
CChartObjectButton ExtAnchorRButton;
CChartObjectButton ExtAnchorRLButton;
//--- кнопки управления углом привязки
CChartObjectButton ExtCornerLUButton;
CChartObjectButton ExtCornerLLButton;
CChartObjectButton ExtCornerRUButton;
CChartObjectButton ExtCornerRLButton;
//--- кнопки +- для координат x и y
CChartObjectButton ExtCoordIncXButton;
CChartObjectButton ExtCoordDecXButton;
CChartObjectButton ExtCoordIncYButton;
CChartObjectButton ExtCoordDecYButton;
CChartObjectButton ExtCoordIncAngleButton;
//--- информационные поля
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
//--- установка параметров вращения и соответствуюших углам часов шрифта Wingdings
   for(int i=0; i<12; i++)
     {
      ExtAngleParameters[i].degrees=i*(360/12);
      if(i<3) ExtAngleParameters[i].symbol_code=uchar(185-i);
      else
         ExtAngleParameters[i].symbol_code=uchar(197-i);
     }
//--- определим размеры окна
   if(!ChartGetInteger(0,CHART_WIDTH_IN_PIXELS,0,ExtChartWidth))
     {
      Print("Не удалось получить ширину графика! Код ошибки = ",GetLastError());
      return(INIT_FAILED);
     }
   if(!ChartGetInteger(0,CHART_HEIGHT_IN_PIXELS,0,ExtChartHeight))
     {
      Print("Не удалось получить высоту графика! Код ошибки = ",GetLastError());
      return(INIT_FAILED);
     }
//--- создадим текстовую метку на графике
   if(!LabelCreate(0,InpName,0,InpX,InpY,CORNER_LEFT_UPPER,"Simple text",InpFont,InpFontSize,
      InpColor,InpAngle,ANCHOR_CENTER,InpBack,InpSelection,InpHidden,InpZOrder))
     {
      return(INIT_FAILED);
     }
//--- подготавливаем кнопки
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
//--- удаляем метку с графика
   ObjectDelete(0,InpName);
//--- удаляем информационные поля
   ObjectDelete(0,"edt_xcoord");
   ObjectDelete(0,"edt_ycoord");
   ObjectDelete(0,"edt_corner");
   ObjectDelete(0,"edt_anchor");
   ObjectDelete(0,"edt_angle");
//--- удаляем кнопки управления способом привязки
   ObjectDelete(0,"btn_anchor_left_upper");
   ObjectDelete(0,"btn_anchor_left");
   ObjectDelete(0,"btn_anchor_left_lower");
   ObjectDelete(0,"btn_anchor_upper");
   ObjectDelete(0,"btn_anchor_center");
   ObjectDelete(0,"btn_anchor_lower");
   ObjectDelete(0,"btn_anchor_right_upper");
   ObjectDelete(0,"btn_anchor_right");
   ObjectDelete(0,"btn_anchor_right_lower");
//--- удаляем кнопки управления углом привязки
   ObjectDelete(0,"btn_corner_left_upper");
   ObjectDelete(0,"btn_corner_left_lower");
   ObjectDelete(0,"btn_corner_right_upper");
   ObjectDelete(0,"btn_corner_right_lower");
//--- удаляем кнопки управления координатами
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
//--- способ приращения координат при заданном угле привязки
   switch(current_corner)
     {
      case CORNER_LEFT_LOWER:  {inv_coord_corner_mode_y=true; break;}
      case CORNER_RIGHT_UPPER: {inv_coord_corner_mode_x=true; break;}
      case CORNER_RIGHT_LOWER: {inv_coord_corner_mode_x=true; 
                                inv_coord_corner_mode_y=true; break;}
     }
//--- сбросим значение ошибки
   ResetLastError();
//--- проверка события нажатия на кнопку мыши
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      Comment("CHARTEVENT_CLICK: "+sparam);

      //--- кнопка управления углом объекта Label
      if(sparam=="btn_inc_angle")
        {
         ButtonPressed(ExtCoordIncAngleButton,COORD_BUTTON);
         //--- увеличиваем индекс (угол поворота)
         ExtCurrentAngleIndex++;
         if(ExtCurrentAngleIndex>ArraySize(ExtAngleParameters)-1) ExtCurrentAngleIndex=0;
         //--- устанавливаем угол поворота
         double angle=ExtAngleParameters[ExtCurrentAngleIndex].degrees;
         ExtCoordIncAngleButton.Description(CharToString(ExtAngleParameters[ExtCurrentAngleIndex].symbol_code));
         //---
         res=ObjectSetDouble(0,InpName,OBJPROP_ANGLE,angle);
         UnSelectButton(ExtCoordIncAngleButton);
        }
      //--- кнопки управления координатами объекта Label
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
      //--- кнопки управления способом привязки метки (OBJPROP_ANCHOR)
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
      //--- кнопки управления углом графика для привязки метки (OBJPROP_CORNER)
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
         Print("Не удалось изменить параметр! Код ошибки = ",GetLastError());
      else
        {
         ShowLabelInfo(0,InpName);
         ChartRedraw();
        }
     }
  }
//+------------------------------------------------------------------+
//| Создает текстовую метку                                          |
//+------------------------------------------------------------------+
bool LabelCreate(const long              chart_ID=0,               // ID графика
                 const string            name="Label",             // имя метки
                 const int               sub_window=0,             // номер подокна
                 const int               x=0,                      // координата по оси X
                 const int               y=0,                      // координата по оси Y
                 const ENUM_BASE_CORNER  corner=CORNER_LEFT_UPPER, // угол графика для привязки
                 const string            text="Label",             // текст
                 const string            font="Arial",             // шрифт
                 const int               font_size=10,             // размер шрифта
                 const color             clr=clrRed,               // цвет
                 const double            angle=0.0,                // наклон текста
                 const ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER, // способ привязки
                 const bool              back=false,               // на заднем плане
                 const bool              selection=false,          // выделить для перемещений
                 const bool              hidden=true,              // скрыт в списке объектов
                 const long              z_order=0)                // приоритет на нажатие мышью
  {
//--- сбросим значение ошибки
   ResetLastError();
//--- создадим текстовую метку
   if(!ObjectCreate(chart_ID,name,OBJ_LABEL,sub_window,0,0))
     {
      Print(__FUNCTION__,": не удалось создать текстовую метку! Код ошибки = ",GetLastError());
      return(false);
     }
//--- установим координаты метки
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
//--- установим угол графика, относительно которого будут определяться координаты точки
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
//--- установим текст
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
//--- установим шрифт текста
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
//--- установим размер шрифта
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
//--- установим угол наклона текста
   ObjectSetDouble(chart_ID,name,OBJPROP_ANGLE,angle);
//--- установим способ привязки
   ObjectSetInteger(chart_ID,name,OBJPROP_ANCHOR,anchor);
//--- установим цвет
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
//--- отобразим на переднем (false) или заднем (true) плане
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
//--- включим (true) или отключим (false) режим перемещения метки мышью
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
//--- скроем (true) или отобразим (false) имя графического объекта в списке объектов
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
//--- установим приоритет на получение события нажатия мыши на графике
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
//--- успешное выполнение
   return(true);
  }
//+------------------------------------------------------------------+
//| Создает кнопки                                                   |
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
//--- показываем текущие свойства метки с именем InpName
   ShowLabelInfo(0,InpName);
  }
//+------------------------------------------------------------------+
//| Показывает информацию о координатах и свойствах привязки метки   |
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
//| Cоздает кнопку (объект типа CChartObjectButton)                  |
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
//| Cоздает объект типа CChartObjectEdit                             |
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
