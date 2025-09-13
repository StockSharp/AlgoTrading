//+------------------------------------------------------------------+
//|                                                 SpecialChart.mqh |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"

#include <Canvas\Canvas.mqh>
#include <Arrays\ArrayDouble.mqh>

//+------------------------------------------------------------------+
//| Special class for drawing several balance charts                 |
//+------------------------------------------------------------------+
class CSpecialChart: public CCanvas
  {
private:
   color             m_bkgroundcolor;  // background color
   color             m_framecolor;     // frame color
   color             m_linecolor;      // line color
   int               m_framewidth;     // frame width in pixels
   int               m_linewidth;      // line width in pixels
   int               m_lines;          // number of lines on the chart
   CArrayDouble      m_seria[];        // arrays for storing the chart values
   bool              m_profitseria[];  // profitable or unprofitable series
   int               m_lastseria_index;// fresh line index on the chart
   color             m_profit;         // profitable series color
   color             m_loss;           // loss-making series color
public:
   //--- constructor/destructor
                     CSpecialChart():m_lastseria_index(0),m_profit(clrGreen),m_loss(clrRed){};
                    ~CSpecialChart() { CCanvas::Destroy(); }
   //--- background, border and line color
   void              SetBackground(const color clr){ m_bkgroundcolor=clr;      };
   void              SetFrameColor(const color clr){ m_framecolor=clr;         };
   void              SetLineColor(const color clr) { m_linecolor=clr;          };
   //--- border and line width
   void              SetFrameWidth(const int w)    { m_framewidth=w;           };
   void              SetLineWidth(const int w)     { m_linewidth=w;            };
   //--- setting the number of lines on the chart
   void              SetLines(const int l) { m_lines=l;ArrayResize(m_seria,l); ArrayResize(m_profitseria,l);};
   //--- updating the object on the screen
   void              Update();
   //--- adding data from the array
   void              AddSeria(const double  &array[],bool profit);
   //--- drawing the chart 
   void              Draw(const int seria_index,color clr);
   //---             switches color representation from color to uint type
   uint              uCLR(const color clr)          { return(XRGB((clr)&0x0FF,(clr)>>8,(clr)>>16));};
   //--- drawing the line using conventional terms
   void              Line(int x1,int y1,int x2,int y2,uint col);
   //--- getting max. and min. values in the series 
   double            MaxValue(const int seria_index);
   double            MinValue(const int seria_index);
  };
//+------------------------------------------------------------------+
//|  Updating the chart                                              |
//+------------------------------------------------------------------+
void CSpecialChart::Update(void)
  {
//--- filling the background
   CCanvas::Erase(CSpecialChart::uCLR(m_bkgroundcolor));
//--- drawing the border
   CCanvas::FillRect(m_framewidth,m_framewidth,
                     m_width-m_framewidth-1,
                     m_height-m_framewidth-1,
                     CSpecialChart::uCLR(m_framecolor));

//--- drawing each series up to 80% from the available square vertically and horizontally
   for(int i=0;i<m_lines;i++)
     {
      color clr=m_loss;
      if(m_profitseria[i]) clr=m_profit;
      Draw(i,clr);
      //Print(__FUNCSIG__," - Drawing: ",i);
     }
//--- updating the chart
   CCanvas::Update();
  }
//+------------------------------------------------------------------+
//|  Adding the new data series for drawing on the chart             |
//+------------------------------------------------------------------+
void CSpecialChart::AddSeria(const double &array[],bool profit)
  {
//PrintFormat("Adding the array to the series number %d",m_lastseria_index);
   m_seria[m_lastseria_index].Resize(0);
   m_seria[m_lastseria_index].AddArray(array);
   m_profitseria[m_lastseria_index]=profit;
//--- tracking the index of the last line (not used currently)  
   m_lastseria_index++;
   if(m_lastseria_index>=m_lines) m_lastseria_index=0;
  }
//+------------------------------------------------------------------+
//|  Getting the highest value                                       |
//+------------------------------------------------------------------+
double CSpecialChart::MaxValue(const int seria_index)
  {
   double res=m_seria[seria_index].At(0);
   int total=m_seria[seria_index].Total();
//--- sorting and comparing
   for(int i=1;i<total;i++)
     {
      if(m_seria[seria_index].At(i)>res) res=m_seria[seria_index].At(i);
     }
//--- result
   return res;
  }
//+------------------------------------------------------------------+
//|  Getting the lowest value                                        |
//+------------------------------------------------------------------+
double CSpecialChart::MinValue(const int seria_index)
  {
   double res=m_seria[seria_index].At(0);;
   int total=m_seria[seria_index].Total();
//--- sorting and comparing  
   for(int i=1;i<total;i++)
     {
      if(m_seria[seria_index].At(i)<res) res=m_seria[seria_index].At(i);
     }
//--- result
   return res;
  }
//+------------------------------------------------------------------+
//| Basic drawing function overload                                  |
//+------------------------------------------------------------------+
void CSpecialChart::Line(int x1,int y1,int x2,int y2,uint col)
  {
//--- as Y axis is turned upside down, we need to prepare y1 and y2
   int y1_adj=m_height-y1;
   int y2_adj=m_height-y2;
//--- submitting the color in CSimpleCanvas color format instead of the natural color
   CCanvas::Line(x1,y1_adj,x2,y2_adj,CSpecialChart::uCLR(col));
//---
  }
//+------------------------------------------------------------------+
//|  Drawing all lines on the chart                                  |
//+------------------------------------------------------------------+
void CSpecialChart::Draw(const int seria_index,color clr)
  {
//--- preparing ratios for converting the values into pixels
   double min=MaxValue(seria_index);
   double max=MinValue(seria_index);
   double size=m_seria[seria_index].Total();
//--- indentation from the chart edge
   double x_indent=m_width*0.1;
   double y_indent=m_height*0.1;
//--- calculating ratios
   double k_y=(max-min)/(m_height-2*m_framewidth-2*y_indent);
   double k_x=(size)/(m_width-2*m_framewidth-2*x_indent);
//--- constant ones
   double start_x=x_indent;
   double start_y=m_height-y_indent;
//--- now, drawing the broken line passing through all dots in the series
   for(int i=1;i<size;i++)
     {
      //--- converting the values into pixels
      int x1=(int)((i-0)/k_x+start_x);  // laying the value number on the horizontal
      int y1=(int)(start_y-(m_seria[seria_index].At(i)-min)/k_y);    // on the vertical
      int x2=(int)((i-1-0)/k_x+start_x);// laying the value number on the horizontal
      int y2=(int)(start_y-(m_seria[seria_index].At(i-1)-min)/k_y);  // on the vertical
      //--- deriving the line from the previous dot to the current one
      Line(x1,y1,x2,y2,clr);
     }
//--- drawing and updating
   CCanvas::Update();
  }
//+------------------------------------------------------------------+
