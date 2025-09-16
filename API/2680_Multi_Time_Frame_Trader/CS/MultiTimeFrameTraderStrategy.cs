using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi time frame regression channel strategy.
/// Converts the MQL "Multi Time Frame Trader" logic to StockSharp.
/// </summary>
public class MultiTimeFrameTraderStrategy : Strategy
{
	private static readonly DataType M1Type = TimeSpan.FromMinutes(1).TimeFrame();
	private static readonly DataType M5Type = TimeSpan.FromMinutes(5).TimeFrame();
	private static readonly DataType H1Type = TimeSpan.FromHours(1).TimeFrame();

	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<int> _bars;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<bool> _useTrading;

	private RegressionChannelState _m1State = null!;
	private RegressionChannelState _m5State = null!;
	private RegressionChannelState _h1State = null!;

	private Sides? _positionSide;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Polynomial degree for the regression channel (1-3).
	/// </summary>
	public int Degree
	{
		get => _degree.Value;
		set => _degree.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier used to build the channel width.
	/// </summary>
	public decimal StdMultiplier
	{
		get => _stdMultiplier.Value;
		set => _stdMultiplier.Value = value;
	}

	/// <summary>
	/// Bars used for regression fitting and slope comparison.
	/// </summary>
	public int Bars
	{
		get => _bars.Value;
		set => _bars.Value = value;
	}

	/// <summary>
	/// Bars to shift the regression evaluation point.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Enables or disables trading logic.
	/// </summary>
	public bool UseTrading
	{
		get => _useTrading.Value;
		set => _useTrading.Value = value;
	}

	public MultiTimeFrameTraderStrategy()
	{
		_degree = Param(nameof(Degree), 1)
			.SetGreaterThanZero()
			.SetDisplay("Polynomial Degree", "Degree for regression channel", "Regression")
			.SetCanOptimize();

		_stdMultiplier = Param(nameof(StdMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Std Multiplier", "Standard deviation multiplier", "Regression")
			.SetCanOptimize();

		_bars = Param(nameof(Bars), 250)
			.SetGreaterThanZero()
			.SetDisplay("Regression Bars", "Bars for regression and slope", "Regression")
			.SetCanOptimize();

		_shift = Param(nameof(Shift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Shift", "Bars to shift regression evaluation", "Regression");

		_useTrading = Param(nameof(UseTrading), true)
			.SetDisplay("Use Trading", "Enable order execution", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, M1Type),
			(Security, M5Type),
			(Security, H1Type)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Clear manual stop/target tracking when the strategy is reset.
		_positionSide = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var degree = Math.Max(1, Math.Min(3, Degree));
		var bars = Math.Max(1, Bars);
		var shift = Math.Max(0, Math.Min(Shift, bars - 1));
		var multiplier = Math.Max(0.1m, StdMultiplier);

		// Initialize regression states for each time frame.
		_m1State = new RegressionChannelState(bars, degree, multiplier, shift);
		_m5State = new RegressionChannelState(bars, degree, multiplier, shift);
		_h1State = new RegressionChannelState(bars, degree, multiplier, shift);

		var m1Subscription = SubscribeCandles(M1Type);
		m1Subscription.Bind(ProcessM1).Start();

		var m5Subscription = SubscribeCandles(M5Type);
		m5Subscription.Bind(ProcessM5).Start();

		var h1Subscription = SubscribeCandles(H1Type);
		h1Subscription.Bind(ProcessH1).Start();
	}

	private void ProcessM1(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update the regression channel with the latest one-minute candle.
		_m1State.Process(candle);

		// Manage existing positions before evaluating fresh entry signals.
		TryManagePosition(candle);

		if (!UseTrading)
			return;

		if (!_m1State.IsReady || !_m5State.IsReady || !_h1State.IsReady)
			return;

		var slopeH1 = _h1State.Slope;
		if (slopeH1 is null)
			return;

		var m5Upper = _m5State.Upper;
		var m5Middle = _m5State.Middle;
		var m5Lower = _m5State.Lower;
		var m1Upper = _m1State.Upper;
		var m1Lower = _m1State.Lower;
		if (m5Upper is null || m5Middle is null || m5Lower is null || m1Upper is null || m1Lower is null)
			return;

		var m5High = _m5State.High;
		var m5Low = _m5State.Low;
		var m1High = _m1State.High;
		var m1Low = _m1State.Low;
		if (m5High is null || m5Low is null || m1High is null || m1Low is null)
			return;

		// Short setup: higher time frame slope is down and both M5 and M1 touch the resistance band.
		if (slopeH1 < 0m && Position >= 0m)
		{
			if (m5High >= m5Upper && m1High >= m1Upper)
			{
				var halfWidth = Math.Abs(m5Upper.Value - m5Middle.Value) / 2m;
				var stop = candle.ClosePrice + halfWidth;
				var target = m5Middle.Value;

				EnterShort(stop, target);
				return;
			}
		}

		// Long setup: higher time frame slope is up and both M5 and M1 test the support band.
		if (slopeH1 > 0m && Position <= 0m)
		{
			if (m5Low <= m5Lower && m1Low <= m1Lower)
			{
				var halfWidth = Math.Abs(m5Middle.Value - m5Lower.Value) / 2m;
				var stop = candle.ClosePrice - halfWidth;
				var target = m5Middle.Value;

				EnterLong(stop, target);
			}
		}
	}

	private void ProcessM5(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest five-minute regression data used for confirmations.
		_m5State.Process(candle);
	}

	private void ProcessH1(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track the hourly regression slope to define the dominant direction.
		_h1State.Process(candle);
	}

	private void TryManagePosition(ICandleMessage candle)
	{
		if (!UseTrading || _positionSide is null)
			return;

		// For long positions check stop loss first, then the profit target.
		if (_positionSide == Sides.Buy)
		{
			if (_stopPrice is not null && candle.LowPrice <= _stopPrice)
			{
				ExitLong();
				return;
			}

			if (_targetPrice is not null && candle.HighPrice >= _targetPrice)
				ExitLong();
		}
		// For short positions mirror the stop and target checks.
		else if (_positionSide == Sides.Sell)
		{
			if (_stopPrice is not null && candle.HighPrice >= _stopPrice)
			{
				ExitShort();
				return;
			}

			if (_targetPrice is not null && candle.LowPrice <= _targetPrice)
				ExitShort();
		}
	}

	private void EnterLong(decimal stop, decimal target)
	{
		// Market entry is issued first, then local stop/target levels are stored.
		BuyMarket();

		_positionSide = Sides.Buy;
		_stopPrice = stop;
		_targetPrice = target;
	}

	private void EnterShort(decimal stop, decimal target)
	{
		SellMarket();

		_positionSide = Sides.Sell;
		_stopPrice = stop;
		_targetPrice = target;
	}

	private void ExitLong()
	{
		SellMarket();

		// Reset tracking so a new setup can be processed immediately.
		_positionSide = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	private void ExitShort()
	{
		BuyMarket();

		_positionSide = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	private sealed class RegressionChannelState
	{
		private readonly int _length;
		private readonly int _degree;
		private readonly decimal _multiplier;
		private readonly int _shift;

		private readonly Queue<decimal> _closes = new();
		private readonly Queue<decimal> _upperHistory = new();

		public decimal? Upper { get; private set; }
		public decimal? Middle { get; private set; }
		public decimal? Lower { get; private set; }
		public decimal? Slope { get; private set; }
		public decimal? High { get; private set; }
		public decimal? Low { get; private set; }
		public bool IsReady { get; private set; }

		public RegressionChannelState(int length, int degree, decimal multiplier, int shift)
		{
			_length = length;
			_degree = Math.Max(1, Math.Min(3, degree));
			_multiplier = multiplier;
			_shift = shift;
		}

		public void Process(ICandleMessage candle)
		{
			High = candle.HighPrice;
			Low = candle.LowPrice;

			_closes.Enqueue(candle.ClosePrice);
			if (_closes.Count > _length)
				_closes.Dequeue();

			if (_closes.Count < _length)
			{
				IsReady = false;
				Upper = null;
				Middle = null;
				Lower = null;
				Slope = null;
				return;
			}

			var values = _closes.ToArray();
			var coeffs = PolyFit(values, _degree);

			var index = values.Length - 1 - Math.Min(_shift, values.Length - 1);
			var mid = PolyEval(coeffs, index);

			decimal sumSquares = 0m;
			for (var i = 0; i < values.Length; i++)
			{
				var estimate = PolyEval(coeffs, i);
				var diff = values[i] - estimate;
				sumSquares += diff * diff;
			}

			var std = (decimal)Math.Sqrt((double)(sumSquares / values.Length));
			var upper = mid + std * _multiplier;
			var lower = mid - std * _multiplier;

			_upperHistory.Enqueue(upper);
			if (_upperHistory.Count > _length + 1)
				_upperHistory.Dequeue();

			decimal? slope = null;
			if (_upperHistory.Count > _length)
				slope = upper - _upperHistory.Peek();

			Upper = upper;
			Middle = mid;
			Lower = lower;
			Slope = slope;
			IsReady = true;
		}

		private static decimal[] PolyFit(IReadOnlyList<decimal> values, int degree)
		{
			var n = values.Count;
			var order = Math.Min(degree, n - 1);
			var size = order + 1;
			var matrix = new decimal[size, size + 1];

			for (var row = 0; row < size; row++)
			{
				for (var col = 0; col < size; col++)
				{
					decimal sum = 0m;
					for (var i = 0; i < n; i++)
						sum += (decimal)Math.Pow(i, row + col);

					matrix[row, col] = sum;
				}

				decimal sumY = 0m;
				for (var i = 0; i < n; i++)
					sumY += values[i] * (decimal)Math.Pow(i, row);

				matrix[row, size] = sumY;
			}

			for (var i = 0; i < size; i++)
			{
				if (matrix[i, i] == 0m)
				{
					var swapRow = i + 1;
					while (swapRow < size && matrix[swapRow, i] == 0m)
						swapRow++;

					if (swapRow < size)
						SwapRows(matrix, i, swapRow, size + 1);
				}

				var pivot = matrix[i, i];
				if (pivot == 0m)
					continue;

				for (var j = i; j < size + 1; j++)
					matrix[i, j] /= pivot;

				for (var k = 0; k < size; k++)
				{
					if (k == i)
						continue;

					var factor = matrix[k, i];
					if (factor == 0m)
						continue;

					for (var j = i; j < size + 1; j++)
						matrix[k, j] -= factor * matrix[i, j];
				}
			}

			var coeffs = new decimal[size];
			for (var i = 0; i < size; i++)
				coeffs[i] = matrix[i, size];

			return coeffs;
		}

		private static void SwapRows(decimal[,] matrix, int a, int b, int width)
		{
			for (var col = 0; col < width; col++)
			{
				(matrix[a, col], matrix[b, col]) = (matrix[b, col], matrix[a, col]);
			}
		}

		private static decimal PolyEval(IReadOnlyList<decimal> coeffs, int x)
		{
			decimal y = 0m;
			decimal power = 1m;

			for (var i = 0; i < coeffs.Count; i++)
			{
				y += coeffs[i] * power;
				power *= x;
			}

			return y;
		}
	}
}
