//+------------------------------------------------------------------+
//|                                                       CChart.mqh |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

#include "ClassSymbolButton.mqh"
#include "ClassChart.mqh"
//+------------------------------------------------------------------+
//| class for implementation of Symbols table                        |
//+------------------------------------------------------------------+
class CTradePad
  {
private:
   int               m_rows;                    // number of strings in the Symbols table
   int               m_columns;                 // number of columns in the Symbols table
   int               m_button_width;            // width of the cell containing a symbol
   int               m_button_height;           // height of the cell containing a symbol
   int               m_top;                     // X coordinate of the upper left corner
   int               m_left;                    // Y coordinate of the upper left corner
   int               m_left_previous_header;    // previous X value for the upper left corner of the header
   int               m_top_previos_header;      // previous Y value for the upper left corner of the header
   color             m_button_text_color;       // unified text color for the buttons
   color             m_button_bg_color;         // unified background color for the buttons
   string            m_prefix;                  // unified prefix for the created CSymbolButton type objects
   string            m_chart_name;              // Chart Object name
   int               m_top_chart;               // Y coordinate of the table's upper left corner
   int               m_left_chart;              // X coordinate of the table's upper left corner
   int               m_footer_width;            // footer width
   CSymbolButton     m_symbol_set[];            // array with the objects for managing Symbol buttons
   string            m_header;                  // name of the line for dragging the object
   int               m_top_buy_button;          // Y coordinate of the upper left corner of BUY button
   int               m_left_buy_button;         // X coordinate of the upper left corner of BUY
   int               m_top_sell_button;         // Y coordinate of the upper left corner of SELL button
   int               m_left_sell_button;        //  X coordinate of the upper left corner of SELL button
   int               m_top_lots_edit;           // Y coordinate of the upper left corner of the field for specifying the volume
   int               m_left_lots_edit;          // X coordinate of the upper left corner of the field for specifying the volume
   int               m_width_lots_edit;         // volume field width
   string            m_buy_button;              // BUY button name
   string            m_sell_button;             // SELL button name
   string            m_lots_edit;               // volume field name
   string            m_chart_symbol_name;       // name of the Symbol displaying Chart object
   ENUM_TIMEFRAMES   m_current_tf;              // period of the Symbol displaying Chart object
   color             m_up_color;
   color             m_down_color;
   color             m_flat_color;
   color             m_blank_color;
   CChart            tradeChart;                // chart object
public:
   void              CTradePad();                // constructor
   bool              CreateTradePad(int cols,int rows,int Xleft,int Ytop,int width,int height,color u,color d,color f,color b);
   int               DeleteTradePad();
   string            GetChartName(){return(m_chart_name);};
   int               GetSymbolButtons(){return(ArraySize(m_symbol_set));};
   void              SetButtons(string symbolName);
   void              MoveTradePad(int x_shift,int y_shift);
   void              GetShiftTradePad(int &x_shift,int &y_shift);
   string            GetHeaderName(){return(m_header);};
   void              SetButtonColors();
   void              SetTrendColors(color u,color d,color f,color b){m_up_color=u;m_down_color=d;m_flat_color=f; m_blank_color=b;};
   void              EmptyFunction();
  };
//+------------------------------------------------------------------+
//|  default constructor (always without parameters)                 |
//+------------------------------------------------------------------+
void CTradePad::CTradePad()
  {
   m_button_bg_color=Green;
   m_button_text_color=White;
   m_prefix="cell_";
   m_header="TablePad";
   m_chart_name="test";
   m_buy_button="BuyButton";
   m_sell_button="SellButton";
   m_lots_edit="InputVolume";
  }
