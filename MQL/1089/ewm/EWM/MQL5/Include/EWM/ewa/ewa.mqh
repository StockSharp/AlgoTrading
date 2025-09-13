//+------------------------------------------------------------------+
//|                                                          ewa.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include "zigzags.mqh"
#include "node.mqh"
#include "analysis.mqh"
#include <EWM\ewm\wave_labels.mqh>

class Ewa
{
  public:
    CChartObjectLabel lbl;
    int level_max;
    Wave_labels *wave_labels;
    Analysis analysis;
    Node first_node;
    string selected_name;
    bool is_analysis;
    void Ewa(Wave_labels *wave_labels);
    void begin_analysis(bool pos, Wave_labels *wave_labels);
    void fill_wave_labels(Node *node, string n, int level, Wave_labels *wave_labels);
    void get_name_wave(string s, Wave_labels *wave_labels);
    Node *find_selected_node(string s, Wave_labels *wave_labels);
    void select(string s, Wave_labels *wave_labels);
    void clear_waves(Wave_labels *wave_labels);
    void parse();
    void analysis_left(Wave_labels *wave_labels);
    void analysis_right(Wave_labels *wave_labels);
    bool set_grey_color();
    void convert();
    void on_init();
    void on_deinit(int reason);
    void clear();
};

void Ewa::Ewa(Wave_labels *wave_labels)
{
  this.wave_labels = wave_labels;
  lbl.Create(0, "wave" ,0 , x_distance + LABELS * interval + 200, y_distance);
  lbl.Description("");
}

void Ewa::on_init()
{

}

void Ewa::on_deinit(int reason)
{
  convert();
  first_node.clear();
  analysis.counted1.Clear();
  analysis.counted2.Clear();
  analysis.counted3.Clear();
  zigzags.remove();
  if(reason != REASON_CHARTCHANGE)
  {
    rules.clear();
  }
}

void Ewa::clear()
{
  first_node.clear();
  analysis.counted1.Clear();
  analysis.counted2.Clear();
  analysis.counted3.Clear();
  clear_waves(GetPointer(wave_labels));
  zigzags.remove();
  lbl.Description("");
}

void Ewa::convert()
{
  is_analysis = false;
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave = wave_labels.At(i);
    if(StringFind(wave.Name(), NAME_AUTO_WAVE) != -1)
    {
      wave.Name((string) wave.get_level() + SEPARATOR + wave.get_text() + SEPARATOR + wave.get_group() + SEPARATOR + NAME_WAVE);
    }
    else
    {
      wave.Color(wave.get_color());
      wave.Selectable(true);
    }
  }
  first_node.clear();
  analysis.counted1.Clear();
  analysis.counted2.Clear();
  analysis.counted3.Clear();
  zigzags.remove();
  lbl.Description("");
}

bool Ewa::set_grey_color()
{
  convert();
  int selected_count = 0;
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave = wave_labels.At(i);
    if(wave.Selected())
    {
      selected_count++;
    }
    wave.Color(clrGray);
    wave.Selectable(false);  
  }
  if(selected_count > 1)
  {
    MessageBox(MSG_SELECTED_MORE, NULL, MB_ICONSTOP);
    convert();
    return(false);
  }
  else if(selected_count == 0)
  {
    if(wave_labels.Total() > 0 && MessageBox(MSG_REMOVE, NULL, MB_OKCANCEL | MB_ICONWARNING) == IDCANCEL)
    {
      convert();
      return(false);
    }
    else
    {
      wave_labels.FreeMode(true);
      wave_labels.Clear();
      ChartRedraw();
    }
  }
  return(true);
}

void Ewa::analysis_left(Wave_labels *wave_labels)
{
  if(MessageBox(MSG_BEGIN_ANALYSIS, NULL, MB_YESNO | MB_ICONQUESTION) == IDNO)
  {
    return;
  }
  if(set_grey_color())
  {
    begin_analysis(LEFT, wave_labels);
  }
}

void Ewa::analysis_right(Wave_labels *wave_labels)
{
  if(MessageBox(MSG_BEGIN_ANALYSIS, NULL, MB_YESNO | MB_ICONQUESTION) == IDNO)
  {
    return;
  }
  if(set_grey_color())
  {
    begin_analysis(RIGHT, wave_labels);
  }
}

