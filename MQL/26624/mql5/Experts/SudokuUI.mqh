//+------------------------------------------------------------------+
//|                                                     SudokuUI.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|               UI is based on Layouts framework by Enrico Lambino |
//|                             https://www.mql5.com/en/users/iceron |
//+------------------------------------------------------------------+

#include <Sudoku/Layouts/GridTk.mqh>
#include <Sudoku/Layouts/MaximizableAppDialog.mqh>
#include <Controls\Edit.mqh>
#include <Controls\Button.mqh>

#include <Sudoku/Sudoku.mqh>
#include <Sudoku/Converter.mqh>

#define SUDOKU_SIZE 81
#define SUDOKU_SIDE 9
#define BLOCKS      9


#resource "intro17.txt" as string IntroductorySudoku

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class SudokuDialog: public MaximizableAppDialog // CAppDialog
{
  protected:
    bool m_solved;
    CGridTk m_main;
    CEdit m_edits[SUDOKU_SIZE];
    CButton m_button_new;
    CButton m_button_hint;
    CButton m_button_undo;
    CPanel m_blocks[BLOCKS];
    
    string currentBackup;
    
    string m_preload;
    ClassicSudoku sudoku;
    
    int m_seed1;
    int m_seed2;
    uint m_hide;
    uint m_cycles;
    bool m_hint_shown;
    bool m_autoupdate;
    bool m_collision;
    
    const static string logger;

  private:
    void move(const int index, const ulong previous, const ulong current, const ulong postpond = (ulong)-1);
    void undo();
    void updateEdit(CEdit *edit, const string text);
    void validate(const int index);
    void checkStructure(SudokuStructure *s);
    
    class Callback: public SudokuNotify
    {
      private:
        SudokuDialog *host;

      public:
        Callback(SudokuDialog *ptr): host(ptr) {}
        void onChange(const uint index, const ulong value)
        {
          host.onChange(index, value);
        }
    };
    
    Callback callback;
    int changeNestingLevel;
  
  public:
    SudokuDialog();
    ~SudokuDialog();
    
    virtual bool Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2);
    virtual bool OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam);
    
    void onChange(const uint index, const ulong value)
    {
      if(!m_autoupdate) return;

      changeNestingLevel++;
      ulong v = sudoku.cellByIndex(index);
      if(v != value)
      {
        const string text = sudoku.cellAsString(index);
        if(apply(index, text, false, value))
        {
          CEdit *edit = &m_edits[index];
          updateEdit(edit, text);
        }
      }
      changeNestingLevel--;
    }
  
    void randomize(const int randShuffling, const int randComposing, const uint excludeLabel, const uint shufflingCycles)
    {
      m_seed1 = randShuffling;
      m_seed2 = randComposing;
      m_hide = excludeLabel;
      m_cycles = shufflingCycles;
    }
    
    void enableAutoUpdate(const bool auto)
    {
      m_autoupdate = auto;
      sudoku.bind(m_autoupdate ? &callback : NULL);
    }

    void preload(string file);

  protected:
    virtual bool CreateMain(const long chart, const string name, const int subwin);
    virtual bool CreateCell(const int button_id, const long chart, const string name, const int subwin);
    virtual bool CreateButtonNew(const long chart, const string name, const int subwin);
    virtual bool CreateBlocks(const long chart, const string name, const int subwin);
    virtual bool CreateHint(const long chart, const string name, const int subwin);
    virtual bool CreateUndo(const long chart, const string name, const int subwin);
    bool OnStartEdit(const int i);
    bool OnEndEdit(const int i);
    void OnClickButtonNew();
    void OnClickButtonHint();
    void OnClickButtonUndo();
    virtual void SelfAdjustment(const bool restore = false) override;
    
    bool apply(const int index, const string value, const bool undo = false, const ulong postpond = (ulong)-1);
    void shuffle();
    void update(const bool init = true);
  
    int getFontSize(const string text) const;
    // string wrapText(const string text) const; // not supported by MQL
};

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(SudokuDialog)
  ON_INDEXED_EVENT(ON_START_EDIT, m_edits, OnStartEdit)
  ON_INDEXED_EVENT(ON_END_EDIT, m_edits, OnEndEdit)
  ON_EVENT(ON_CLICK, m_button_new, OnClickButtonNew)
  ON_EVENT(ON_CLICK, m_button_hint, OnClickButtonHint)
  ON_EVENT(ON_CLICK, m_button_undo, OnClickButtonUndo)