//+------------------------------------------------------------------+
//|  creating Symbols Table (actually, it is a constructor)          |
//+------------------------------------------------------------------+
bool CTradePad::CreateTradePad(int cols,int rows,int Xleft,int Ytop,int width,int height,
                               color u,color d,color f,color b)
  {
   bool res=false;
//---
   SetTrendColors(u,d,f,b);
   m_left=Xleft;
   m_top=Ytop;
   int Yord,Xord;
//--- how many buttons will we have?
   ArrayResize(m_symbol_set,cols*rows);
   int j=0,tradeSymbols=SymbolsTotal(false);
   string symb[];
   ArrayResize(symb,cols*rows);

//--- draw the header
   m_top_previos_header=m_top-20;
   m_left_previous_header=m_left;
   if(ObjectFind(0,m_header)<0) ObjectCreate(0,m_header,OBJ_BUTTON,0,0,0,0,0);
   ObjectSetInteger(0,m_header,OBJPROP_COLOR,White);
   ObjectSetInteger(0,m_header,OBJPROP_BGCOLOR,Blue);
   ObjectSetInteger(0,m_header,OBJPROP_XDISTANCE,m_left_previous_header);
   ObjectSetInteger(0,m_header,OBJPROP_YDISTANCE,m_top_previos_header);
   ObjectSetInteger(0,m_header,OBJPROP_XSIZE,cols*width);
   ObjectSetInteger(0,m_header,OBJPROP_YSIZE,20);
   ObjectSetString(0,m_header,OBJPROP_TEXT,"Trade Pad ");
   ObjectSetString(0,m_header,OBJPROP_FONT,"Tahoma");
   ObjectSetInteger(0,m_header,OBJPROP_SELECTABLE,true);

//--- draw the buttons
   for(int c=0;c<cols;c++)
     {
      Xord=m_left+c*width;         // Symbol button's X ordinate
      for(int r=0;r<rows;r++)
        {
         Yord=m_top+r*height;     // Symbol button's Y ordinate
         string name;
         if(j>=tradeSymbols) j=0;
         name=SymbolName(j,false);
         j++;
         symb[c*rows+r]=name;
         SymbolSelect(name,true);
         double v;
         color col;
         col=GetColorOfSymbol(name,PERIOD_CURRENT,
                              m_up_color,m_down_color,
                              m_flat_color,m_blank_color,v);
         m_symbol_set[c*rows+r].CreateSymbolButton(Yord,
                                                   Xord,
                                                   height,
                                                   width,
                                                   name,
                                                   m_button_text_color,
                                                   col);
         //Sleep(10);// pause in order to see the buttons' generation
        }
     }
//--- click the first button
   ObjectSetInteger(0,symb[0],OBJPROP_STATE,true);

//--- create Chart object
   double chartheight=252;
   m_current_tf=ChartPeriod(0);
//   if(ObjectFind(0,m_chart_name)<0)ObjectCreate(0,m_chart_name,OBJ_CHART,0,0,0,0,0);

   m_top_chart=m_top+rows*height;
   m_left_chart=m_left;
/*
   ObjectSetInteger(0,m_chart_name,OBJPROP_XDISTANCE,m_left_chart);
   ObjectSetInteger(0,m_chart_name,OBJPROP_YDISTANCE,m_top_chart);
*/
   m_footer_width=cols*width;
/*
   ObjectSetInteger(0,m_chart_name,OBJPROP_XSIZE,m_footer_width);
   chartheight=cols*width/4.0*3.0;
   ObjectSetInteger(0,m_chart_name,OBJPROP_YSIZE,(int)chartheight);
   ObjectSetInteger(0,m_chart_name,OBJPROP_SELECTABLE,0);
   ObjectSetString(0,m_chart_name,OBJPROP_SYMBOL,symb[0]);

*/
   chartheight=252;
   tradeChart.CreateChart(m_left_chart,m_top_chart,m_footer_width,int(chartheight),symb[0],"testChart");

//--- create the footer
//--- SELL button
   if(ObjectFind(0,m_sell_button)<0) ObjectCreate(0,m_sell_button,OBJ_BUTTON,0,0,0,0,0);
   ObjectSetInteger(0,m_sell_button,OBJPROP_COLOR,White);
   ObjectSetInteger(0,m_sell_button,OBJPROP_BGCOLOR,OrangeRed);
   m_top_sell_button=int(m_top_chart+chartheight);
   m_left_sell_button=m_left_chart;
   ObjectSetInteger(0,m_sell_button,OBJPROP_XDISTANCE,m_left_sell_button);
   ObjectSetInteger(0,m_sell_button,OBJPROP_YDISTANCE,m_top_sell_button);
   ObjectSetInteger(0,m_sell_button,OBJPROP_XSIZE,100);
   ObjectSetInteger(0,m_sell_button,OBJPROP_YSIZE,40);
   ObjectSetString(0,m_sell_button,OBJPROP_FONT,"Tahoma");
   ObjectSetInteger(0,m_sell_button,OBJPROP_FONTSIZE,15);
   ObjectSetString(0,m_sell_button,OBJPROP_TEXT,"SELL");
   ObjectSetInteger(0,m_sell_button,OBJPROP_SELECTABLE,false);

//--- BUY button
   if(ObjectFind(0,m_buy_button)<0) ObjectCreate(0,m_buy_button,OBJ_BUTTON,0,0,0,0,0);
   ObjectSetInteger(0,m_buy_button,OBJPROP_COLOR,White);
   ObjectSetInteger(0,m_buy_button,OBJPROP_BGCOLOR,Blue);
   m_top_buy_button=m_top_sell_button;
   m_left_buy_button=m_left_chart+m_footer_width-100;
   ObjectSetInteger(0,m_buy_button,OBJPROP_XDISTANCE,m_left_buy_button);
   ObjectSetInteger(0,m_buy_button,OBJPROP_YDISTANCE,m_top_buy_button);
   ObjectSetInteger(0,m_buy_button,OBJPROP_XSIZE,100);
   ObjectSetInteger(0,m_buy_button,OBJPROP_YSIZE,40);
   ObjectSetString(0,m_buy_button,OBJPROP_FONT,"Tahoma");
   ObjectSetInteger(0,m_buy_button,OBJPROP_FONTSIZE,15);
   ObjectSetString(0,m_buy_button,OBJPROP_TEXT,"BUY");
   ObjectSetInteger(0,m_buy_button,OBJPROP_SELECTABLE,false);

//--- Volume field
   if(ObjectFind(0,m_lots_edit)<0) ObjectCreate(0,m_lots_edit,OBJ_EDIT,0,0,0,0,0);
   ObjectSetInteger(0,m_lots_edit,OBJPROP_COLOR,White);
   ObjectSetInteger(0,m_lots_edit,OBJPROP_BGCOLOR,DarkOliveGreen);
   m_top_lots_edit=m_top_sell_button;
   m_left_lots_edit=m_left_chart+100;
   m_width_lots_edit=m_footer_width-200;
   ObjectSetInteger(0,m_lots_edit,OBJPROP_XDISTANCE,m_left_lots_edit);
   ObjectSetInteger(0,m_lots_edit,OBJPROP_YDISTANCE,m_top_lots_edit);
   ObjectSetInteger(0,m_lots_edit,OBJPROP_XSIZE,m_width_lots_edit);
   ObjectSetInteger(0,m_lots_edit,OBJPROP_YSIZE,40);
   ObjectSetString(0,m_lots_edit,OBJPROP_FONT,"Tahoma");
   ObjectSetInteger(0,m_lots_edit,OBJPROP_FONTSIZE,10);
   ObjectSetString(0,m_lots_edit,OBJPROP_TEXT,"   0.1");
   ObjectSetInteger(0,m_lots_edit,OBJPROP_SELECTABLE,false);

//---  command for drawing all the changes
   ChartRedraw(0);

//--- wait a bit and try to draw the buttons' background once again
   Sleep(300);
   SetButtonColors();
   ChartRedraw(0);

//---
   return(res);
  }