void Ewa::begin_analysis(bool pos, Wave_labels *wave_labels)
{
  datetime date1 = 0;
  datetime date2 = 0;
  datetime times[];
  int type = TYPE4;
  int index = wave_labels.get_selected();
  int temp_index;
  if(index >= 0)
  {
    Wave_label *wave_label = wave_labels.At(index);
    wave_label.Selected(false);
    level_max = wave_label.get_level() - 1;
  }
  if(index == -1)
  {
    while(CopyTime(_Symbol, _Period, Bars(_Symbol, _Period) - 1, 1, times) != 1)
    {
      Sleep(DELAY);
    }
    date1 = times[0];
    while(CopyTime(_Symbol, _Period, 0, 1, times) != 1)
    {
      Sleep(DELAY);
    }
    date2 = times[0];
    level_max = descrs.Total() - 2;
  }
  else if(pos == LEFT)
  {
    Wave_label *wave_label = wave_labels.At(index);
    date2 = wave_label.get_time(); 
    int total = wave_labels.Total();
    for(int i = index; i >= 0; i--)
    {
      Wave_label *temp_wave_label = wave_labels.At(i);
      if(temp_wave_label.get_time() < wave_label.get_time())
      {
        date1 = temp_wave_label.get_time();
        type = TYPE1;
        temp_index = i;
        break;
      }
    }
    if(date1 == 0)
    {
      while(CopyTime(_Symbol, _Period, Bars(_Symbol, _Period) - 1, 1, times) != 1)
      {
        Sleep(DELAY);
      }
      date1 = times[0];
      type = TYPE2;
    }
  }
  else if(pos == RIGHT)
  {
    Wave_label *wave_label = wave_labels.At(index);
    date1 = wave_label.get_time(); 
    int total = wave_labels.Total();
    for(int i = index; i < total; i++)
    {
      Wave_label *temp_wave_label = wave_labels.At(i);
      if(temp_wave_label.get_time() > wave_label.get_time())
      {
        date2 = temp_wave_label.get_time();
        type = TYPE1;
        temp_index = i;
        break;
      }
    }
    if(date2 == 0)
    {
      while(CopyTime(_Symbol, _Period, 0, 1, times) != 1)
      {
        Sleep(DELAY);
      }
      date2 = times[0];
      type = TYPE3;
    }
  }
  int bars = Bars(_Symbol, _Period, date1, date2);
  while(CopyRates(_Symbol, _Period, date1, date2, rates) != bars)
  {
    Sleep(DELAY);
  }
  zigzags.create(0, bars - 1, type);
  Wave *wave = new Wave;
  wave.num_zigzag = zigzags.Total() - 1;
  wave.index[0] = 0;
  wave.index[1] = bars - 1;
  wave.value[0] = 0;
  wave.value[1] = 0;
  wave.trend = "Undefined";
  first_node.wave = wave;
  first_node.text = "First node";
  string name_waves;
  for(int i = 0; i < patterns.Total(); i++)
  {
    Pattern *pattern = patterns.At(i);
    name_waves += pattern.name + ",";
  }
  switch(type)
  {
    case TYPE1:
      analysis.analysis1(wave, 1, GetPointer(first_node), name_waves, 0);
      break;
    case TYPE2:
      analysis.analysis2(wave, 1, GetPointer(first_node), name_waves, 0);
      break;
    case TYPE3:
      analysis.analysis3(wave, 1, GetPointer(first_node), name_waves, 0);
      break;    
    case TYPE4:
      analysis.analysis4(wave, 1, GetPointer(first_node), name_waves, 0);
      break;
  }
  fill_wave_labels(GetPointer(first_node), "", 0, wave_labels);
  is_analysis = true;
  wave_labels.sort();
  wave_labels.correct();
  get_name_wave("", wave_labels);
  MessageBox(MSG_ANALYSIS_COMPLETED, NULL, MB_ICONEXCLAMATION);
}


void Ewa::fill_wave_labels(Node *node, string n, int level, Wave_labels *wave_labels)
{
  if(level > level_max)
  {
    return;
  }
  if(node.childs.Total() > 0)
  {
    Node *child_node;
    bool selected = false;
    for(int i = 1; i < node.childs.Total(); i++)
    {
      child_node = node.childs.At(i);
      if(child_node.selected)
      {
        selected = true;
        n += SEPARATOR + (string) i;
        break;
      }
    }
    if( ! selected)
    {
      n += SEPARATOR + "0";
      child_node = node.childs.At(0);
    }
    Wave *wave = child_node.wave;
    string group = (string) (wave_labels.max_group++ + 1);
    for(int i = 1, j = 0; i <= 5; i++)
    {
      if(wave.fixed[i] == 1)
      {
        Wave_label *wave_label = new Wave_label;
        Pattern *pattern = patterns.find(wave.name);
        Subwaves_descr *subwaves_descr = pattern.subwaves.At(i - 1);
        int wave_level = level_max - level;
        string map = n + SEPARATOR + (string) j++;
        for(int k = 0; k < LABELS; k++)
        {
          if(etalon[k] == subwaves_descr.wave_label)
          {
            Descr *descr = descrs.At(wave_level);
            string name = (string) wave_level + SEPARATOR + descr.labels[k] + SEPARATOR + group + SEPARATOR + NAME_AUTO_WAVE + map;
            wave_label.create(name, descr.labels[k], rates[wave.index[i]].time, wave.value[i]);
            if(map == selected_name)
            {
              wave_label.Selected(true);
            }
            wave_label.set_time(rates[wave.index[i]].time);
            wave_label.set_price(wave.value[i]);
            if(wave.value[i] == rates[wave.index[i]].high)
            {
              wave_label.set_pos(TOP);
            }
            else
            {
              wave_label.set_pos(BOTTOM);
            }
            wave_label.move(EVERY_BAR);
            break;
          }
        }
        wave_labels.Add(wave_label);
       }
    }
    for(int i=0; i < child_node.childs.Total(); i++)
    {
      fill_wave_labels(child_node.childs.At(i), n + "_" + IntegerToString(i), level + 1, wave_labels);
    }
  }
}
  
