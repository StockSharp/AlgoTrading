using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Chaikin Volatility Stochastic turning points.
/// </summary>
public class ChaikinVolatilityStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _wmaLength;
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<bool> _enableShorts;

	private ExponentialMovingAverage _rangeEma = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private WeightedMovingAverage _wma = null!;

	private decimal? _prev;
	private decimal? _prevPrev;

	public ChaikinVolatilityStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculation.", "General");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetDisplay("EMA Length", "Length for smoothing high-low range.", "Indicator")
			.SetCanOptimize(true);

		_stochLength = Param(nameof(StochLength), 5)
			.SetDisplay("Stochastic Length", "Lookback for stochastic calculation.", "Indicator")
			.SetCanOptimize(true);

		_wmaLength = Param(nameof(WmaLength), 5)
			.SetDisplay("WMA Length", "Weighted moving average period.", "Indicator")
			.SetCanOptimize(true);

		_enableLongs = Param(nameof(EnableLongs), true)
			.SetDisplay("Enable Longs", "Allow long entries.", "Trading");

		_enableShorts = Param(nameof(EnableShorts), true)
			.SetDisplay("Enable Shorts", "Allow short entries.", "Trading");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }
	public int WmaLength { get => _wmaLength.Value; set => _wmaLength.Value = value; }
	public bool EnableLongs { get => _enableLongs.Value; set => _enableLongs.Value = value; }
	public bool EnableShorts { get => _enableShorts.Value; set => _enableShorts.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rangeEma = new ExponentialMovingAverage { Length = EmaLength };
		_highest = new Highest { Length = StochLength };
		_lowest = new Lowest { Length = StochLength };
		_wma = new WeightedMovingAverage { Length = WmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var emaValue = _rangeEma.Process(new DecimalIndicatorValue(_rangeEma, range, candle.OpenTime));

		if (!emaValue.IsFinal)
			return;

		var high = _highest.Process(emaValue);
		var low = _lowest.Process(emaValue);

		if (high is not decimal hh || low is not decimal ll || hh == ll)
			return;

		var percent = (emaValue.ToDecimal() - ll) / (hh - ll) * 100m;
		var smooth = _wma.Process(new DecimalIndicatorValue(_wma, percent, candle.OpenTime));

		if (!smooth.IsFinal)
			return;

		var current = smooth.ToDecimal();

		if (_prev.HasValue && _prevPrev.HasValue)
		{
			var wasRising = _prev.Value > _prevPrev.Value;
			var isFalling = current < _prev.Value;
			var wasFalling = _prev.Value < _prevPrev.Value;
			var isRising = current > _prev.Value;

			if (wasRising && isFalling)
			{
				if (EnableShorts && Position < 0)
					BuyMarket(Math.Abs(Position));

				if (EnableLongs && Position <= 0)
					BuyMarket(Volume);
			}
			else if (wasFalling && isRising)
			{
				if (EnableLongs && Position > 0)
					SellMarket(Position);

				if (EnableShorts && Position >= 0)
					SellMarket(Volume);
			}
		}

		_prevPrev = _prev;
		_prev = current;
	}
}
