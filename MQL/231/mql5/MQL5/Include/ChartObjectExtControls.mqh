//+------------------------------------------------------------------+
//|                                       ChartObjectExtControls.mqh |
//|                                      Copyright 2010, Investeo.pl |
//|                                               http://Investeo.pl |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Investeo.pl"
#property link      "http://Investeo.pl"

#include <Arrays\ArrayObj.mqh>
#include <ChartObjects\ChartObjectsTxtControls.mqh>
#include <ChartObjects\ChartObjectsArrows.mqh>
#include <Arrays\ArrayString.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CChartObjectProgressBar : public CChartObjectEdit
  {

private:
   int               m_value;
   int               m_min;
   int               m_max;
   int               m_direction;
   color             m_color;
   CChartObjectEdit  m_bar;
   string            m_name;
   long              m_chart_id;

public:
   int               GetValue();
   int               GetMin();
   int               GetMax();

   void              SetValue(int val);
   void              SetMin(int val);
   void              SetMax(int val);

   void              SetColor(color bgcol,color fgcol);

   bool              Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,int direction);

   //--- method of identifying the object
   virtual int       Type() const { return(OBJ_EDIT); }
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectProgressBar::Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,int direction=0)
  {
   bool result=ObjectCreate(chart_id,name,(ENUM_OBJECT)Type(),window,0,0,0);

   m_name=name;
   m_chart_id=chart_id;
   m_direction=direction;

   if(direction!=0)
     {
      Y=Y-sizeY;
     }

   ObjectSetInteger(chart_id,name,OBJPROP_BGCOLOR,White);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,White);
   ObjectSetInteger(chart_id,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(chart_id,name,OBJPROP_READONLY,true);

   result&=m_bar.Create(chart_id,name+"m_bar",window,X,Y,sizeX,sizeY);
   m_bar.Color(White);
   m_bar.ReadOnly(true);
   m_bar.Selectable(false);

//---
   if(result) result&=Attach(chart_id,name,window,1);
   result&=X_Distance(X);
   result&=Y_Distance(Y);
   result&=X_Size(sizeX);
   result&=Y_Size(sizeY);
//---
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CChartObjectProgressBar::GetValue(void)
  {
   return m_value;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CChartObjectProgressBar::GetMin(void)
  {
   return m_min;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CChartObjectProgressBar::GetMax(void)
  {
   return m_max;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectProgressBar::SetValue(int val)
  {
   if(m_direction==0) // horizontal ProgressBar
     {
      double sizex=(double)ObjectGetInteger(m_chart_id,m_name,OBJPROP_XSIZE,0);

      double stepSize=sizex/(m_max-m_min);

      m_value=val;
      m_bar.Create(m_bar.ChartId(),m_bar.Name(),m_bar.Window(),m_bar.X_Distance(),m_bar.Y_Distance(),(int)MathFloor(stepSize*m_value),m_bar.Y_Size());
        } else {
      double sizey=(double)ObjectGetInteger(m_chart_id,m_name,OBJPROP_YSIZE,0);

      double stepSize=sizey/(m_max-m_min);
      // Print("stepsize = "+stepSize+" m_bar.X_Size() = "+m_bar.X_Size()+" m_bar.Y_Size() = "+m_bar.Y_Size());
      m_value=val;
      m_bar.Create(m_bar.ChartId(),m_bar.Name(),m_bar.Window(),m_bar.X_Distance(),(int)(this.Y_Distance()+sizey-MathFloor(stepSize*m_value)),m_bar.X_Size(),(int)MathFloor(stepSize*m_value));

     }

   m_bar.Description(IntegerToString(m_value));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectProgressBar::SetMin(int val)
  {
   m_min=val;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectProgressBar::SetMax(int val)
  {
   m_max=val;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectProgressBar::SetColor(color bgCol,color fgCol=White)
  {
   m_color=bgCol;
   m_bar.BackColor(m_color);
   m_bar.Color(fgCol);
  }
//+------------------------------------------------------------------+

// Spinner control

class CChartObjectSpinner: public CChartObjectEdit
  {

private:
   double            m_value;
   double            m_stepSize;
   double            m_min;
   double            m_max;
   int               m_precision;
   string            m_name;
   long              m_chart_id;
   CChartObjectButton m_up,m_down;

public:
   double            GetValue();
   double            GetMin();
   double            GetMax();

   void              SetValue(double val);
   void              SetMin(double val);
   void              SetMax(double val);
   void              SetStepSize(double val);

   double            Inc();
   double            Dec();

   bool              Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,double val,double stepSize,int precision);

   //--- method of identifying the object
   virtual int       Type() const { return(OBJ_EDIT); }

  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectSpinner::Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,double val=0.0,double stepSize=1.0,int precision=8)
  {
   bool result=ObjectCreate(chart_id,name,(ENUM_OBJECT)Type(),window,0,0,0);

   m_name=name;
   m_chart_id=chart_id;
   m_value=val;
   m_stepSize=stepSize;
   m_precision=precision;

   ObjectSetInteger(chart_id,name,OBJPROP_BGCOLOR,White);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Black);
   ObjectSetInteger(chart_id,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(chart_id,name,OBJPROP_READONLY,true);

   result&=m_up.Create(chart_id, name+"_up", window, X+sizeX, Y, 15, sizeY/2);
   result&=m_down.Create(chart_id, name+"_down", window, X+sizeX, Y+sizeY/2, 15, sizeY/2);
   m_up.Description("+");
   m_down.Description("-");
   ObjectSetString(chart_id,name,OBJPROP_TEXT,0,(DoubleToString(m_value,precision)));

//---
   if(result) result&=Attach(chart_id,name,window,1);
   result&=X_Distance(X);
   result&=Y_Distance(Y);
   result&=X_Size(sizeX);
   result&=Y_Size(sizeY);
//---
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectSpinner::SetValue(double val)
  {
   if(val>=m_min && val<=m_max) m_value=val;
   this.Description(DoubleToString(m_value));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectSpinner::SetMin(double min)
  {
   m_min=min;

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectSpinner::SetMax(double max)
  {
   m_max=max;
  }
  
void CChartObjectSpinner::SetStepSize(double val)
  {
   m_stepSize=val;
  }
//+------------------------------------------------------------------+
double CChartObjectSpinner::GetValue(void)
  {
   return m_value;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CChartObjectSpinner::Inc(void)
  {
   if(NormalizeDouble(m_max-m_value-m_stepSize,m_precision)>0.0) m_value+=m_stepSize;
   else m_value=m_max;
   this.Description(DoubleToString(m_value, m_precision));
   m_up.State(false);
   return m_value;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CChartObjectSpinner::Dec(void)
  {
   //Print(NormalizeDouble(m_value-m_stepSize-m_min,m_precision));

   if(NormalizeDouble(m_value-m_stepSize-m_min,m_precision)>0.0)
      m_value-=m_stepSize; else m_value=m_min;
   this.Description(DoubleToString(m_value,m_precision));
   m_down.State(false);

   return m_value;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CChartObjectTextSpinner: public CChartObjectEdit
  {

private:
   CArrayString     *m_values;
   int               m_currIndex;
   string            m_name;
   long              m_chart_id;
   CChartObjectButton m_up,m_down;

public:
   string            GetCurrentVal();
   string            GetName();
   int               GetIndex();
   void              SetIndex(int ind);
   bool              Inc();
   bool              Dec();

   bool              Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,const CArrayString *arr);
   void             ~CChartObjectTextSpinner();

   //--- method of identifying the object
   virtual int       Type() const { return(OBJ_EDIT); }

  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectTextSpinner::Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,const CArrayString *arr)
  {
   bool result=ObjectCreate(chart_id,name,(ENUM_OBJECT)Type(),window,0,0,0);

   m_values=new CArrayString();

   m_values.AssignArray(arr);
   m_name=name;
   m_chart_id=chart_id;

   ObjectSetInteger(chart_id,name,OBJPROP_BGCOLOR,White);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Black);
   ObjectSetInteger(chart_id,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(chart_id,name,OBJPROP_READONLY,true);
   ObjectSetInteger(chart_id,name,OBJPROP_FONTSIZE,9);

   result&=m_up.Create(chart_id, name+"_up", window, X+sizeX, Y, 15, sizeY/2);
   result&=m_down.Create(chart_id, name+"_down", window, X+sizeX, Y+sizeY/2, 15, sizeY/2);
   m_up.Description("+");
   m_down.Description("-");
   ObjectSetString(chart_id,name,OBJPROP_TEXT,0,m_values.At(0));
   m_currIndex=0;

//---
   if(result) result&=Attach(chart_id,name,window,1);
   result&=X_Distance(X);
   result&=Y_Distance(Y);
   result&=X_Size(sizeX);
   result&=Y_Size(sizeY);
//---
   return(result);

  }
  
  void CChartObjectTextSpinner::~CChartObjectTextSpinner(void)
  {
   m_values.Clear();
   delete m_values;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CChartObjectTextSpinner::SetIndex(int ind)
  {
   m_currIndex=ind;
   ObjectSetString(m_chart_id,m_name,OBJPROP_TEXT,0,m_values.At(ind));

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CChartObjectTextSpinner::GetIndex()
  {
   return(m_currIndex);

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CChartObjectTextSpinner::GetName(void)
  {
   if(CheckPointer(m_values))
      return m_name;
   else return NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CChartObjectTextSpinner::GetCurrentVal(void)
  {
   if(CheckPointer(m_values))
      return this.Description();

   else return "";
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectTextSpinner::Inc(void)
  {
   bool result=false;
   this.m_up.State(false);

   if(CheckPointer(m_values))
      if(m_currIndex<m_values.Total()-1)
        {

         m_currIndex++;
         this.SetString(OBJPROP_TEXT,m_values.At(m_currIndex));

         //result = ObjectSetString(m_chart_id,m_name,OBJPROP_TEXT,0,m_values.At(m_currIndex));
        }

   return result;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectTextSpinner::Dec(void)
  {
   bool result=false;
   this.m_down.State(false);

   if(CheckPointer(m_values))
      if(m_currIndex>0)
        {
         m_currIndex--;
         this.SetString(OBJPROP_TEXT,m_values.At(m_currIndex));

         //result = ObjectSetString(m_chart_id,m_name,OBJPROP_TEXT,0,m_values.At(m_currIndex));
        }

   return result;
  }
//+------------------------------------------------------------------+
class CChartObjectEditTable
  {

private:
   CArrayObj        *array2D;
   int               m_rows;
   int               m_cols;
   string            m_baseName;

public:

   bool              Create(long chart_id,string name,int window,int rows,int cols,int startX,int startY,int sizeX,int sizeY,color Bg,int deltaX,int deltaY);
   bool              Delete();
   bool              SetColor(int row,int col,color newColor);
   color             GetColor(int row,int col);
   bool              SetText(int row,int col,string newText);
   string            GetText(int row,int col);
   bool              SetFontSize(int row,int col,int newSize);

  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectEditTable::Create(long chart_id,string name,int window,int rows=1,int cols=1,int startX=0,int startY=0,int sizeX=15,int sizeY=15,color Bg=White,int deltaX=5,int deltaY=5)
  {
   m_rows=rows;
   m_cols=cols;
   m_baseName=name;
   int i=0,j=0;

   array2D=new CArrayObj();
   if(array2D==NULL) return false;

   for(j=0; j<m_cols; j++)
     {
      CArrayObj *new_array=new CArrayObj();
      if(array2D==NULL) return false;

      array2D.Add(new_array);
      for(i=0; i<m_rows; i++)
        {
         CChartObjectEdit *new_edit=new CChartObjectEdit();

         new_edit.Create(chart_id, name+IntegerToString(i)+":"+IntegerToString(j), window, startX+j*(sizeX+deltaX), startY+i*(sizeY+deltaY), sizeX, sizeY);
         new_edit.BackColor(Bg);
         new_edit.Color(White);
         new_edit.Selectable(false);
         new_edit.ReadOnly(true);
         new_edit.Description("");
         new_array.Add(new_edit);
        }
     }

   return true;
  }
//+------------------------------------------------------------------+
bool CChartObjectEditTable::Delete(void)
  {
   if (CheckPointer(array2D))
   for(int j=0; j<m_cols; j++)
     {
      CArrayObj *column_array=array2D.At(j);
      column_array.Clear();
      delete column_array;
     }
   delete array2D;
   return true;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectEditTable::SetColor(int row,int col,color newColor)
  {
   CArrayObj *sub_array;
   CChartObjectEdit *element;

   if((row>=0 && row<m_rows) && (col>=0 && col<m_cols))
     {
      if(array2D!=NULL)
        {
         sub_array=array2D.At(col);
         element=(CChartObjectEdit*)sub_array.At(row);
         element.BackColor(newColor);

         return true;
        }
     }

   return false;
  }
//+------------------------------------------------------------------+
color CChartObjectEditTable::GetColor(int row,int col)
  {
   CArrayObj *sub_array;
   CChartObjectEdit *element;

   if((row>=0 && row<m_rows) && (col>=0 && col<m_cols))
     {
      if(array2D!=NULL)
        {
         sub_array=array2D.At(col);
         element=(CChartObjectEdit*)sub_array.At(row);
         return element.BackColor();
        }
     }

   return NULL;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectEditTable::SetText(int row,int col,string newText)
  {
   CArrayObj *sub_array;
   CChartObjectEdit *element;

   if((row>=0 && row<m_rows) && (col>=0 && col<m_cols))
     {
      if(array2D!=NULL)
        {
         sub_array=array2D.At(col);
         element=(CChartObjectEdit*)sub_array.At(row);
         element.Description(newText);

         return true;
        }
     }

   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string CChartObjectEditTable::GetText(int row,int col)
  {
   CArrayObj *sub_array;
   CChartObjectEdit *element;

   if((row>=0 && row<m_rows) && (col>=0 && col<m_cols))
     {
      if(array2D!=NULL)
        {
         sub_array=array2D.At(col);
         element=(CChartObjectEdit*)sub_array.At(row);
         return element.Description();
        }
     }

   return NULL;
  }
//+------------------------------------------------------------------+
bool CChartObjectEditTable::SetFontSize(int row,int col,int newSize)
  {
   CArrayObj *sub_array;
   CChartObjectEdit *element;

   if((row>=0 && row<m_rows) && (col>=0 && col<m_cols))
     {
      if(array2D!=NULL)
        {
         sub_array=array2D.At(col);
         element=(CChartObjectEdit*)sub_array.At(row);
         element.FontSize(newSize);

         return true;
        }
     }

   return false;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CChartObjectRectangleLabel : public CChartObjectEdit
  {
public:
   //--- method of creating the object
   bool              Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,color fg,color bg);
   //--- method of identifying the object
   virtual int       Type() const        { return(OBJ_RECTANGLE_LABEL); }
   //--- methods for working with files
   bool              Border(ENUM_BORDER_TYPE border);
   int               Border() const;

  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectRectangleLabel::Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,color fg=Gray,color bg=Gray)
  {
   bool result=ObjectCreate(chart_id,name,(ENUM_OBJECT)Type(),window,0,0,0);
//---
   if(result) result&=Attach(chart_id,name,window,1);
   result&=X_Distance(X);
   result&=Y_Distance(Y);
   result&=X_Size(sizeX);
   result&=Y_Size(sizeY);
   result&=BackColor(bg);
   result&=Color(fg);
//---
   return(result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CChartObjectRectangleLabel::Border(void) const
  {
//--- checking
   if(m_chart_id==-1) return(0);
//---
   return((int)ObjectGetInteger(m_chart_id,m_name,OBJPROP_BORDER_TYPE));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectRectangleLabel::Border(ENUM_BORDER_TYPE border)
  {
//--- checking
   if(m_chart_id==-1) return(false);
//---
   return(ObjectSetInteger(m_chart_id,m_name,OBJPROP_BORDER_TYPE,border));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CChartObjectListbox : public CChartObject
  {

private:
   CArrayString     *m_strList;
   CArrayObj        *m_editList;
   CChartObjectButton *m_upBtn;
   CChartObjectButton *m_dnBtn;
   int               m_currentPos;
   int               m_rowHeight;
   int               m_nRows;
   int               m_firstVisiblePos;
   int               m_lastVisiblePos;
   bool              m_sorted;

public:
   void              CChartObjectListbox();
   void             ~CChartObjectListbox();
   
   //--- method of creating the object
   bool              Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,int rowHeight,int fontSize);
   //--- method of identifying the object
   virtual int       Type() const        { return(OBJ_RECTANGLE_LABEL); }
   
   // -- adding, inserting and removing elements
   bool              Add(const CArrayString* strArray);
   bool              Add(string newString);
   bool              Insert(const CArrayString* strArray, int insertIdx);
   bool              Insert(const string newString, int insertIdx);
   bool              Remove(int idx);
   void              RemoveAll();
   string            Get();
   int               Idx();
   void              Sort(int sortMode);
   CArrayString*     List();
   
   bool              Up();
   bool              Down();
   bool              Refresh();
   int               Total();
   
   void              Info();
};
  
void CChartObjectListbox::CChartObjectListbox(void)
{
   m_editList = new CArrayObj();
   m_strList = new CArrayString();
   m_upBtn = new CChartObjectButton();
   m_dnBtn = new CChartObjectButton();
   m_currentPos = 0;
   m_firstVisiblePos = 0;
   m_sorted=false;
}

void CChartObjectListbox::~CChartObjectListbox(void)
{
  m_editList.Clear();
  m_strList.Clear();
  delete m_editList;
  delete m_strList;
  delete m_upBtn;
  delete m_dnBtn;
}

bool CChartObjectListbox::Add(string newString)
{
   return m_strList.Add(newString);
}

bool CChartObjectListbox::Add(const CArrayString *newStrings)
{
   return m_strList.AddArray(newStrings);
}

bool CChartObjectListbox::Insert(const string newString, int insertIdx)
{
   return m_strList.Insert(newString, insertIdx);
}

bool CChartObjectListbox::Insert(const CArrayString *strArray,int insertIdx)
{
   return m_strList.InsertArray(strArray, insertIdx);
}

void CChartObjectListbox::Sort(int sortMode)
{
   m_strList.Sort(sortMode);
   m_sorted=true;
}

bool CChartObjectListbox::Remove(int idx)
{
   if (m_strList.Total()==0) return false;
   if (m_strList.Total()==1) { RemoveAll(); return true; };
   if (m_currentPos!=m_firstVisiblePos) m_currentPos--; 
   m_lastVisiblePos--;
   return m_strList.Delete(idx);
}

void CChartObjectListbox::RemoveAll()
{
   m_strList.Clear();
   m_currentPos = 0;
   m_firstVisiblePos = 0;
   m_lastVisiblePos = m_nRows-1;
   
}

bool CChartObjectListbox::Up(void)
{
   if (m_strList.Total()==0 || m_currentPos==0) return false;
   if (m_firstVisiblePos==m_currentPos) { m_firstVisiblePos--; m_lastVisiblePos--; }
   m_currentPos--;
   Refresh();
   return true;
}

bool CChartObjectListbox::Down(void)
{
   if (m_currentPos>=m_strList.Total()-1) return false;
   if (m_lastVisiblePos==m_currentPos) { m_firstVisiblePos++; m_lastVisiblePos++; }
   m_currentPos++;
   Refresh();
   return true;
   
}

bool CChartObjectListbox::Refresh()
{
   int idx=0;
   
   for (int i=m_firstVisiblePos; i<MathMax((double)m_lastVisiblePos+1, (double)m_nRows); i++, idx++)
      {
         CChartObjectEdit *element = m_editList.At(idx);
         if (i<m_strList.Total())
         {
            if (m_sorted==true) element.Description(m_strList.At(m_strList.Total()-1-i));
               else element.Description(m_strList.At(i));
         } else element.Description("");
         if (i==m_currentPos) element.BackColor(LightBlue); else element.BackColor(Snow);
      }      
         
   return true;   
}

void CChartObjectListbox::Info(void)
{
   Print("Number of strings = " + IntegerToString(m_strList.Total()) + " firstVisiblePos = " + IntegerToString(m_firstVisiblePos) 
         + " lastVisiblePos = " + IntegerToString(m_lastVisiblePos) + " currentPos = " + IntegerToString(m_currentPos) + " m_nRows = " + IntegerToString(m_nRows)); 
}

string CChartObjectListbox::Get(void)
{
if (m_strList.Total()!=0) 
   if (m_sorted==true) 
      return m_strList.At(m_strList.Total()-1-m_currentPos);
      else return m_strList.At(m_currentPos);
   else return "";
}

int CChartObjectListbox::Idx(void)
{
if (m_strList.Total()!=0) if (m_sorted==true) return m_strList.Total()-1-m_currentPos; else return m_currentPos;
   return -1;
}

int CChartObjectListbox::Total(void)
{
 return m_strList.Total();
}

CArrayString* CChartObjectListbox::List(void)
{
   return m_strList;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CChartObjectListbox::Create(long chart_id,string name,int window,int X,int Y,int sizeX,int sizeY,int rowHeight,int fontSize=10)
  {
   bool result;

   m_rowHeight=rowHeight;
   int ySizeBtn=20;
   
   m_nRows=(sizeY-40)/rowHeight;
   
   m_lastVisiblePos = m_nRows-1;
   
   result=m_upBtn.Create(0,name+"up",0,X,Y,sizeX,ySizeBtn);
   m_upBtn.Description("UP");
   for (int i=0; i<m_nRows; i++)
      {
         CChartObjectEdit* newEdit = new CChartObjectEdit();
         newEdit.Create(0, name+"edit"+IntegerToString(i), 0, X, Y+ySizeBtn+i*m_rowHeight, sizeX, m_rowHeight);
         newEdit.BackColor(Snow);
         newEdit.FontSize(fontSize);
         newEdit.Color(DimGray);
         newEdit.ReadOnly(true);
         m_editList.Add(newEdit);
      }
   result&=m_dnBtn.Create(0,name+"down",0,X,Y+ySizeBtn+m_nRows*m_rowHeight,sizeX,ySizeBtn);
   m_dnBtn.Description("DOWN");
   
   return result;

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
ENUM_TIMEFRAMES  StringToTimeframe(string tf)
  {

   if(tf=="M1") return PERIOD_M1;
   if(tf=="M5") return PERIOD_M5;
   if(tf=="M15") return PERIOD_M15;
   if(tf=="M30") return PERIOD_M30;
   if(tf=="H1") return PERIOD_H1;
   if(tf=="H4") return PERIOD_H4;
   if(tf=="D1") return PERIOD_D1;
   if(tf=="W1") return PERIOD_W1;
   if(tf=="MN1") return PERIOD_MN1;

   return PERIOD_CURRENT;
  }
//+------------------------------------------------------------------+
int TimeframeToInt(ENUM_TIMEFRAMES tf)
  {
   if(tf==PERIOD_M1) return 0;
   if(tf==PERIOD_M5) return 1;
   if(tf==PERIOD_M15) return 2;
   if(tf==PERIOD_M30) return 3;
   if(tf==PERIOD_H1) return 4;
   if(tf==PERIOD_H4) return 5;
   if(tf==PERIOD_D1) return 6;
   if(tf==PERIOD_W1) return 7;
   if(tf==PERIOD_MN1) return 8;
   return -1;
  }
//+------------------------------------------------------------------+
