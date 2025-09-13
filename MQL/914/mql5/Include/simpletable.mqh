//+------------------------------------------------------------------+
//|                                                  SimpleTable.mqh |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#include<ChartObjects\ChartObjectsTxtControls.mqh>
//+------------------------------------------------------------------+
//|  Column implementation class                                     |
//+------------------------------------------------------------------+
class CColumn
  {
private:
   //--- coordinates of the upper left corner of the first cell
   int               m_x;
   int               m_y;
   //--- width and height of each cell
   int               m_width;
   int               m_height;
   //--- cell background and border colors
   color             m_backcolor;
   color             m_bordercolor;
   //--- number of cells and the array for storing cells
   int               m_total;
   CChartObjectEdit  m_items[];
   //--- text adjustment type
   ENUM_ALIGN_MODE   m_align;
   //---
   color             m_textcolor;
public:
   //--- constructor and destructor
                     CColumn(void):m_total(0){ };
                    ~CColumn(void){};
   //--- creating the column
   void              Create(const int X,const int Y,const int w,const int h);
   //--- setting the background and the border colors
   void              BackColor(const color clr);
   void              BorderColor(const color clr);
   //--- text adjustment
   void              TextAlign(const ENUM_ALIGN_MODE align);
   //---
   void              Color(const color clr);
   //--- adding the line to the column
   void              AddItem(string name,string value);
   //--- setting the value
   void              SetValue(const int index,const string svalue);
   //--- delete all
   void              DeleteAll(void);
  };
//+------------------------------------------------------------------+
//|  Constructor                                                     |
//+------------------------------------------------------------------+
void CColumn::Create(const int X,const int Y,const int w,const int h)
  {
   m_x=X;
   m_y=Y;
   m_width=w;
   m_height=h;
  }
//+------------------------------------------------------------------+
//|  Text adjustment                                                 |
//+------------------------------------------------------------------+
void CColumn::TextAlign(const ENUM_ALIGN_MODE align)
  {
//--- changing adjustment type
   m_align=align;
//--- setting adjustment for the already existing objects
   for(int i=0;i<m_total;i++)
     {
      m_items[i].TextAlign(m_align);
     }
  }
//+------------------------------------------------------------------+
//|  Setting text color                                              |
//+------------------------------------------------------------------+
void CColumn::Color(const color clr)
  {
   m_textcolor=clr;
//--- changing the colors for the already existing objects
   for(int i=0;i<m_total;i++) m_items[i].Color(m_textcolor);
//---
  }
//+------------------------------------------------------------------+
//| Setting the background color for a cell                          |
//+------------------------------------------------------------------+
void CColumn::BackColor(const color clr)
  {
   m_backcolor=clr;
   for(int i=0;i<m_total;i++) m_items[i].BackColor(m_backcolor);
//---
  }
//+------------------------------------------------------------------+
//| Setting the border color                                         |
//+------------------------------------------------------------------+
void CColumn::BorderColor(const color clr)
  {
   m_bordercolor=clr;
   for(int i=0;i<m_total;i++) m_items[i].BorderColor(m_bordercolor);
  }
//+------------------------------------------------------------------+
//| Adding one more cell to the bottom of the column                 |
//+------------------------------------------------------------------+
void CColumn::AddItem(string name,string value)
  {
//--- preparing an array 
   int size=ArraySize(m_items);
   int new_size;
   if((new_size=ArrayResize(m_items,size+1))!=-1) m_total=new_size;
//--- calculating Y coordinate for created graphical object
   int curr_y=m_y+m_height*size;
//--- creating a cell
   if(m_items[size].Create(0,name,0,m_x,curr_y,m_width,m_height))
     {
      m_items[size].BorderColor(m_bordercolor);
      m_items[size].BackColor(m_backcolor);
      m_items[size].Color(m_textcolor);
      m_items[size].Description(value);
      m_items[size].TextAlign(m_align);
     }
   else  Comment(__FUNCTION__," Error",GetLastError());
//---
  }
//+------------------------------------------------------------------+
//| Updating a text in a cell                                        |
//+------------------------------------------------------------------+
void CColumn::SetValue(const int index,const string svalue)
  {
   if(index<m_total) m_items[index].Description(svalue);
//---  
  }
