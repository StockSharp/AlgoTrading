//+------------------------------------------------------------------+
//|                                                     analysis.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include "patterns.mqh"
#include "node.mqh"
#include "wave.mqh"
#include "counted.mqh"
#include <EWM\defines.mqh>
#include "zigzags.mqh"
#include <Arrays\ArrayDouble.mqh>

class Analysis
{
  
  public:
    Counted counted1;
    Counted counted2;
    Counted counted3;
    void analysis(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level, Points *points, int v0, int v1, int v2, int v3, int v4, int v5, int f0, int f1, int f2, int f3, int f4, int f5, int num_wave1, int num_wave2);
    void analysis1(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level);
    void analysis2(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level);
    void analysis3(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level);
    void analysis4(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level);
    string get_name_subwaves(Pattern *pattern, int num_wave);
    void sorting_wave(Node *parent_node);
};

string Analysis::get_name_subwaves(Pattern *pattern, int num_wave)
{
  string name_subwaves = "";
  Subwaves_descr *subwaves_descr = pattern.subwaves.At(num_wave - 1);
  for(int i = 0; i < subwaves_descr.name_wave.Total(); i++)
  {
    name_subwaves += subwaves_descr.name_wave.At(i) + ",";
  }
  return(name_subwaves);
}

void Analysis::sorting_wave(Node *parent_node)
{
  Wave *parent_wave = parent_node.wave;
  Pattern *pattern = patterns.find(parent_wave.name);
  CArrayDouble scores;
  double score = 0;
  int count = 0;
  for(int i = 0;i < parent_node.childs.Total(); i++)
  {
    Node *node = parent_node.childs.At(i);
    scores.Clear();
    for(int j = 0; j < node.childs.Total(); j++)
    {
      Node *child_node = node.childs.At(j);
      Wave *child_wave = child_node.wave;
      Subwaves_descr *subwaves_descr = pattern.subwaves.At((int) node.text - 1);
      for(int k = 0; k < subwaves_descr.probability.Total(); k++)
      {
        if(subwaves_descr.name_wave.At(k) == child_wave.name)
        {
          Pattern *temp_pattern = patterns.find(child_wave.name);
          scores.Add(0.07 * subwaves_descr.probability.At(k) * temp_pattern.probability + 0.4 * child_wave.pattern_score);
        }
      }
    }
    node.childs.FreeMode(false);
    bool b;
    do
    {
      b = false;
      for(int j = 0;j < scores.Total() - 1; j++)
      {
        if(scores.At(j) < scores.At(j + 1))
        {
          double temp_score = scores.At(j);
          scores.Update(j, scores.At(j + 1));
          scores.Update(j + 1, temp_score);
          Node *temp_node = node.childs.At(j);
          node.childs.Update(j, node.childs.At(j + 1));
          node.childs.Update(j + 1, temp_node);
          b = true;
        }
      }
    }
    while(b);
    if(scores.Total() > 0)
    {
      count++;
      score += scores.At(0);
    }
  }
  if(count > 0)
  {
    parent_wave.pattern_score += score / count;
  }
}