EVENT_MAP_END(MaximizableAppDialog)
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
SudokuDialog::SudokuDialog():
  m_solved(false),
  m_seed1(-1),
  m_seed2(-1),
  m_hide(0),
  m_cycles(100),
  callback(&this)

{
  sudoku.enableMultipleCheck();
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
SudokuDialog::~SudokuDialog()
{
  if(StringLen(m_preload) > 0)
  {
    sudoku.exportPosition(m_preload + logger);
  }
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool SudokuDialog::Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2)
{
  if(!MaximizableAppDialog::Create(chart, name, subwin, x1, y1, x2, y2))
    return (false);

  if(!CreateMain(chart, name, subwin))
    return (false);

  if(!CreateBlocks(chart, name, subwin)) return false;

  for(int i = 1; i <= SUDOKU_SIZE; i++)
  {
    if(!CreateCell(i, chart, "cell", subwin))
      return (false);
  }
  
  if(!CreateButtonNew(chart, name, subwin))
    return (false);
  if(!CreateHint(chart, name, subwin))
    return (false);
  if(!CreateUndo(chart, name, subwin))
    return (false);
  if(!m_main.Pack())
    return (false);
  if(!Add(m_main))
    return (false);

  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool SudokuDialog::CreateMain(const long chart, const string name, const int subwin)
{
  if(!m_main.Create(chart, name + "main", subwin, 0, 0, ClientAreaWidth(), ClientAreaHeight()))
    return (false);
  m_main.Init(10, 9, 2, 2);
  m_main.ColorBackground(clrDarkGray);
  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool SudokuDialog::CreateButtonNew(const long chart, const string name, const int subwin)
{
  if(!m_button_new.Create(chart, name + "buttonnew", m_subwin, 0, 0, 101, 101))
    return (false);
  m_button_new.Text("New");
  m_button_new.ColorBackground(clrYellow);
  if(!m_main.Grid(&m_button_new, 9, 0, 1, 3))
    return (false);
  return (true);
}

bool SudokuDialog::CreateBlocks(const long chart, const string name, const int subwin)
{
  const int blockSide = (int)sqrt(SUDOKU_SIZE / BLOCKS);

  for(int i = 0; i < BLOCKS; i++)
  {
    if(!m_blocks[i].Create(chart, name + "block" + (string)i, m_subwin, 0, 0, 101, 101))
      return (false);
    m_blocks[i].ColorBackground(clrWhite);
    if(!m_main.Grid(&m_blocks[i], (i / blockSide) * blockSide, (i % blockSide) * blockSide, blockSide, blockSide))
      return (false);
  }
  return (true);
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool SudokuDialog::CreateHint(const long chart, const string name, const int subwin)
{
  if(!m_button_hint.Create(chart, name + "hint", m_subwin, 0, 0, 101, 101))
    return (false);
  m_button_hint.Text("Hint");
  m_button_hint.ColorBackground(clrGreenYellow);
  if(!m_main.Grid(&m_button_hint, 9, 3, 1, 3))
    return (false);
    
  return (true);
}

bool SudokuDialog::CreateUndo(const long chart, const string name, const int subwin)
{
  if(!m_button_undo.Create(chart, name + "undo", m_subwin, 0, 0, 101, 101))
    return (false);
  m_button_undo.Text("Undo");
  m_button_undo.Hide();
  if(!m_main.Grid(&m_button_undo, 9, 6, 1, 3))
    return (false);
    
  return (true);
}

int SudokuDialog::getFontSize(const string text) const
{
  const int n = MathMax(StringLen(text), 1);
  //const int n = StringLen(text) > 1 ? 3 : 1;
  return ClientAreaHeight() / 9 / (n + 1);
}

// until multiline editboxes are not supported in MT,
// this method for formatting many guesses will not work
/*
string SudokuDialog::wrapText(const string text) const
{
  const int n = StringLen(text);
  if(n > 2)
  {
    return StringSubstr(text, 0, n / 2) + "\n" + StringSubstr(text, n / 2);
  }
  return text;
}
*/

bool SudokuDialog::CreateCell(const int button_id, const long chart, const string name, const int subwin)
{
  CEdit *edit = &m_edits[button_id - 1];
  if(!edit.Create(chart, name + IntegerToString(button_id), subwin, 0, 0, 100, 100))
    return (false);
  if(!edit.Text(/*IntegerToString(button_id)*/""))
    return (false);
  edit.Id(button_id);
  edit.ColorBackground(clrBeige);
  edit.TextAlign(ALIGN_CENTER);
  edit.FontSize(getFontSize(edit.Text()));
  edit.Font("Verdana");
  // Add replaced by Grid (because cell controls are "misplaced" by blocks,
  // which are added to the main window before cells in order to lay "under" them)
  //if(!m_main.Add(edit))
  //  return (false);
  const int size = (int)sqrt(SUDOKU_SIZE);
  if(!m_main.Grid(edit, (button_id - 1) / size, (button_id - 1) % size))
    return (false);
  return (true);
}

bool SudokuDialog::OnStartEdit(const int e)
{
  if(m_collision) update(false);
  CEdit *edit = &m_edits[e];
  currentBackup = edit.Text();
  return true;
}

bool SudokuDialog::OnEndEdit(const int e)
{
  CEdit *edit = &m_edits[e];
  
  string text = edit.Text();
  StringTrimLeft(text);
  StringTrimRight(text);
  for(int i = 0; i < StringLen(text); i++)
  {
    if((text[i] < '1' || text[i] > '9')) // && text[i] != '\n' && text[i] != ' '
    {
      edit.Text(/*wrapText*/(currentBackup));
      return true;
    }
  }
  
  if(text != currentBackup)
  {
    if(apply(e, text))
    {
      #ifdef SUDOKU_LOG_USER_MOVES
      int count = (int)GlobalVariableGet(m_preload + "_move_count");
      printf("%d user [%d,%d] %s -> %s", count, e / SUDOKU_SIDE, e % SUDOKU_SIDE, currentBackup, text);
      #endif
      updateEdit(edit, text);
    }
  }
  
  return true;
}

void SudokuDialog::updateEdit(CEdit *edit, const string text)
{
  edit.Text(text); //edit.Text(wrapText(edit.Text()));
  edit.FontSize(getFontSize(text));
  edit.Color(StringLen(text) > 1 ? clrGreen : clrBlue);
}

void SudokuDialog::OnClickButtonNew(void)
{
  if(m_collision) update(false);
  m_button_new.Text("Please, wait...");
  shuffle();
  m_button_new.Text("New");
  m_solved = false;
}

void SudokuDialog::OnClickButtonHint(void)
{
  if(!m_hint_shown)
  {
    sudoku.findCandidates();
  }
  else
  {
    sudoku.clearCandidates();
  }
  m_hint_shown = !m_hint_shown;
  update(false);
}

void SudokuDialog::OnClickButtonUndo(void)
{
  if(m_collision) update(false);
  undo();
}

void SudokuDialog::SelfAdjustment(const bool restore = false)
{
  CSize size;
  size.cx = ClientAreaWidth();
  size.cy = ClientAreaHeight();
  m_main.Size(size);

  for(int i = 0; i < ArraySize(m_edits); i++)
  {
    m_edits[i].FontSize(getFontSize(m_edits[i].Text()));
  }

  m_main.Pack();
}

void SudokuDialog::preload(string file)
{
  if(StringLen(file) == 0)
  {
    if(m_seed1 == -1 && m_seed2 == -1 && m_hide == 0)
    {
      if(sudoku.loadFromText(IntroductorySudoku))
      {
        file = "IntroductorySudoku17.txt";
        sudoku.saveToFile(file);
      }
      else
      {
        return;
      }
    }
    else
    {
      Comment("Click 'New' button to generate new puzzle");
      return;
    }
  }

  if(sudoku.loadFromFile(file))
  {
    ulong field[];
    Comment("Loading ", file, ", please wait");
    sudoku.exportField(field); // preserve state for a moment
    if(sudoku.solve() && sudoku.getStatus() == CORRECT) // try to solve for correction check
    {
      sudoku.importField(field); // restore state
      uint f = sudoku.countFilled();
      uint nesting = sudoku.getNestingLevel();
      m_preload = file;

      update(); // first, show initial state with read-only clues
      
      string restored;
      if(sudoku.importPosition(file + logger))
      {
        restored = ", Position restored";
        update(false); // second, show partial/completed state with moves/edits
        
        uint upd = sudoku.countFilled();
        if(upd == f && GlobalVariableGet(m_preload + "_move_count") > 0)
        {
          if(MessageBox("Current state seems initial but undo exist, clear undo?", NULL, MB_YESNO) == IDYES)
          {
            int len = StringLen(m_preload);
            for(int i = GlobalVariablesTotal() - 1; i >= 0; i--)
            {
              string name = GlobalVariableName(i);
              if(StringSubstr(name, 0, len) == m_preload)
              {
                GlobalVariableDel(name);
              }
            }
          }
        }
      }
      else
      {
        GlobalVariableDel(m_preload + "_move_count");
      }
      Print(sudoku.exportAsText());

      Comment("Loaded ", file, ": ", f, " clues, difficulty: ", (float)(1.0 * SUDOKU_SIZE / 2 / f * sqrt(nesting + 1)), restored);
      
      if(GlobalVariableGet(m_preload + "_move_count") > 0)
      {
        m_button_undo.Show();
      }
      else
      {
        m_button_undo.Hide();
      }
    }
    else
    {
      string msg = "Incorrect sudoku: " + EnumToString(sudoku.getStatus());
      Comment(msg);
      Print(msg);
    }
  }
  else
  {
    Alert("Wrong file format or sudoku size: ", file);
  }
}

void SudokuDialog::checkStructure(SudokuStructure *s)
{
  if(!s.isSolved(true))
  {
    m_collision = true;
    for(uint i = 0; i < s.size(); i++)
    {
      uint index = s.getIndex(i);
      CEdit *edit = &m_edits[index];
      edit.ColorBackground(0x400000 ^ edit.ColorBackground());
    }
  }
}

void SudokuDialog::validate(const int index)
{
  const int row = index / SUDOKU_SIDE, column = index % SUDOKU_SIDE;

  AutoPtr<SudokuStructure> r(sudoku.row(row));
  AutoPtr<SudokuStructure> c(sudoku.column(column));
  AutoPtr<SudokuStructure> b(sudoku.block(sudoku.coordinates2block(row, column)));
  checkStructure(~r);
  checkStructure(~c);
  checkStructure(~b);
}

//+------------------------------------------------------------------+
//| return true on valid move/mark                                   |
//+------------------------------------------------------------------+
bool SudokuDialog::apply(const int index, const string value, const bool undo = false, const ulong postpond = (ulong)-1)
{
  const int n = StringLen(value);
  const int y = index / SUDOKU_SIDE, x = index % SUDOKU_SIDE;
  ulong previous = sudoku.cellByIndex(index);
  if(n == 1)
  {
    if(sudoku.setValue(y, x, 0, (uchar)value[0]))
    {
      if(sudoku.countFilled() == SUDOKU_SIZE)
      {
        if(sudoku.isSolved())
        {
          Alert("Solved ", m_preload);
          m_solved = true;
        }
        else
        {
          Alert("Wrong ", m_preload);
        }
      }
      
      if(!undo)
      {
        ulong current = sudoku.cellByIndex(index);
        // log this move/edit
        move(index, postpond != (ulong)-1 ? postpond : previous, current, postpond);
      }

      if(m_autoupdate)
      {
        validate(index);
      }

      return true;
    }
  }
  else if(n > 1)
  {
    sudoku.clearCandidates(y, x);
    for(int i = 0; i < n; i++)
    {
      if(!sudoku.setCandidate(y, x, 0, (uchar)value[i])) return false;
    }

    if(!undo)
    {
      ulong current = sudoku.cellByIndex(index);
      move(index, postpond != (ulong)-1 ? postpond : previous, current, postpond);
    }

    return true;
  }
  else // n == 0 - erase this cell
  {
    if(sudoku.setValue(y, x))
    {
      if(!undo)
      {
        move(index, postpond != (ulong)-1 ? postpond : previous, 0, postpond);
      }
      return true;
    }
  }
  return false;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SudokuDialog::shuffle(void)
{
  if(StringLen(m_preload) > 0)
  {
    sudoku.exportPosition(m_preload + logger);
  }

  Comment("Generating composition...");
  sudoku.initialize();                             // initially ordered field
  sudoku.randomize(m_seed1, m_cycles);             // shuffling
  uint nesting = sudoku.generate(m_seed2, m_hide); // clues selection
  ulong field[];
  int n = sudoku.exportField(field);               // implied to be SUDOKU_SIZE
  uint f = sudoku.countFilled();
  
  Comment("Done: ", f, " clues, difficulty estimate: ", (float)(1.0 * n / 2 / f * sqrt(nesting + 1)));
  if(m_seed1 > -1 && m_seed2 > -1 && m_hide > 0)
  {
    m_preload = "gen" + (string)m_seed1 + "x" + (string)m_seed2 + "-" + (string)m_hide + "-" + (string)m_cycles;
  }
  else
  {
    MathSrand(GetTickCount());
    m_preload = "rnd" + (string)rand();
  }
  m_preload += ".txt";
  
  sudoku.saveToFile(m_preload);
  
  Print("Generated soduku is saved in file ", m_preload);
  Print(sudoku.exportAsText());

  update(true);
}

void SudokuDialog::update(const bool init = true)
{
  int cnt = 0;
  for(int i = 0; i < m_main.ControlsTotal(); i++)
  {
    CWnd *control = m_main.Control(i);
    if(StringFind(control.Name(), "cell") >= 0)
    {
      CEdit *edit = control;
      if(cnt < SUDOKU_SIZE)
      {
        const string s = sudoku.cellAsString(cnt);
        const bool filled = (StringLen(s) > 0);
        edit.Text(s);
        if(init)
        {
          edit.ReadOnly(filled);
          edit.ColorBackground(filled ? clrBeige : clrMintCream);
          edit.Color(filled ? clrBlack : clrBlue);
        }
        else
        {
          edit.Color(sudoku.getCandidatesCount(cnt) > 0 ? clrGreen : (edit.ReadOnly() ? clrBlack : clrBlue));
          if(m_collision)
          {
            const color clr = edit.ColorBackground();
            if(clr != clrBeige && clr != clrMintCream)
            {
              edit.ColorBackground(0x400000 ^ clr);
            }
          }
        }
        edit.FontSize(getFontSize(edit.Text()));
      }
      cnt++;
    }
  }
  m_collision = false;
}

//+------------------------------------------------------------------+

void SudokuDialog::move(const int index, const ulong previous, const ulong current, const ulong postpond = (ulong)-1)
{
  int count = (int)GlobalVariableGet(m_preload + "_move_count");
  count++;
  Converter<ulong,double> cnv;
  GlobalVariableSet(m_preload + "_" + (string)count + "_cell", index);
  GlobalVariableSet(m_preload + "_" + (string)count + "_value", cnv[(previous << 32) | (uint)current]);
  GlobalVariableSet(m_preload + "_move_count", count);
  
  #ifdef SUDOKU_LOG_AUTO_MOVES
  string padding = "";
  if(changeNestingLevel > 0) StringInit(padding, changeNestingLevel * 2, ' ');
  printf("%smove [%d,%d]: %s -> %s %s", padding, index / SUDOKU_SIDE, index % SUDOKU_SIDE, sudoku.rawValueAsString(previous), sudoku.rawValueAsString(current), postpond == (ulong)-1 ? "" : "*");
  #endif
  
  if(postpond == (ulong)-1)
  {
    m_button_undo.Show();
  }
}

void SudokuDialog::undo()
{
  int count = (int)GlobalVariableGet(m_preload + "_move_count");
  if(count > 0)
  {
    int index = (int)GlobalVariableGet(m_preload + "_" + (string)count + "_cell");
    double value = GlobalVariableGet(m_preload + "_" + (string)count + "_value");
    Converter<double,ulong> cnv;
    ulong change = cnv[value];
    ulong previous = change >> 32;
    ulong current = change & 0xFFFFFFFF;
    
    ulong present = sudoku.cellByIndex(index);
    if(present == current)
    {
      GlobalVariableDel(m_preload + "_" + (string)count + "_cell");
      GlobalVariableDel(m_preload + "_" + (string)count + "_value");
      
      #ifdef SUDOKU_LOG_USER_MOVES
      printf("%d undo [%d,%d] %s -> %s", count, index / SUDOKU_SIDE, index % SUDOKU_SIDE, sudoku.valueAsString(present), sudoku.valueAsString(previous));
      #endif
      if(!apply(index, sudoku.valueAsString(previous), true))
      {
        Comment("Can't undo");
      }
      else
      {
        CEdit *edit = &m_edits[index];
        const string text = sudoku.cellAsString(index);
        updateEdit(edit, text);
      }
      
      count--;
      GlobalVariableSet(m_preload + "_move_count", count);
    }
    else
    {
      Print("Undo is not in sync [" , index / SUDOKU_SIDE, ",", index % SUDOKU_SIDE, "], expected: ", sudoku.rawValueAsString(current), ", in the cell: ", sudoku.rawValueAsString(present), ", planned: ", sudoku.rawValueAsString(previous));
      if(present == previous)
      {
        Print("Undo step is void");
        count--;
        GlobalVariableSet(m_preload + "_move_count", count);
      }
    }
    if(count == 0)
    {
      GlobalVariableDel(m_preload + "_move_count");
      m_button_undo.Hide();
    }
  }
}

const static string SudokuDialog::logger = ".log";
