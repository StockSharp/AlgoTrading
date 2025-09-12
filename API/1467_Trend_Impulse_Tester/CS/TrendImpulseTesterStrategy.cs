using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend impulse tester based on EMA trend, ADX confirmation and RSI triggers.
/// Enters in direction of trend when RSI crosses threshold.
/// </summary>
public class TrendImpulseTesterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxMin;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiUp;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private AverageDirectionalIndex _adx;
	private RelativeStrengthIndex _rsi;
	private decimal? _prevRsi;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// ADX length.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// Minimal ADX value for trend.
	/// </summary>
	public decimal AdxMin
	{
		get => _adxMin.Value;
		set => _adxMin.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI upper threshold.
	/// </summary>
	public int RsiUp
	{
		get => _rsiUp.Value;
		set => _rsiUp.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendImpulseTesterStrategy"/>.
	/// </summary>
	public TrendImpulseTesterStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length", "General")
		.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length", "General")
		.SetCanOptimize(true);

		_adxLength = Param(nameof(AdxLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Length", "ADX period", "General")
		.SetCanOptimize(true);

		_adxMin = Param(nameof(AdxMin), 18m)
		.SetDisplay("ADX Min", "Minimal ADX for trend", "General")
		.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "General")
		.SetCanOptimize(true);

		_rsiUp = Param(nameof(RsiUp), 55)
		.SetGreaterThanZero()
		.SetDisplay("RSI Up", "RSI bullish threshold", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_emaFast = default;
		_emaSlow = default;
		_adx = default;
		_rsi = default;
		_prevRsi = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, _adx, _rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal adxValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevRsi == null)
		{
				_prevRsi = rsiValue;
				return;
		}

		var uptrend = emaFast > emaSlow && adxValue > AdxMin;
		var downtrend = emaFast < emaSlow && adxValue > AdxMin;

		if (uptrend && _prevRsi <= RsiUp && rsiValue > RsiUp && Position <= 0)
		{
				BuyMarket();
		}
		else if (downtrend && _prevRsi >= 100 - RsiUp && rsiValue < 100 - RsiUp && Position >= 0)
		{
				SellMarket();
		}

		_prevRsi = rsiValue;
	}
}