void Analysis::analysis(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level, Points *points, int v0, int v1, int v2, int v3, int v4, int v5, int f0, int f1, int f2, int f3, int f4, int f5, int num_wave1, int num_wave2)
{
  string name_waves[];
  StringSplit(name_subwaves, ',', name_waves);
  int index1 = parent_wave.index[num_wave - 1];
  int index2 = parent_wave.index[num_wave];
  int j = 0;
  while(j < ArrayRange(name_waves, 0))
  {
    string name_wave = name_waves[j++];
    Pattern *pattern = patterns.find(name_wave);
    if(pattern.num_wave == num_wave1 || pattern.num_wave == num_wave2)
    {
      Wave *wave = new Wave;
      wave.name = name_wave;
      wave.level = level;
      wave.num_zigzag = points.num_zigzag;
      //wave.parent_wave = parent_wave;
      wave.value[0] = f0 <= 0 ? 0 : points.value.At(v0);
      wave.value[1] = f1 <= 0 ? 0 : points.value.At(v1);
      wave.value[2] = f2 <= 0 ? 0 : points.value.At(v2);
      wave.value[3] = f3 <= 0 ? 0 : points.value.At(v3);
      wave.value[4] = f4 <= 0 ? 0 : points.value.At(v4);
      wave.value[5] = f5 <= 0 ? 0 : points.value.At(v5);
      wave.index[0] = f0 < 0 ? 0 : (f0 == 0 ? index1 : points.index.At(v0));
      wave.index[1] = f1 < 0 ? 0 : (f1 == 0 && f2 == 1 ? index1 : (f0 == 1 && f1 == 0 ? index2 : points.index.At(v1)));
      wave.index[2] = f2 < 0 ? 0 : (f2 == 0 && f3 == 1 ? index1 : (f1 == 1 && f2 == 0 ? index2 : points.index.At(v2)));
      wave.index[3] = f3 < 0 ? 0 : (f3 == 0 && f4 == 1 ? index1 : (f2 == 1 && f3 == 0 ? index2 : points.index.At(v3)));
      wave.index[4] = f4 < 0 ? 0 : (f4 == 0 && f5 == 1 ? index1 : (f3 == 1 && f4 == 0 ? index2 : points.index.At(v4)));
      wave.index[5] = f5 < 0 ? 0 : (f5 == 0 ? index2 : points.index.At(v5));
      wave.fixed[0] = f0;
      wave.fixed[1] = f1;
      wave.fixed[2] = f2;
      wave.fixed[3] = f3;
      wave.fixed[4] = f4;
      wave.fixed[5] = f5;
      if(pattern.check(wave))
      {
        Node *parent_node = node.add(name_wave, wave);
        for(int i = 1; i <= 5; i++)
        {
          if(wave.fixed[i] >= 0)
          {
            if(wave.fixed[i] == 1 && wave.fixed[i - 1] == 1)
            {
              name_subwaves = get_name_subwaves(pattern, i);
              Node *child_node = parent_node.add((string) i);
              if( ! counted1.is(wave, i, child_node, name_subwaves))
              {
                analysis1(wave, i, child_node, name_subwaves, level + 1);
              }
            }
            if(wave.fixed[i] == 1 && wave.fixed[i - 1] == 0)
            {
              name_subwaves = get_name_subwaves(pattern, i);
              Node *child_node = parent_node.add((string) i);
              if( ! counted2.is(wave, i, child_node, name_subwaves))
              {
                analysis2(wave, i, child_node, name_subwaves, level + 1);
              }
            }
            else if(wave.fixed[i] == 0 && wave.fixed[i - 1] == 1)
            {
              name_subwaves = get_name_subwaves(pattern, i);
              Node *child_node = parent_node.add((string) i);
              if( ! counted3.is(wave, i, child_node, name_subwaves))
              {
                analysis3(wave, i, child_node, name_subwaves, level + 1);
              }
            }
          }
          sorting_wave(parent_node);
        }
      }
      else
      {
        delete wave;
      }
    }
  }
}

