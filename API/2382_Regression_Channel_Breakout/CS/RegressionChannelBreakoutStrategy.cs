using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading regression channel breakouts.
/// </summary>
public class RegressionChannelBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailingStart;
	private readonly StrategyParam<decimal> _trailingStep;

	private LinearRegression _regression;
	private StandardDeviation _stdev;

	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _shortStop;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }
	public decimal TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }

	public RegressionChannelBreakoutStrategy()
	{
		_length = Param(nameof(Length), 250)
			.SetDisplay("Length", "Number of candles for regression and deviation", "Common")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_deviation = Param(nameof(Deviation), 2m)
			.SetDisplay("Deviation", "Standard deviation multiplier", "Common")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Common");

		_useTrailing = Param(nameof(UseTrailing), false)
			.SetDisplay("Use Trailing", "Enable trailing stop logic", "Trailing");

		_trailingStart = Param(nameof(TrailingStart), 30m)
			.SetDisplay("Trailing Start", "Profit required before trailing starts", "Trailing");

		_trailingStep = Param(nameof(TrailingStep), 30m)
			.SetDisplay("Trailing Step", "Distance between price and trailing stop", "Trailing");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_regression = new LinearRegression { Length = Length };
		_stdev = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_regression, _stdev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal reg, decimal dev)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var middle = reg;
		var upper = reg + Deviation * dev;
		var lower = reg - Deviation * dev;
		var price = candle.ClosePrice;

		if (price >= middle && Position > 0)
		{
			SellMarket();
			ResetTrailing();
		}
		else if (price <= middle && Position < 0)
		{
			BuyMarket();
			ResetTrailing();
		}
		else
		{
			if (candle.LowPrice <= lower && Position <= 0)
			{
				BuyMarket();
				_entryPrice = price;
				ResetTrailing();
			}
			else if (candle.HighPrice >= upper && Position >= 0)
			{
				SellMarket();
				_entryPrice = price;
				ResetTrailing();
			}
		}

		if (UseTrailing)
			ApplyTrailing(price);
	}

	private void ApplyTrailing(decimal price)
	{
		if (Position > 0)
		{
			if (_entryPrice == 0m)
				_entryPrice = price;

			var profit = price - _entryPrice;

			if (profit >= TrailingStart)
			{
				var stop = price - TrailingStep;

				if (_longStop == 0m || stop > _longStop)
					_longStop = stop;
			}

			if (_longStop != 0m && price <= _longStop)
			{
				SellMarket();
				ResetTrailing();
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice == 0m)
				_entryPrice = price;

			var profit = _entryPrice - price;

			if (profit >= TrailingStart)
			{
				var stop = price + TrailingStep;

				if (_shortStop == 0m || stop < _shortStop)
					_shortStop = stop;
			}

			if (_shortStop != 0m && price >= _shortStop)
			{
				BuyMarket();
				ResetTrailing();
			}
		}
		else
		{
			ResetTrailing();
		}
	}

	private void ResetTrailing()
	{
		_entryPrice = 0m;
		_longStop = 0m;
		_shortStop = 0m;
	}
}
