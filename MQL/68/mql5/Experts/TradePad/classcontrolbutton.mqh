//+------------------------------------------------------------------+
//|                                               CControlButton.mqh |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//+------------------------------------------------------------------+
//| control object                                                   |
//+------------------------------------------------------------------+
class CControlButton
  {
private:
   string            m_control_name;       // control's unique name
   int               m_top;                // Y coordinate of the upper-left corner
   int               m_left;               // X coordinate of the upper-left corner
   int               m_width;              // control width 
   int               m_heigt;              // control height
   string            m_text;               // control text
   string            m_font;               // text font
   int               m_text_size;          // control text size
   color             m_text_color;         // text color
   color             m_bg_color;           // background color
public:
   void              CControlButton(){m_top=0; m_left=0;m_text_size=10;m_font="Arial";m_bg_color=Blue;};
   void              CreateButton(int l,int t,int w,int h,string button_name,string button_text);
   void              MoveControlButton(int shiftX,int shiftY);
   void              SetTextDetails(int text_size,string font_name,color text_color);
   void              SetTextForControl(string text);
   void              SetWidthAndHeight(int w,int h);
   void              SetBGColor(color bg_color);
   void              DeleteControl();
   string            GetControlName(){return(m_control_name);};
  };
//+------------------------------------------------------------------+
//| create the control                                               |
//+------------------------------------------------------------------+
void CControlButton::CreateButton(int l,int t,int w,int h,
                                  string button_name,string button_text)
  {
   m_top=0;
   m_left=0;
   m_control_name=button_name;
   //Print("CreateButton function is creating control named ",button_name);
   if(ObjectFind(0,m_control_name)<0) ObjectCreate(0,m_control_name,OBJ_BUTTON,0,0,0,0,0);
   SetWidthAndHeight(w,h);
   MoveControlButton(l,t);
   ObjectSetInteger(0,m_control_name,OBJPROP_SELECTABLE,false);

  }
//+------------------------------------------------------------------+
//|  delete the control                                              |
//+------------------------------------------------------------------+
void CControlButton::DeleteControl()
  {
   if(ObjectFind(0,m_control_name)>=0)
     {
      if(!ObjectDelete(0,m_control_name))
        {
         Print("Failed to delete the object named ",m_control_name,"! Error #",GetLastError());
        }
     }
  }
//+------------------------------------------------------------------+
//|  move control along the axes by the specified values             |
//+------------------------------------------------------------------+
void CControlButton::MoveControlButton(int shiftX,int shiftY)
  {
   m_top+=shiftY;
   m_left+=shiftX;
   ObjectSetInteger(0,m_control_name,OBJPROP_XDISTANCE,m_left);
   ObjectSetInteger(0,m_control_name,OBJPROP_YDISTANCE,m_top);
  }
//+------------------------------------------------------------------+
//| set control's height and width                                   |
//+------------------------------------------------------------------+
void CControlButton::SetWidthAndHeight(int w,int h)
  {
   m_width=w;
   m_heigt=h;
   ObjectSetInteger(0,m_control_name,OBJPROP_XSIZE,m_width);
   ObjectSetInteger(0,m_control_name,OBJPROP_YSIZE,m_heigt);
  }
//+------------------------------------------------------------------+
//|  set control text attributes                                     |
//+------------------------------------------------------------------+
void CControlButton::SetTextDetails(int text_size,
                                    string font_name,color text_color)
  {
   m_text_size=text_size;
   m_font=font_name;
   m_text_color=text_color;
   //Print("Start setting attributes for the button named ",m_control_name);
   ObjectSetInteger(0,m_control_name,OBJPROP_COLOR,text_color);
   //Print("Button text color is set ",m_control_name);
   ObjectSetString(0,m_control_name,OBJPROP_FONT,font_name);
   //Print("Button text font is set ",m_control_name);
   ObjectSetInteger(0,m_control_name,OBJPROP_FONTSIZE,m_text_size);
   //Print("Button text font size is set ",m_control_name);
  }
//+------------------------------------------------------------------+
//|  set control text                                                |
//+------------------------------------------------------------------+
void CControlButton::SetTextForControl(string text)
  {
   m_text=text;
   //Print("Set control text:",text);
   ObjectSetString(0,m_control_name,OBJPROP_TEXT,m_text);
  }
//+------------------------------------------------------------------+
//| set control background color                                     |
//+------------------------------------------------------------------+
void CControlButton::SetBGColor(color bg_color)
  {
   m_bg_color=bg_color;
   ObjectSetInteger(0,m_control_name,OBJPROP_BGCOLOR,m_bg_color);
  }
//+------------------------------------------------------------------+
