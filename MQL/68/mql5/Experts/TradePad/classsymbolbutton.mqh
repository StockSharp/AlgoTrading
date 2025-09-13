//+------------------------------------------------------------------+
//|                                                CSymbolButton.mqh |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

//+------------------------------------------------------------------+
//|  Class for creating a symbol button                              |
//+------------------------------------------------------------------+
class CSymbolButton
  {
private:
   double            m_top;            // Y coordinate of the upper-left corner
   double            m_left;           // X coordinate of the upper-left corner
   double            m_height;         // button height
   double            m_width;          // button width
   color             m_txt_col;        // text color
   color             m_bg_col;         // background color
   int               m_ind_handle;     // pointer to the indicator for the button
   string            m_symbol_name;    // name of the Symbol, for which the button is created

public:
   string            m_button_name;    // button's unique name
   bool              CreateSymbolButton(double top,double left,double height,double width,
                                        string buttonID,color TextColor,color BGColor);// constructor
   bool              DeleteSymbolButton();
   void              MoveButton(int x_shift,int y_shift);
   color             GetBGColor(){return(m_bg_col);};
   void              SetBGColor(color bg_color);
   string            GetSymbolName(){return(m_symbol_name);};
   void              SetSymbolName(string s){m_symbol_name=s;};
  };
//+------------------------------------------------------------------+
//| delete Symbol button                                             |
//+------------------------------------------------------------------+
bool CSymbolButton::DeleteSymbolButton()
  {
   if(ObjectFind(0,m_button_name)>=0)
     {
      //Print("Delete the cell with the name ",m_button_name);
      if(!ObjectDelete(0,m_button_name))
        {
         Print("Failed to delete the object named ",m_button_name,"! Error #",GetLastError());
        }
      else
        {
         //ChartRedraw(0);
        }
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| setup object of Class CSymbolButton                              |
//+------------------------------------------------------------------+
bool CSymbolButton::CreateSymbolButton(double top,double left,double height,double width,
                                       string buttonID,color TextColor,color BGColor)
  {
   bool res=false;
//---
   if(ObjectFind(0,buttonID)<0)
     {
      m_top=top;
      m_left=left;
      ObjectCreate(ChartID(),buttonID,OBJ_BUTTON,0,0,0,0,0);
      ObjectSetInteger(0,buttonID,OBJPROP_COLOR,TextColor);
      ObjectSetInteger(0,buttonID,OBJPROP_BGCOLOR,BGColor);
      ObjectSetInteger(0,buttonID,OBJPROP_XDISTANCE,int(m_left));
      ObjectSetInteger(0,buttonID,OBJPROP_YDISTANCE,int(m_top));
      ObjectSetInteger(0,buttonID,OBJPROP_XSIZE,int(width));
      ObjectSetInteger(0,buttonID,OBJPROP_YSIZE,int(height));
      ObjectSetString(0,buttonID,OBJPROP_FONT,"Arial");
      ObjectSetString(0,buttonID,OBJPROP_TEXT,buttonID);
      ObjectSetInteger(0,buttonID,OBJPROP_FONTSIZE,10);
      ObjectSetInteger(0,buttonID,OBJPROP_SELECTABLE,0);
      m_button_name=buttonID;
      SetSymbolName(buttonID);
      //ChartRedraw(ChartID());
     }
//  else
//    {
//    }
//---
   return(res);
  }
//+------------------------------------------------------------------+
//| set Symbol button background color                               |
//+------------------------------------------------------------------+
void CSymbolButton::SetBGColor(color bg_color)
  {
   ObjectSetInteger(0,m_button_name,OBJPROP_BGCOLOR,bg_color);
  }
//+------------------------------------------------------------------+
//| shift the symbol button by the specified value                   |
//+------------------------------------------------------------------+
void CSymbolButton::MoveButton(int x_shift,int y_shift)
  {
   m_top+=y_shift;
   m_left+=x_shift;
   ObjectSetInteger(0,m_button_name,OBJPROP_XDISTANCE,int(m_left));
   ObjectSetInteger(0,m_button_name,OBJPROP_YDISTANCE,int(m_top));
//ChartRedraw(0);
  }
