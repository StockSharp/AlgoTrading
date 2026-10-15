using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji trading strategy.
/// A doji is a candle whose body is at most Tolerance of its range. A doji closing above the EMA opens a long.
/// The stop sits at the lowest low of the last StopBars candles at entry. Once price has risen TrailTriggerPercent above the entry,
/// a trailing stop follows the highest high at TrailOffsetPercent below it.
/// </summary>
public class DojiTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _tolerance;
	private readonly StrategyParam<int> _stopBars;
	private readonly StrategyParam<decimal> _trailTriggerPercent;
	private readonly StrategyParam<decimal> _trailOffsetPercent;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _highestSinceEntry;
	private bool _trailActive;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Largest body as a fraction of the candle range that still counts as a doji.
	/// </summary>
	public decimal Tolerance { get => _tolerance.Value; set => _tolerance.Value = value; }

	/// <summary>
	/// Candles whose lowest low sets the stop.
	/// </summary>
	public int StopBars { get => _stopBars.Value; set => _stopBars.Value = value; }

	/// <summary>
	/// Profit percent that activates the trailing stop.
	/// </summary>
	public decimal TrailTriggerPercent { get => _trailTriggerPercent.Value; set => _trailTriggerPercent.Value = value; }

	/// <summary>
	/// Trailing stop distance in percent below the highest high.
	/// </summary>
	public decimal TrailOffsetPercent { get => _trailOffsetPercent.Value; set => _trailOffsetPercent.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DojiTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA length", "Indicators");

		_tolerance = Param(nameof(Tolerance), 0.05m)
			.SetNotNegative()
			.SetDisplay("Tolerance", "Largest body as a fraction of the range for a doji", "Pattern");

		_stopBars = Param(nameof(StopBars), 450)
			.SetGreaterThanZero()
			.SetDisplay("Stop Bars", "Candles whose lowest low sets the stop", "Risk");

		_trailTriggerPercent = Param(nameof(TrailTriggerPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Trail Trigger %", "Profit percent that activates the trailing stop", "Risk");

		_trailOffsetPercent = Param(nameof(TrailOffsetPercent), 0.5m)
			.SetNotNegative()
			.SetDisplay("Trail Offset %", "Trailing stop distance below the highest high", "Risk");
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
		ClearTrade();
	}

	private void ClearTrade()
	{
		_entryPrice = null;
		_stopPrice = null;
		_highestSinceEntry = null;
		_trailActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ClearTrade();

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var lowest = new Lowest { Length = StopBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFormed || !lowestValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _entryPrice is decimal entry)
		{
			_highestSinceEntry = _highestSinceEntry is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;

			if (!_trailActive && _highestSinceEntry >= entry * (1 + TrailTriggerPercent / 100m))
				_trailActive = true;

			var stop = _stopPrice;
			if (_trailActive)
			{
				var trail = _highestSinceEntry.Value * (1 - TrailOffsetPercent / 100m);
				stop = stop is decimal s ? Math.Max(s, trail) : trail;
			}

			if (stop is decimal exit && candle.LowPrice <= exit)
			{
				SellMarket(Position);
				ClearTrade();
			}

			return;
		}

		var range = candle.HighPrice - candle.LowPrice;
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var isDoji = range > 0m && body <= range * Tolerance;

		if (Position == 0 && isDoji && candle.ClosePrice > emaValue.GetValue<decimal>())
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = lowestValue.GetValue<decimal>();
			_highestSinceEntry = null;
			_trailActive = false;
		}
	}
}