//+------------------------------------------------------------------+
//|  Deleting all objects                                            |
//+------------------------------------------------------------------+
void CColumn::DeleteAll(void)
  {
//--- if there are elements for deletion
   if(m_total>0)
     {
      //--- passing in cycle and deleting
      for(int i=m_total-1;i>=0;i--)
        {
         string n=m_items[i].Name();
         if(!m_items[i].Delete()) Print(__FUNCTION__,"Failed to delete the object. Error ",GetLastError());
         ResetLastError();
        }
     }
//---    
  }
//+------------------------------------------------------------------+
//|  Class of the simple two-column table                            |
//+------------------------------------------------------------------+
class CSimpleTable
  {
private:
   string            m_name;           // table name
   int               m_x;              // X coordinate of the upper-left corner
   int               m_y;              // Y coordinate of the upper-left corner
   color             m_backcolor;      // background color
   color             m_bordercolor;    // border color
   color             m_textcolor;      // text color
   CColumn           m_columns[2];     // table columns
   int               m_rows;           // number of rows
public:
   //--- constructor/destructor
                     CSimpleTable();
                    ~CSimpleTable();
   //--- initializing
   void              Create(const string name,const int X,const int Y,const int w1,const int w2,const int h);
   //--- setting the color
   void              BackColor(const color clr);
   void              BorderColor(const color clr);
   void              TextColor(const color clr);
   //--- adding a row to the table
   void              AddRow(const string left_cell,const string right_cell);
   //--- number of rows in the tables
   int               Rows(void) {return (m_rows);};
   //--- updating a cell by column and row indices
   void              SetValue(const int col,const int row,const string sval);
  };
//+------------------------------------------------------------------+
//| Initializing                                                     |
//+------------------------------------------------------------------+
CSimpleTable::Create(const string name,const int X,const int Y,const int w1,const int w2,const int h)
  {
//---
   m_name=name;
   m_x=X;
   m_y=Y;
//--- creating and adjusting the first column
   m_columns[0].Create(X,Y,w1,h);
   m_columns[0].TextAlign(ALIGN_LEFT);
//--- creating and adjusting the second column
   m_columns[1].Create(X+w1,Y,w2,h);
   m_columns[1].TextAlign(ALIGN_RIGHT);
//---
  }
//+------------------------------------------------------------------+
//|  Constructor                                                     |
//+------------------------------------------------------------------+
CSimpleTable::CSimpleTable(): m_rows(0)
  {
//--- table cells names are generated randomly, recharging the generator
   MathSrand(GetTickCount());
  }
//+------------------------------------------------------------------+
//|  Destructor                                                      |
//+------------------------------------------------------------------+
CSimpleTable::~CSimpleTable()
  {
  }
//+------------------------------------------------------------------+
//|  Setting the background color                                    |
//+------------------------------------------------------------------+
void CSimpleTable::BackColor(const color clr)
  {
   m_backcolor=clr;
   m_columns[0].BackColor(m_backcolor);
   m_columns[1].BackColor(m_backcolor);
  }
//+------------------------------------------------------------------+
//|  Setting the border color                                        |
//+------------------------------------------------------------------+
void CSimpleTable::BorderColor(const color clr)
  {
   m_bordercolor=clr;
   m_columns[0].BorderColor(m_bordercolor);
   m_columns[1].BorderColor(m_bordercolor);
  }
//+------------------------------------------------------------------+
//|  Setting the text color                                          |
//+------------------------------------------------------------------+
void CSimpleTable::TextColor(const color clr)
  {
   m_textcolor=clr;
   m_columns[0].Color(m_textcolor);
   m_columns[1].Color(m_textcolor);
  }
//+------------------------------------------------------------------+
//|  Adding a row                                                    |
//+------------------------------------------------------------------+
void CSimpleTable::AddRow(const string left_cell,const string right_cell)
  {
   string cell_name=m_name+"_c1_"+(string)MathRand();
   //PrintFormat("1. Adding a cell %s",cell_name);
   m_columns[0].AddItem(cell_name,left_cell);
   cell_name=m_name+"_c2_"+(string)MathRand();
   //PrintFormat("2. Adding a cell %s",cell_name);
   m_columns[1].AddItem(cell_name,right_cell);
//--- rows counter
   m_rows++;
  }
//+------------------------------------------------------------------+
//| Installing/updating a value in a cell                            |
//+------------------------------------------------------------------+
void CSimpleTable::SetValue(const int col,const int row,const string sval)
  {
   if(col>1 || col<0) return;
   if(row>m_rows) return;
   m_columns[col].SetValue(row,sval);
  }
//+------------------------------------------------------------------+