//+------------------------------------------------------------------+
//|  set background color                                            |
//+------------------------------------------------------------------+
void CTradePad::SetButtonColors()
  {
   int i,buttons=GetSymbolButtons();
   double v;// indicator values are received here

   struct IndicatorValue
     {
      string            symbol;
      ENUM_TIMEFRAMES   timeframe;
      double            value;
     };

   IndicatorValue state[];
   ArrayResize(state,buttons);
//Print("SetButtonColors");
//---
   string out="";
   for(i=0;i<buttons;i++)
     {
      state[i].symbol=m_symbol_set[i].GetSymbolName();
      state[i].timeframe=m_current_tf;
      color c; // button color is received here      
      c=GetColorOfSymbol(m_symbol_set[i].GetSymbolName(),
                         m_current_tf,m_up_color,m_down_color,
                         m_flat_color,m_blank_color,v);
      state[i].value=v;
      StringAdd(out,DoubleToString(v,2));
      StringAdd(out,", ");
      m_symbol_set[i].SetBGColor(c);
     }
   ChartRedraw();

//Print(out);
//---
  }
//+------------------------------------------------------------------+
//| Delete all Symbols Table objects                                 |
//+------------------------------------------------------------------+
int CTradePad::DeleteTradePad()
  {
   int deleted=0;
   int size=ArraySize(m_symbol_set);
//---
   tradeChart.DeleteChart();
   for(int i=0;i<size;i++)
     {
      m_symbol_set[i].DeleteSymbolButton();
      deleted++;
     }
   ObjectDelete(0,m_header);
   ObjectDelete(0,m_buy_button);
   ObjectDelete(0,m_sell_button);
   ObjectDelete(0,m_lots_edit);
//---
   return(deleted);
  }