void Analysis::analysis1(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level)
{
  int v0;
  int v1;
  int v2;
  int v3;
  int v4;
  int v5;
  Points points;
  int index1 = parent_wave.index[num_wave - 1];
  int index2 = parent_wave.index[num_wave];
  double value1 = ((parent_wave.fixed[num_wave - 1] == 1) ? parent_wave.value[num_wave - 1] : 0);
  double value2 = ((parent_wave.fixed[num_wave] == 1) ? parent_wave.value[num_wave] : 0);
  int num_points = 4 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v0 = 0;
  v1 = 1;
  v3 = points.value.Total() - 1;
  while(v1 <= v3 - 2)
  {
    v2=v1+1;
    while(v2 <= v3-1)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 1, 1, 1, 1, -1, -1, 3, 3);
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
  num_points = 6 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v0 = 0;
  v1 = 1;
  v5 = points.value.Total() - 1;
  while(v1 <= v5 - 4)
  {
    v2 = v1 + 1;
    while(v2 <= v5 - 3)
    {
      v3 = v2 + 1;
      while(v3 <= v5 - 2)
      {
        v4= v3 + 1;
        while(v4 <= v5 - 1)
        {
          analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 1, 1, 1, 1, 1, 1, 5, 5);
          v4 = v4 + 2;
        }
        v3 = v3 + 2;
      }
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
}

void Analysis::analysis2(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level)
{
  int v0;
  int v1;
  int v2;
  int v3;
  int v4;
  int v5;
  Points points;
  int index1 = parent_wave.index[num_wave - 1];
  int index2 = parent_wave.index[num_wave];
  double value1 = ((parent_wave.fixed[num_wave - 1] == 1) ? parent_wave.value[num_wave - 1] : 0);
  double value2 = ((parent_wave.fixed[num_wave] == 1) ? parent_wave.value[num_wave] : 0);
  int num_points = 2 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v5 = points.value.Total() - 1;
  v4 = v5 - 1;
  while(v4 >= 0)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, -1, -1, 0, 1, 1, 5, 5);
    v4 = v4 - 2;
  }
  v3 = points.value.Total() - 1;
  v2 = v3- 1;
  while(v2 >= 0)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, 0, 1, 1, -1, -1, 3, 3);
    v2 = v2 - 2;
  }
  num_points = 3 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v5 = points.value.Total() - 1;
  v4 = v5 - 1;
  while(v4 >= 1)
  {
    v3 = v4 - 1;
    while(v3 >= 0)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, -1, 0, 1, 1, 1, 5, 5);
      v3 = v3 - 2;
    }
    v4 = v4 - 2;
  }
  v3 = points.value.Total()-1;
  v2 = v3 - 1;
  while(v2 >= 1)
  {
    v1 = v2 - 1;
    while(v1 >= 0)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 0, 1, 1, 1, -1, -1, 3, 3);
      v1 = v1 - 2;
    }
    v2 = v2 - 2;
  }
  num_points = 4 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v5 = points.value.Total() - 1;
  v4 = v5 - 1;
  while(v4 >= 2)
  {
    v3 = v4 - 1;
    while(v3 >= 1)
    {
      v2 = v3 - 1;
      while(v2 >= 0)
      {
        analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, 0, 1, 1, 1, 1, 5, 5);
        v2 = v2 - 2;
      }
      v3 = v3 - 2;
    }
    v4 = v4 - 2;
  }
  num_points = 5 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v5 = points.value.Total() - 1;
  v4 = v5 - 1;
  while(v4 >= 3)
  {
    v3 = v4 - 1;
    while(v3 >= 2)
    {
      v2 = v3 - 1;
      while(v2 >= 1)
      {
        v1 = v2 - 1;
        while(v1 >= 0)
        {
          analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 0, 1, 1, 1, 1, 1, 5, 5);
          v1 = v1 - 2;
        }
        v2 = v2 - 2;
      }
      v3 = v3 - 2;
    }
    v4 = v4 - 2;
  }
}

