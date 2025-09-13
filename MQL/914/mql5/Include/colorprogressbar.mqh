//+------------------------------------------------------------------+
//|                                             ColorProgressBar.mqh |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

#include <Canvas\Canvas.mqh>
//+------------------------------------------------------------------+
//|  The progress bar class using two colors                         |
//+------------------------------------------------------------------+
class CColorProgressBar :public CCanvas
  {
private:
   color             m_goodcolor,m_badcolor;    // "good" and "bad" colors
   color             m_backcolor,m_bordercolor; // background and border colors
   int               m_x;                       // X coordinate of the upper-left corner 
   int               m_y;                       // Y coordinate of the upper-left corner 
   int               m_width;                   // width
   int               m_height;                  // height
   int               m_borderwidth;             // border width
   bool              m_passes[];                // number of handled passes
   int               m_lastindex;               // last pass index
public:
   //--- constructor/destructor
                     CColorProgressBar();
                    ~CColorProgressBar(){ CCanvas::Destroy(); };
   //--- initializing
   bool              Create(const string name,int x,int y,int width,int height,ENUM_COLOR_FORMAT clrfmt);
   //--- resetting the counter to zero
   void              Reset(void) { m_lastindex=0;     };
   //--- background, border and line color
   void              BackColor(const color clr)  { m_backcolor=clr;   };
   void              BorderColor(const color clr){ m_bordercolor=clr; };
   //---             switches color representation from color to uint type
   uint              uCLR(const color clr) { return(XRGB((clr)&0x0FF,(clr)>>8,(clr)>>16));};
   //--- border and line width
   void              BorderWidth(const int w) { m_borderwidth=w;      };
   //--- adding result for drawing the line in the progress bar
   void              AddResult(bool good);
   //--- updating the progress bar on the chart
   void              Update(void);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CColorProgressBar::CColorProgressBar():m_lastindex(0),m_goodcolor(clrSeaGreen),m_badcolor(clrLightPink)
  {
//--- setting the passes array size to be a bit oversized
   ArrayResize(m_passes,5000,1000);
   ArrayInitialize(m_passes,0);
//---
  }
//+------------------------------------------------------------------+
//|  Initializing                                                    |
//+------------------------------------------------------------------+
bool CColorProgressBar::Create(const string name,int x,int y,int width,int height,ENUM_COLOR_FORMAT clrfmt)
  {
   bool res=false;
//--- invoking the parent class to create canvas
   if(CCanvas::CreateBitmapLabel(name,x,y,width,height,clrfmt))
     {
      //--- storing width and height
      m_height=height;
      m_width=width;
      res=true;
     }
//--- result
   return(res);
  }
//+------------------------------------------------------------------+
//|  Adding the result                                               |
//+------------------------------------------------------------------+
void CColorProgressBar::AddResult(bool good)
  {
   m_passes[m_lastindex]=good;
//--- adding one more vertical line having necessary color to the progress bar
   LineVertical(m_lastindex,m_borderwidth,m_height-m_borderwidth,uCLR(good?m_goodcolor:m_badcolor));
//--- update on the chart
   CCanvas::Update();
//--- updating the index
   m_lastindex++;
   if(m_lastindex>=m_width) m_lastindex=0;
//---
  }
//+------------------------------------------------------------------+
//|  Updating the chart                                              |
//+------------------------------------------------------------------+
void CColorProgressBar::Update(void)
  {
//--- filling the background with the border color
   CCanvas::Erase(CColorProgressBar::uCLR(m_bordercolor));
//--- drawing a rectangle using the background color
   CCanvas::FillRectangle(m_borderwidth,m_borderwidth,
                          m_width-m_borderwidth-1,
                          m_height-m_borderwidth-1,
                          CColorProgressBar::uCLR(m_backcolor));
//--- updating the chart
   CCanvas::Update();
  }
//+------------------------------------------------------------------+
