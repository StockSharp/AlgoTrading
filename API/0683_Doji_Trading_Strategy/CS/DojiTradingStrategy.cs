using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji pattern strategy with EMA filter and trailing stop.
/// </summary>
public class DojiTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _tolerance;
	private readonly StrategyParam<int> _stopBars;
	private readonly StrategyParam<decimal> _trailTriggerPercent;
	private readonly StrategyParam<decimal> _trailOffsetPercent;

	private ExponentialMovingAverage _ema;
	private Lowest _lowest;

	private decimal? _entryPrice;
	private decimal _highestPrice;
	private decimal? _stopPrice;
	private bool _trailingActive;

	/// <summary>
	/// Initializes a new instance of the <see cref="DojiTradingStrategy"/> class.
	/// </summary>
	public DojiTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period for EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_tolerance = Param(nameof(Tolerance), 0.05m)
			.SetRange(0.01m, 0.10m)
			.SetDisplay("Doji Tolerance", "Maximum body size as % of close", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(0.02m, 0.08m, 0.01m);

		_stopBars = Param(nameof(StopBars), 450)
			.SetGreaterThanZero()
			.SetDisplay("Stop Length", "Bars for stop loss", "Risk");

		_trailTriggerPercent = Param(nameof(TrailTriggerPercent), 1m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Trailing Threshold %", "Profit percent to activate trailing", "Risk");

		_trailOffsetPercent = Param(nameof(TrailOffsetPercent), 0.5m)
			.SetRange(0.1m, 3m)
			.SetDisplay("Trailing Offset %", "Distance of trailing stop", "Risk");
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Maximum allowed difference between open and close as percent.
	/// </summary>
	public decimal Tolerance
	{
		get => _tolerance.Value;
		set => _tolerance.Value = value;
	}

	/// <summary>
	/// Bars to calculate stop loss.
	/// </summary>
	public int StopBars
	{
		get => _stopBars.Value;
		set => _stopBars.Value = value;
	}

	/// <summary>
	/// Profit percent to activate trailing stop.
	/// </summary>
	public decimal TrailTriggerPercent
	{
		get => _trailTriggerPercent.Value;
		set => _trailTriggerPercent.Value = value;
	}

	/// <summary>
	/// Distance of trailing stop in percent.
	/// </summary>
	public decimal TrailOffsetPercent
	{
		get => _trailOffsetPercent.Value;
		set => _trailOffsetPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = null;
		_highestPrice = default;
		_stopPrice = null;
		_trailingActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_lowest = new Lowest { Length = StopBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var lowestVal = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.ServerTime));
		var sl = lowestVal.IsFormed ? lowestVal.ToDecimal() : (decimal?)null;

		var close = candle.ClosePrice;
		var isDoji = Math.Abs(candle.OpenPrice - close) <= Tolerance * close;

		if (Position == 0)
		{
			if (isDoji && close > emaValue)
			{
				BuyMarket();
				_entryPrice = close;
				_highestPrice = close;
				_stopPrice = sl;
				_trailingActive = false;
			}

			return;
		}

		_highestPrice = Math.Max(_highestPrice, candle.HighPrice);

		if (!_trailingActive && _entryPrice is decimal entry && close >= entry * (1 + TrailTriggerPercent / 100))
			_trailingActive = true;

		if (_trailingActive)
		{
			var trail = _highestPrice * (1 - TrailOffsetPercent / 100);
			_stopPrice = sl.HasValue ? Math.Max(sl.Value, trail) : trail;
		}
		else if (sl.HasValue)
		{
			_stopPrice = sl.Value;
		}

		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			_entryPrice = null;
			_stopPrice = null;
			_trailingActive = false;
		}
	}
}