void Analysis::analysis3(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level)
{
  int v0;
  int v1;
  int v2;
  int v3;
  int v4;
  int v5;
  Points points;
  int index1 = parent_wave.index[num_wave - 1];
  int index2 = parent_wave.index[num_wave];
  double value1 = ((parent_wave.fixed[num_wave - 1] == 1) ? parent_wave.value[num_wave - 1] : 0);
  double value2 = ((parent_wave.fixed[num_wave] == 1) ? parent_wave.value[num_wave] : 0);
  int num_points = 2 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v0 = 0;
  v1 = v0 + 1;
  while(v1 <= points.value.Total() - 1)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 1, 1, 0, -1, -1, -1, 5, 3);
    v1 = v1 + 2;
  }
  num_points = 3 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v0 = 0;
  v1 = v0 + 1;
  while(v1 <= points.value.Total() - 2)
  {
    v2 = v1 + 1;
    while(v2 <= points.value.Total() - 1)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 1, 1, 1, 0, -1, -1, 5, 3);
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
  num_points = 4 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v0 = 0;
  v1 = v0 + 1;
  while(v1 <= points.value.Total() - 3)
  {
    v2 = v1 + 1;
    while(v2 <= points.value.Total() - 2)
    {
      v3 = v2 + 1;
      while(v3 <= points.value.Total() - 1)
      {
        analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 1, 1, 1, 1, 0, -1, 5, 5);
        v3 = v3 + 2;
      }
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
  num_points = 5 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v0 = 0;
  v1 = v0 + 1;
  while(v1 <= points.value.Total() - 4)
  {
    v2 = v1 + 1;
    while(v2 <= points.value.Total() - 3)
    {
      v3 = v2+ 1;
      while(v3 <= points.value.Total() - 2)
      {
        v4 = v3 + 1;
        while(v4 <= points.value.Total() - 1)
        {
          analysis(parent_wave, num_wave, node, name_subwaves,level, GetPointer(points), v0, v1, v2, v3, v4, v5, 1, 1, 1, 1, 1, 0, 5, 5);
          v4 = v4 + 2;
        }
        v3 = v3 + 2;
      }
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
}
 
void Analysis::analysis4(Wave *parent_wave, int num_wave, Node *node, string name_subwaves, int level)
{
  int v0;
  int v1;
  int v2;
  int v3;
  int v4;
  int v5;
  Points points;
  int index1 = parent_wave.index[num_wave - 1];
  int index2 = parent_wave.index[num_wave];
  double value1 = ((parent_wave.fixed[num_wave - 1] == 1) ? parent_wave.value[num_wave - 1] : 0);
  double value2 = ((parent_wave.fixed[num_wave] == 1) ? parent_wave.value[num_wave] : 0);
  int num_points = 2 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v1 = 0;
  while(v1 <= points.value.Total() - 2)
  {
    v2 = v1 + 1;
    while(v2 <= points.value.Total() - 1)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 0, 1, 1, 0, -1, -1, 5, 3);
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
  num_points = 2 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v2 = 0;
  while(v2 <= points.value.Total() - 2)
  {
    v3 = v2 + 1;
    while(v3 <= points.value.Total() - 1)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, 0, 1, 1, 0, -1, 5, 5);
      v3 = v3 + 2;
    }
    v2 = v2 + 2;
  }
  num_points = 2 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v3 = 0;
  while(v3 <= points.value.Total() - 2)
  {
    v4 = v3 + 1;
    while(v4 <= points.value.Total() - 1)
    {
      analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, -1, 0, 1, 1, 0, 5, 5);
      v4 = v4 + 2;
    }
    v3 = v3 + 2;
  }
  num_points = 3 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v1 = 0;
  while(v1 <= points.value.Total() - 3)
  {
    v2 = v1 + 1;
    while(v2 <= points.value.Total() - 2)
    {
      v3 = v2 + 1;
      while(v3 <= points.value.Total() - 1)
      {
        analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 0, 1, 1, 1, 0, -1, 5, 5);
        v3 = v3 + 2;
      }
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
  num_points = 3 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v2 = 0;
  while(v2 <= points.value.Total() - 3)
  {
    v3 = v2 + 1;
    while(v3 <= points.value.Total() - 2)
    {
      v4 = v3 + 1;
      while(v4 <= points.value.Total() - 1)
      {
        analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, 0, 1, 1, 1, 0, 5, 5);
        v4 = v4 + 2;
      }
      v3 = v3 + 2;
    }
    v2 = v2 + 2;
  }
  num_points = 4 + MAX_POINTS - 6;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v1=0;
  while(v1 <= points.value.Total() - 4)
  {
    v2 = v1 + 1;
    while(v2 <= points.value.Total() - 3)
    {
      v3 = v2 + 1;
      while(v3 <= points.value.Total() - 2)
      {
        v4 = v3 + 1;
        while(v4 <= points.value.Total() - 1)
        {
          analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 0, 1, 1, 1, 1, 0, 5, 5);
          v4 = v4 + 2;
        }
        v3 = v3 + 2;
      }
      v2 = v2 + 2;
    }
    v1 = v1 + 2;
  }
  num_points= 1 + (MAX_POINTS - 6) / 2;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v1 = 0;
  while(v1 <= points.value.Total() - 1)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, 0, 1, 0, -1, -1, -1, 5, 3);
    v1 = v1 + 1;
  }
  num_points = 1 + (MAX_POINTS - 6) / 2;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v2 = 0;
  while(v2 <= points.value.Total() - 1)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5,-1, 0, 1, 0, -1, -1, 5, 3);
    v2 =v2 + 1;
  }
  num_points = 1 + (MAX_POINTS - 6) / 2;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v3 = 0;
  while(v3 <= points.value.Total() - 1)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, -1, 0, 1, 0, -1, 5, 5);
    v3 = v3 + 1;
  }
  num_points = 1 + (MAX_POINTS - 6) / 2;
  if( ! zigzags.get_points(num_points, index1, index2, value1, value2, GetPointer(points), parent_wave.num_zigzag))
  {
    return;
  }
  v4 = 0;
  while(v4 <= points.value.Total() - 1)
  {
    analysis(parent_wave, num_wave, node, name_subwaves, level, GetPointer(points), v0, v1, v2, v3, v4, v5, -1, -1, -1, 0, 1, 0, 5, 5);
    v4 = v4 + 1;
  }
  bool b;
  node.childs.FreeMode(false);
  do
  {
    b = false;
    for(int i = 0; i < node.childs.Total() - 1; i++)
    {
      Node *child_node1 = node.childs.At(i);
      Wave *wave1 = child_node1.wave;
      Node *child_node2 = node.childs.At(i + 1);
      Wave *wave2 = child_node2.wave;
      if(wave1.pattern_score < wave2.pattern_score)
      {
        b = true;
        Node *temp_node = node.childs.At(i);
        node.childs.Update(i, node.childs.At(i + 1));
        node.childs.Update(i + 1, temp_node);
      }
    }
  }
  while(b);
}
