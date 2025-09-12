using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates matrix manipulation using a one dimensional list.
/// </summary>
public class MatrixFunctionsStrategy : Strategy
{
	private List<decimal> _matrix = [];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_matrix = InitMatrix(3, 3, 0m);

		Set(_matrix, 0, 0, 1m);
		Set(_matrix, 1, 1, 2m);
		Set(_matrix, 2, 2, 3m);

		LogInfo(PrintMatrix(_matrix));
	}

	private static int GetRows(List<decimal> m)
	{
		return (int)m[0];
	}

	private static int GetColumns(List<decimal> m)
	{
		return (int)m[1];
	}

	private static List<decimal> InitMatrix(int rows, int columns, decimal fill)
	{
		var size = rows * columns + 2;
		var m = new List<decimal>(size);
		m.Add(rows);
		m.Add(columns);
		for (var i = 0; i < rows * columns; i++)
			m.Add(fill);
		return m;
	}

	private static int GetIndex(List<decimal> m, int row, int column)
	{
		return 2 + row * GetColumns(m) + column;
	}

	private static void Set(List<decimal> m, int row, int column, decimal value)
	{
		var idx = GetIndex(m, row, column);
		if (idx >= 2 && idx < m.Count)
			m[idx] = value;
	}

	private static decimal Get(List<decimal> m, int row, int column)
	{
		var idx = GetIndex(m, row, column);
		return idx >= 2 && idx < m.Count ? m[idx] : 0m;
	}

	private static List<decimal> GetRow(List<decimal> m, int row)
	{
		var cols = GetColumns(m);
		var res = new List<decimal>(cols);
		for (var c = 0; c < cols; c++)
			res.Add(Get(m, row, c));
		return res;
	}

	private static List<decimal> GetBody(List<decimal> m)
	{
		var res = new List<decimal>(m.Count - 2);
		for (var i = 2; i < m.Count; i++)
			res.Add(m[i]);
		return res;
	}

	private static int RowFromIndex(List<decimal> m, int index)
	{
		var cols = GetColumns(m);
		return (index - 2) / cols;
	}

	private static int ColumnFromIndex(List<decimal> m, int index)
	{
		var cols = GetColumns(m);
		return (index - 2) % cols;
	}

	private static (int row, int column) CoordinatesFromIndex(List<decimal> m, int index)
	{
		return (RowFromIndex(m, index), ColumnFromIndex(m, index));
	}

	private static int IndexFromCoordinates(List<decimal> m, int row, int column)
	{
		return GetIndex(m, row, column);
	}

	private static void AddRowTop(List<decimal> m, IList<decimal> row)
	{
		var cols = GetColumns(m);
		var rows = GetRows(m);
		for (var c = cols - 1; c >= 0; c--)
		{
			var val = c < row.Count ? row[c] : 0m;
			m.Insert(2, val);
		}
		var bodySize = rows * cols;
		var start = 2 + bodySize;
		if (m.Count > start)
			m.RemoveRange(start, m.Count - start);
	}

	private static void RemoveRows(List<decimal> m, int from, int to)
	{
		var cols = GetColumns(m);
		var rows = GetRows(m);
		var start = 2 + from * cols;
		var count = (to - from + 1) * cols;
		m.RemoveRange(start, count);
		m[0] = rows - (to - from + 1);
	}

	private static void RemoveColumns(List<decimal> m, int from, int to)
	{
		var cols = GetColumns(m);
		var rows = GetRows(m);
		var count = to - from + 1;
		for (var r = rows - 1; r >= 0; r--)
		{
			var start = 2 + r * cols + from;
			m.RemoveRange(start, count);
		}
		m[1] = cols - count;
	}

	private static void InsertRows(List<decimal> m, int at, IList<IList<decimal>> rows)
	{
		var cols = GetColumns(m);
		var insertIndex = 2 + at * cols;
		var added = 0;
		foreach (var row in rows)
		{
			for (var c = 0; c < cols; c++)
				m.Insert(insertIndex + c, c < row.Count ? row[c] : 0m);
			insertIndex += cols;
			added++;
		}
		m[0] = GetRows(m) + added;
	}

	private static void InsertColumns(List<decimal> m, int at, IList<IList<decimal>> columns)
	{
		var cols = GetColumns(m);
		var rows = GetRows(m);
		for (var r = rows - 1; r >= 0; r--)
		{
			var insertIndex = 2 + r * (cols + columns.Count) + at;
			foreach (var col in columns)
			{
				var val = r < col.Count ? col[r] : 0m;
				m.Insert(insertIndex, val);
				insertIndex++;
			}
		}
		m[1] = cols + columns.Count;
	}

	private static void AppendRows(List<decimal> m, IList<IList<decimal>> rows)
	{
		InsertRows(m, GetRows(m), rows);
	}

	private static void AppendColumns(List<decimal> m, IList<IList<decimal>> columns)
	{
		InsertColumns(m, GetColumns(m), columns);
	}

	private static List<decimal> PopRow(List<decimal> m)
	{
		var rows = GetRows(m);
		var cols = GetColumns(m);
		var start = 2 + (rows - 1) * cols;
		var res = new List<decimal>(cols);
		for (var c = 0; c < cols; c++)
			res.Add(m[start + c]);
		m.RemoveRange(start, cols);
		m[0] = rows - 1;
		return res;
	}

	private static List<decimal> PopColumn(List<decimal> m)
	{
		var rows = GetRows(m);
		var cols = GetColumns(m);
		var res = new List<decimal>(rows);
		for (var r = rows - 1; r >= 0; r--)
		{
			var idx = 2 + r * cols + (cols - 1);
			res.Insert(0, m[idx]);
			m.RemoveAt(idx);
		}
		m[1] = cols - 1;
		return res;
	}

	private static string PrintMatrix(List<decimal> m)
	{
		var rows = GetRows(m);
		var cols = GetColumns(m);
		var sb = new StringBuilder();
		sb.Append("[");
		for (var r = 0; r < rows; r++)
		{
			if (r > 0)
				sb.Append(", ");
			sb.Append("[");
			for (var c = 0; c < cols; c++)
			{
				if (c > 0)
					sb.Append(", ");
				sb.Append(Get(m, r, c));
			}
			sb.Append("]");
		}
		sb.Append("]");
		return sb.ToString();
	}
}
