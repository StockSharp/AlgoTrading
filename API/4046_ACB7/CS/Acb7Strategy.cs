namespace StockSharp.Samples.Strategies;

using System;
using System.Diagnostics;
using System.Text;

using StockSharp.Algo.Strategies;

/// <summary>
/// Port of the MetaTrader 4 script ACB7.
/// Repeats the matrix multiplication benchmark and reports the result in the log.
/// The strategy does not trade; it focuses on replicating the original computational routine.
/// </summary>
public class Acb7Strategy : Strategy
{
	private readonly StrategyParam<int> _runs;
	private readonly StrategyParam<bool> _logMatrices;

	private MatrixBuffer _matrix1 = null!;
	private MatrixBuffer _matrix2 = null!;
	private MatrixBuffer _matrix3 = null!;

	/// <summary>
	/// Number of times the matrix multiplication routine is executed on start.
	/// </summary>
	public int Runs
	{
		get => _runs.Value;
		set => _runs.Value = value;
	}

	/// <summary>
	/// Enables printing the matrices after the benchmark completes.
	/// </summary>
	public bool LogMatrices
	{
		get => _logMatrices.Value;
		set => _logMatrices.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Acb7Strategy"/> with default parameters.
	/// </summary>
	public Acb7Strategy()
	{
		_runs = Param(nameof(Runs), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Runs", "Number of matrix multiplications executed on start.", "Benchmark")
			.SetCanOptimize(true)
			.SetOptimize(100, 5000, 100);

		_logMatrices = Param(nameof(LogMatrices), true)
			.SetDisplay("Log Matrices", "Enable printing matrix contents after successful execution.", "Output");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Recreate the matrices so each run starts from the original values defined in the script.
		_matrix1 = new MatrixBuffer(1, 2, 3, 1m, 2m, 3m, 4m, 5m, 6m);
		_matrix2 = new MatrixBuffer(2, 3, 2, 7m, 8m, 9m, 10m, 11m, 12m);
		_matrix3 = new MatrixBuffer(3);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Execute the matrix processing benchmark and measure the elapsed time.
		var stopwatch = Stopwatch.StartNew();
		var success = false;

		for (var i = 0; i < Runs; i++)
		{
			// Each iteration mirrors the original loop that repeatedly called afi.MatrixProcessing.
			success = ProcessMatrices();
		}

		stopwatch.Stop();

		if (success)
		{
			if (LogMatrices)
			{
				// Log the result matrix followed by the original operands, matching the MQL script output order.
				LogMatrix(_matrix3);
				LogMatrix(_matrix2);
				LogMatrix(_matrix1);
			}

			this.AddInfoLog($"Success: {Runs} runs in {stopwatch.ElapsedMilliseconds} ms.");
		}
		else
		{
			this.AddInfoLog("Failure");
		}

		// Preserve the trailing blank alert emitted by the script for faithful logging.
		this.AddInfoLog(string.Empty);

		Stop();
	}

	// Equivalent to the afi.MatrixProcessing function from the script.
	private bool ProcessMatrices()
	{
		if (_matrix1.Columns != _matrix2.Rows)
			return false;

		_matrix3.Resize(_matrix1.Rows, _matrix2.Columns);

		for (var row = 1; row <= _matrix3.Rows; row++)
		{
			for (var column = 1; column <= _matrix3.Columns; column++)
			{
				var cell = 0m;

				for (var k = 1; k <= _matrix1.Columns; k++)
				{
					// Multiply the current row of Matrix 1 by the matching column of Matrix 2.
					cell += _matrix1.GetCell(row, k) * _matrix2.GetCell(k, column);
				}

				_matrix3.SetCell(row, column, cell);
			}
		}

		return true;
	}

	// Equivalent to the afr.MatrixPrint helper in the original code.
	private void LogMatrix(MatrixBuffer matrix)
	{
		if (matrix.Rows == 0 || matrix.Columns == 0)
		{
			this.AddInfoLog($"Matrix ID: {matrix.Id} Rows={matrix.Rows} Columns={matrix.Columns}");
			return;
		}

		for (var row = matrix.Rows; row >= 1; row--)
		{
			var builder = new StringBuilder();

			for (var column = 1; column <= matrix.Columns; column++)
			{
				builder.Append(" (");
				builder.Append(row);
				builder.Append(',');
				builder.Append(column);
				builder.Append(")= ");
				builder.Append(matrix.GetCell(row, column));
				builder.Append(';');
			}

			this.AddInfoLog(builder.ToString());
		}

		this.AddInfoLog($"Matrix ID: {matrix.Id} Rows={matrix.Rows} Columns={matrix.Columns}");
	}

	// Lightweight matrix container that keeps the same access pattern as the script helper routines.
	private sealed class MatrixBuffer
	{
		private decimal[] _values;

		public MatrixBuffer(int id, int rows, int columns, params decimal[] values)
		{
			Id = id;
			Rows = rows;
			Columns = columns;
			_values = CreateStorage(rows, columns, values);
		}

		public MatrixBuffer(int id)
		{
			Id = id;
			Rows = 0;
			Columns = 0;
			_values = Array.Empty<decimal>();
		}

		public int Id { get; }

		public int Rows { get; private set; }

		public int Columns { get; private set; }

		public decimal GetCell(int row, int column)
		{
			return _values[(row - 1) * Columns + (column - 1)];
		}

		public void SetCell(int row, int column, decimal value)
		{
			_values[(row - 1) * Columns + (column - 1)] = value;
		}

		public void Resize(int rows, int columns)
		{
			Rows = rows;
			Columns = columns;

			if (rows == 0 || columns == 0)
			{
				_values = Array.Empty<decimal>();
				return;
			}

			_values = new decimal[rows * columns];
		}

		private static decimal[] CreateStorage(int rows, int columns, decimal[] values)
		{
			if (rows == 0 || columns == 0)
			{
				if (values.Length == 0)
					return Array.Empty<decimal>();

				var copy = new decimal[values.Length];
				Array.Copy(values, copy, values.Length);
				return copy;
			}

			var storage = new decimal[rows * columns];
			var length = Math.Min(storage.Length, values.Length);

			if (length > 0)
				Array.Copy(values, storage, length);

			return storage;
		}
	}
}