void Ewa::get_name_wave(string s, Wave_labels *wave_labels)
{
  if( ! is_analysis)
  {
    return;
  }
  bool selected=false;
  Node *node = find_selected_node(s, wave_labels);
  string name = "Undefined";
  int num = -1;
  Wave *wave;
  if(node.childs.Total() > 0)
  {
    for(int i = 0; i < node.childs.Total(); i++)
    {
      Node *temp_node = node.childs.At(i);
      if(temp_node.selected)
      {
        wave = temp_node.wave;
        name = wave.name;
        selected = true;
        num = i;
        break;
      }
    }
    if( ! selected)
    {
      Node *temp_node = node.childs.At(0);
      wave = temp_node.wave;
      name = wave.name;
      num = 0;
    }
  }
  lbl.Description((string) (num + 1) + " of " + (string) node.childs.Total() + ": " + name);
  lbl.Selectable(false);
  lbl.Anchor(ANCHOR_CENTER);
  int selected_label = wave_labels.get_selected();
  int level = -1;
  if(selected_label >= 0)
  {
    Wave_label *wave_label = wave_labels.At(selected_label);
    level = wave_label.get_level();
  }
  else
  {
    for(int i = 0; i < wave_labels.Total(); i++)
    {
      Wave_label *wave_label = wave_labels.At(i);
      if(StringFind(wave_label.Name(), NAME_AUTO_WAVE) >= 0)
      {
        if(wave_label.get_level() > level)
        {
          level = wave_label.get_level();
        }
      }
    }
    level++; 
  }
  Descr *descr = descrs.At(level);
  lbl.Font(descr.font);
  lbl.FontSize(descr.font_size);
  lbl.Color(descr.clr);
}

Node *Ewa::find_selected_node(string s, Wave_labels *wave_labels)
{
  bool selected = false;
  string result[];
  for(int i = wave_labels.Total() - 1; i >= 0; i--)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      selected = true;
      StringSplit(wave_label.Name(), '_', result);
      break;
    }
  }
  if( ! selected)
  {
    selected_name = "";
    return(GetPointer(first_node));
  }
  Node *node = GetPointer(first_node);
  int n = 0;
  int i = 4;
  selected_name = "";
  while(i < ArrayRange(result, 0))
  {
    n = (int) result[i++];
    selected_name += SEPARATOR + (string) n;
    node = node.childs.At(n);
  }
  if((s == "PrevRight" || s == "NextRight") && node.parent.childs.Total() > n + 1)
  {
    node = node.parent.childs.At(n + 1);
  }
  return(node);
}

void Ewa::select(string s, Wave_labels *wave_labels)
{
  if( ! is_analysis)
  {
    return;
  }
  Node *selected_node = find_selected_node(s, wave_labels);
  Node *temp_node = NULL;
  bool selected = false;
  int total = selected_node.childs.Total();
  for(int i=0; i < total; i++)
  {
    temp_node = selected_node.childs.At(i);
    if(temp_node.selected)
    {
      temp_node.selected=false;
      if((s=="PrevLeft" || s=="PrevRight") && i > 0)
      {
        temp_node=selected_node.childs.At(i - 1);
      }
      else if((s == "NextLeft" || s == "NextRight") && i < total - 1)
      {
        temp_node = selected_node.childs.At(i + 1);
      }
      temp_node.selected=true;
      selected = true;
      break;
    }
  }
  if( ! selected && selected_node.childs.Total() > 0)
  {
    temp_node = selected_node.childs.At(0);
    if(selected_node.childs.Total() > 1)
    {
      temp_node = selected_node.childs.At(1);
    }
    temp_node.selected = true;
  }
  clear_waves(wave_labels);
  fill_wave_labels(GetPointer(first_node), "", 0, wave_labels);
  wave_labels.sort();
  wave_labels.correct();
}

void Ewa::clear_waves(Wave_labels *wave_labels)
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(StringFind(wave_label.Name(), NAME_AUTO_WAVE) != -1)
    {
      wave_labels.Delete(i);
      i--;
    }
  }
}