//+------------------------------------------------------------------+
//|  unpress the button and set the Symbol on the Chart              |
//+------------------------------------------------------------------+
void CTradePad::SetButtons(string name)
  {
   int handle=ObjectFind(0,name);

//--- if BUY button is pressed
   if(name==m_buy_button)
     {
      //--- unpress the button back
      Sleep(200);
      ObjectSetInteger(0,name,OBJPROP_STATE,false);
      return;
     }
//--- if SELL button is pressed
   if(name==m_sell_button)
     {
      //--- unpress the button back
      Sleep(200);
      ObjectSetInteger(0,name,OBJPROP_STATE,false);
      return;
     }

//--- handle pressing CChart class controls
   if(tradeChart.IsChartControlEvent(name))
     {
      //Print("Handling pressing CChart class control");
      tradeChart.DoChartOperations(name);
      m_current_tf=tradeChart.GetChartTimeframe();
      SetButtonColors();
      ChartRedraw(0);
      return;
     }

//Print("Handling pressing the button named ", name);
   if(handle>=0)
     {
      int size=GetSymbolButtons();
      for(int i=0;i<size;i++)
        {
         if(m_symbol_set[i].m_button_name==name)
           {
            bool selected=ObjectGetInteger(0,name,OBJPROP_STATE);
            //Print("Button "+name+" pressed=",selected);
            if(!selected)
               ObjectSetInteger(0,m_symbol_set[i].m_button_name,OBJPROP_STATE,false);
           }
         else
           {
            ObjectSetInteger(0,m_symbol_set[i].m_button_name,OBJPROP_STATE,false);
           }

        }
     }
   tradeChart.SetSymbolForChart(name);
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
//| move all Symbols Table elements                                  |
//+------------------------------------------------------------------+
void CTradePad::MoveTradePad(int x_shift,int y_shift)
  {
   int buttons=GetSymbolButtons();
//--- shift the buttons
   for(int i=0;i<buttons;i++)
     {
      m_symbol_set[i].MoveButton(x_shift,y_shift);
     }
//--- shift Chart
   tradeChart.MoveChart(x_shift,y_shift);
/*
   m_left_chart+=x_shift;
   m_top_chart+=y_shift;
   ObjectSetInteger(0,m_chart_name,OBJPROP_XDISTANCE,m_left_chart);
   ObjectSetInteger(0,m_chart_name,OBJPROP_YDISTANCE,m_top_chart);
*/
//--- shift BUY button
   m_left_buy_button+=x_shift;
   m_top_buy_button+=y_shift;
   ObjectSetInteger(0,m_buy_button,OBJPROP_XDISTANCE,m_left_buy_button);
   ObjectSetInteger(0,m_buy_button,OBJPROP_YDISTANCE,m_top_buy_button);
//--- shift SELL button
   m_left_sell_button+=x_shift;
   m_top_sell_button+=y_shift;
   ObjectSetInteger(0,m_sell_button,OBJPROP_XDISTANCE,m_left_sell_button);
   ObjectSetInteger(0,m_sell_button,OBJPROP_YDISTANCE,m_top_sell_button);
//--- shift InputVolume entry field
   m_left_lots_edit+=x_shift;
   m_top_lots_edit+=y_shift;
   ObjectSetInteger(0,m_lots_edit,OBJPROP_XDISTANCE,m_left_lots_edit);
   ObjectSetInteger(0,m_lots_edit,OBJPROP_YDISTANCE,m_top_lots_edit);
//--- drawing command
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CTradePad::GetShiftTradePad(int &x_shift,int &y_shift)
  {
//--- calculate shift values
   int dx=x_shift-m_left_previous_header;
   int dy=y_shift-m_top_previos_header;
//--- save new coordinates
   m_left_previous_header=x_shift;
   m_top_previos_header=y_shift;
//--- return found shifts
   x_shift=dx;
   y_shift=dy;
  }
//+------------------------------------------------------------------+
//| get the color depending on the trend direction                   |
//+------------------------------------------------------------------+
color GetColorOfSymbol(string symbol,
                       ENUM_TIMEFRAMES period,
                       color up,
                       color dn,
                       color flat,
                       color empty,
                       double &value)
  {
   color trend=flat;
//---
   int stochastic=iStochastic(symbol,period,5,3,3,MODE_SMA,STO_LOWHIGH);
   double values [];
   if(CopyBuffer(stochastic,1,0,1,values)<=0) return(empty);
   value=values[0];
   if(values[0]>80) trend=up;
   if(values[0]<20) trend=dn;
   return(trend);
//---
  }
//+------------------------------------------------------------------+
