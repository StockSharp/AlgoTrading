using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Center of Gravity regression channel mean reversion strategy.
/// Approximates price with a polynomial regression and builds a standard deviation envelope.
/// Buys when price stays above the lower deviation band and optional stops manage risk.
/// </summary>
public class CenterOfGravityMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _barsBack;
	private readonly StrategyParam<int> _polynomialDegree;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;

	private readonly Queue<decimal> _closes = new();

	private decimal? _entryPrice;
	private decimal? _currentLowerBand;
	private decimal? _currentUpperBand;
	private decimal? _currentCenter;

	/// <summary>
	/// Initializes a new instance of the <see cref="CenterOfGravityMeanReversionStrategy"/> class.
	/// </summary>
	public CenterOfGravityMeanReversionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to build the regression channel", "General");

		_barsBack = Param(nameof(BarsBack), 125)
			.SetGreaterThanZero()
			.SetDisplay("Bars Back", "Number of historical bars used for regression", "Channel")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_polynomialDegree = Param(nameof(PolynomialDegree), 2)
			.SetGreaterThanZero()
			.SetDisplay("Polynomial Degree", "Degree of polynomial regression", "Channel");

		_stdMultiplier = Param(nameof(StdMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Std Multiplier", "Multiplier applied to close price standard deviation", "Channel");

		_stopLossDistance = Param(nameof(StopLossDistance), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Distance", "Optional stop loss distance in price units", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Distance", "Optional take profit distance in price units", "Risk");
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of historical bars used in regression.
	/// </summary>
	public int BarsBack
	{
		get => _barsBack.Value;
		set => _barsBack.Value = value;
	}

	/// <summary>
	/// Polynomial regression degree.
	/// </summary>
	public int PolynomialDegree
	{
		get => _polynomialDegree.Value;
		set => _polynomialDegree.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier applied to channel width.
	/// </summary>
	public decimal StdMultiplier
	{
		get => _stdMultiplier.Value;
		set => _stdMultiplier.Value = value;
	}

	/// <summary>
	/// Optional stop loss distance expressed in price units.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Optional take profit distance expressed in price units.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Most recent lower band value.
	/// </summary>
	public decimal? CurrentLowerBand => _currentLowerBand;

	/// <summary>
	/// Most recent upper band value.
	/// </summary>
	public decimal? CurrentUpperBand => _currentUpperBand;

	/// <summary>
	/// Most recent regression center value.
	/// </summary>
	public decimal? CurrentCenter => _currentCenter;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closes.Clear();
		_entryPrice = null;
		_currentLowerBand = null;
		_currentUpperBand = null;
		_currentCenter = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest close in the rolling window.
		UpdatePriceBuffer(candle.ClosePrice);

		if (_closes.Count < BarsBack + 1)
			return;

		// Skip trading when the regression cannot be calculated.
		if (!TryCalculateBands(out var center, out var upper, out var lower))
			return;

		_currentCenter = center;
		_currentUpperBand = upper;
		_currentLowerBand = lower;

		if (CheckLongExit(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (lower < candle.LowPrice && Position <= 0)
		{
			// Combine the order volume with any short exposure to fully reverse.
			var volume = Volume + Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private void UpdatePriceBuffer(decimal closePrice)
	{
		// Maintain a bounded queue with the most recent closes only.
		_closes.Enqueue(closePrice);

		var maxCount = BarsBack + 1;
		while (_closes.Count > maxCount)
		{
			_closes.Dequeue();
		}
	}

	private bool TryCalculateBands(out decimal center, out decimal upper, out decimal lower)
	{
		var degree = PolynomialDegree;
		var count = _closes.Count;
		var lookback = BarsBack;

		var closes = _closes.ToArray();
		var dataLength = lookback + 1;

		if (count < dataLength || degree < 1)
		{
			center = default;
			upper = default;
			lower = default;
			return false;
		}

		var size = degree + 1;
		var matrix = new double[size, size];
		var rhs = new double[size];
		var result = new double[size];
		var sumPowers = new double[2 * degree + 1];
		var data = new double[count];

		// Convert decimal closes to doubles for matrix calculations.
		for (var i = 0; i < count; i++)
		{
			data[i] = (double)closes[i];
		}

		// Pre-compute sums of powers for the normal equation matrix.
		for (var power = 0; power <= 2 * degree; power++)
		{
			double sum = 0;
			for (var n = 0; n <= lookback; n++)
			{
				sum += Math.Pow(n, power);
			}
			sumPowers[power] = sum;
		}

		for (var row = 0; row < size; row++)
		{
			for (var col = 0; col < size; col++)
			{
				matrix[row, col] = sumPowers[row + col];
			}

			double sum = 0;
			for (var n = 0; n <= lookback; n++)
			{
				var price = data[count - 1 - n];
				sum += price * Math.Pow(n, row);
			}
			rhs[row] = sum;
		}

		// Solve the linear system via Gaussian elimination to obtain the coefficients.
		if (!SolveLinearSystem(matrix, rhs, result))
		{
			center = default;
			upper = default;
			lower = default;
			return false;
		}

		var centerValue = result[0];
		if (double.IsNaN(centerValue) || double.IsInfinity(centerValue))
		{
			center = default;
			upper = default;
			lower = default;
			return false;
		}

		double total = 0;
		for (var i = count - dataLength; i < count; i++)
		{
			total += data[i];
		}
		var mean = total / dataLength;

		double variance = 0;
		for (var i = count - dataLength; i < count; i++)
		{
			var diff = data[i] - mean;
			variance += diff * diff;
		}
		variance /= dataLength;

		// Standard deviation of closes defines the envelope width.
		var std = Math.Sqrt(Math.Max(variance, 0)) * (double)StdMultiplier;
		if (double.IsNaN(std) || double.IsInfinity(std))
		{
			center = default;
			upper = default;
			lower = default;
			return false;
		}

		center = (decimal)centerValue;
		var stdDec = (decimal)std;

		upper = center + stdDec;
		lower = center - stdDec;
		return true;
	}

	private static bool SolveLinearSystem(double[,] matrix, double[] rhs, double[] result)
	{
		var size = rhs.Length;

		for (var k = 0; k < size; k++)
		{
			var pivotRow = k;
			var pivotValue = Math.Abs(matrix[k, k]);

			for (var i = k + 1; i < size; i++)
			{
				var value = Math.Abs(matrix[i, k]);
				if (value > pivotValue)
				{
					pivotValue = value;
					pivotRow = i;
				}
			}

			if (pivotValue < 1e-10)
				return false;

			if (pivotRow != k)
			{
				SwapRows(matrix, rhs, k, pivotRow);
			}

			var pivot = matrix[k, k];
			if (Math.Abs(pivot) < 1e-10)
				return false;

			for (var col = k; col < size; col++)
			{
				matrix[k, col] /= pivot;
			}
			rhs[k] /= pivot;

			for (var row = 0; row < size; row++)
			{
				if (row == k)
					continue;

				var factor = matrix[row, k];
				if (Math.Abs(factor) < 1e-12)
					continue;

				for (var col = k; col < size; col++)
				{
					matrix[row, col] -= factor * matrix[k, col];
				}
				rhs[row] -= factor * rhs[k];
			}
		}

		for (var i = 0; i < size; i++)
		{
			result[i] = rhs[i];
		}

		return true;
	}

	private static void SwapRows(double[,] matrix, double[] rhs, int rowA, int rowB)
	{
		var size = rhs.Length;

		for (var col = 0; col < size; col++)
		{
			(matrix[rowA, col], matrix[rowB, col]) = (matrix[rowB, col], matrix[rowA, col]);
		}

		(rhs[rowA], rhs[rowB]) = (rhs[rowB], rhs[rowA]);
	}

	private bool CheckLongExit(ICandleMessage candle)
	{
		// Evaluate optional protective exits using candle extremes.
		var exitPrice = _entryPrice;
		if (Position > 0 && exitPrice.HasValue)
		{
			var stopLoss = StopLossDistance;
			var takeProfit = TakeProfitDistance;
			var position = Position;

			if (stopLoss > 0m && candle.LowPrice <= exitPrice.Value - stopLoss)
			{
				SellMarket(position);
				_entryPrice = null;
				return true;
			}

			if (takeProfit > 0m && candle.HighPrice >= exitPrice.Value + takeProfit)
			{
				SellMarket(position);
				_entryPrice = null;
				return true;
			}
		}
		else if (Position <= 0)
		{
			_entryPrice = null;
		}

		return false;
	}
}
