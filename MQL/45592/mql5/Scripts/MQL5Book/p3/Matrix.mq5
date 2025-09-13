//+------------------------------------------------------------------+
//|                                                       Matrix.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Matrix class with operator overloads                             |
//+------------------------------------------------------------------+
class Matrix
{
protected:
   double m[];
   int rows;
   int columns;
   
   // check matrix compatibility for summation
   bool isCompatible(const Matrix &other)
   {
      const bool check = rows == other.rows && columns == other.columns;
      if(!check) Print("Check failed: matrices must be of the same sizes");
      return check;
   }
   
   // check matrix compatibility for multiplication
   bool isMCompatible(const Matrix &other)
   {
      const bool check = columns == other.rows;
      if(!check) Print("Check failed: this.columns should be equal to other.rows");
      return check;
   }
   
   void assign(const int r, const int c, const double v)
   {
      m[r * columns + c] = v;
   }
   
public:
   Matrix(const Matrix &other) : rows(other.rows), columns(other.columns)
   {
      ArrayCopy(m, other.m);
   }

   Matrix(const int r, const int c) : rows(r), columns(c)
   {
      ArrayResize(m, rows * columns);
      ArrayInitialize(m, 0);
   }

   class MatrixRow
   {
   protected:
      const Matrix *owner;
      const int row;
      
   public:
      class MatrixElement
      {
      protected:
         const MatrixRow *row;
         const int column;
         
      public:
         MatrixElement(const MatrixRow &mr, const int c) : row(&mr), column(c) { }
         MatrixElement(const MatrixElement &other) : row(other.row), column(other.column) { }
         
         // return value of this element as double
         double operator~() const
         {
            return row.owner.m[row.row * row.owner.columns + column];
         }
         
         // assign double value to this element
         double operator=(const double v)
         {
            row.owner.m[row.row * row.owner.columns + column] = v;
            return v;
         }
      };
   
      MatrixRow(const Matrix &m, const int r) : owner(&m), row(r) { }
      MatrixRow(const MatrixRow &other) : owner(other.owner), row(other.row) { }
      
      // access matrix element as object to allow subsequent edition/assignment
      // note the type of index is int
      MatrixElement operator[](int c)
      {
         return MatrixElement(this, c);
      }

      // access matrix element as double value for immediate reading
      // note the type of index is uint
      double operator[](uint c)
      {
         return owner.m[row * owner.columns + c];
      }
   };
   

   Matrix *operator=(const double &a[])
   {
      if(ArraySize(a) == ArraySize(m))
      {
         ArrayCopy(m, a);
      }
      return &this;
   }

   Matrix *operator=(const Matrix &other) // optional, required for builds pre-2715
   {
      ArrayCopy(m, other.m);
      ArrayResize(m, ArraySize(other.m));
      rows = other.rows;
      columns = other.columns;
      return &this;
   }

   MatrixRow operator[](int r)
   {
      return MatrixRow(this, r);
   }

   Matrix *operator+=(const Matrix &other)
   {
      if(!isCompatible(other)) return &this;
      for(int i = 0; i < rows * columns; ++i)
      {
         m[i] += other.m[i];
      }
      return &this;
   }

   Matrix operator+(const Matrix &other) const
   {
      Matrix temp(this);
      return temp += other;
   }

   Matrix operator-(const Matrix &other) const
   {
      Matrix temp(this);
      return temp += other * -1.0;
   }

   Matrix *operator*=(const Matrix &other)
   {
      // check multiplication prerequisite: this.columns == other.rows
      if(!isMCompatible(other)) return &this;
      
      // declare temporary matrix for calculations
      // with resulting size this.rows * other.columns
      Matrix temp(rows, other.columns);
      
      for(int r = 0; r < temp.rows; ++r)
      {
         for(int c = 0; c < temp.columns; ++c)
         {
            double t = 0;
            // sum up products of i-th elements
            // from r-th row of this and c-th column of other matrices
            for(int i = 0; i < columns; ++i)
            {
               t += m[r * columns + i] * other.m[i * other.columns + c];
            }
            temp.assign(r, c, t);
         }
      }
      // copy result into current object
      this = temp; // call overloaded assignment for current object
      return &this;
   }

   Matrix operator*(const Matrix &other) const
   {
      Matrix temp(this);
      return temp *= other;
   }

   Matrix *operator*=(const double v)
   {
      for(int i = 0; i < ArraySize(m); ++i)
      {
         m[i] *= v;
      }
      return &this;
   }

   Matrix operator*(const double v) const
   {
      Matrix temp(this);
      return temp *= v;
   }
   
   bool operator==(const Matrix &other) const
   {
      return ArrayCompare(m, other.m) == 0;
   }

   bool operator!=(const Matrix &other) const
   {
      return !(this == other);
   }

   void print() const
   {
      ArrayPrint(m);
   }
};

//+------------------------------------------------------------------+
//| Helper identity matrix class                                     |
//+------------------------------------------------------------------+
class MatrixIdentity : public Matrix
{
public:
   MatrixIdentity(const int n) : Matrix(n, n)
   {
      for(int i = 0; i < n; ++i)
      {
         m[i * rows + i] = 1;
      }
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Matrix m(2, 3), n(3, 2);
   MatrixIdentity p(2);
   
   double ma[] = {-1,  0, -3,
                   4, -5,  6};
   double na[] = {7,  8,
                  9,  1,
                  2,  3};
   m = ma;
   n = na;

   m[0][0] = m[0][(uint)0] + 2; // can read and write matrix elements
   m[0][1] = ~m[0][1] + 2;      // equivalent
   
   Matrix r = m * n + p;                    // expression
   Matrix r2 = m.operator*(n).operator+(p); // equivalent
   Print(r == r2); // true

   m.print(); // 1.00000  2.00000 -3.00000  4.00000 -5.00000  6.00000
   n.print(); // 7.00000 8.00000 9.00000 1.00000 2.00000 3.00000
   r.print(); // 20.00000  1.00000 -5.00000  46.00000
}
//+------------------------------------------------------------------+
