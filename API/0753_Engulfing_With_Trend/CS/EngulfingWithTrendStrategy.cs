using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining SuperTrend filter with bullish and bearish engulfing patterns.
/// </summary>
public class EngulfingWithTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _boringThreshold;
	private readonly StrategyParam<decimal> _engulfingThreshold;
	private readonly StrategyParam<decimal> _stopLevel;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum body percentage to treat the previous candle as boring.
	/// </summary>
	public decimal BoringThreshold
	{
		get => _boringThreshold.Value;
		set => _boringThreshold.Value = value;
	}

	/// <summary>
	/// Minimum body percentage for the current candle.
	/// </summary>
	public decimal EngulfingThreshold
	{
		get => _engulfingThreshold.Value;
		set => _engulfingThreshold.Value = value;
	}

	/// <summary>
	/// Offset in ticks for stop calculation.
	/// </summary>
	public decimal StopLevel
	{
		get => _stopLevel.Value;
		set => _stopLevel.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public EngulfingWithTrendStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier for SuperTrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_boringThreshold = Param(nameof(BoringThreshold), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Boring Threshold", "Body percentage to skip previous candle", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_engulfingThreshold = Param(nameof(EngulfingThreshold), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Engulfing Threshold", "Body percentage for current candle", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(20m, 80m, 5m);

		_stopLevel = Param(nameof(StopLevel), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Level", "Offset in ticks for stop and target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 400m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevCandle = null;
		_stopLoss = 0m;
		_takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st = (SuperTrendIndicatorValue)supertrendValue;
		var isUptrend = st.IsUpTrend;
		var isDowntrend = !st.IsUpTrend;

		var step = Security.PriceStep ?? 1m;
		var offset = StopLevel * step;

		if (_prevCandle != null)
		{
			var prevBodyPerc = Math.Abs(_prevCandle.OpenPrice - _prevCandle.ClosePrice) * 100m /
				Math.Abs(_prevCandle.HighPrice - _prevCandle.LowPrice);
			var currBodyPerc = Math.Abs(candle.OpenPrice - candle.ClosePrice) * 100m /
				Math.Abs(candle.HighPrice - candle.LowPrice);

			var isBoring = prevBodyPerc <= BoringThreshold;
			var isSmall = currBodyPerc <= EngulfingThreshold;

			var bullEngulfing = isUptrend &&
				_prevCandle.ClosePrice < _prevCandle.OpenPrice &&
				candle.ClosePrice > _prevCandle.OpenPrice &&
				!isBoring && !isSmall;

			var bearEngulfing = isDowntrend &&
				_prevCandle.ClosePrice > _prevCandle.OpenPrice &&
				candle.ClosePrice < _prevCandle.OpenPrice &&
				!isBoring && !isSmall;

			if (bullEngulfing && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopLoss = candle.LowPrice;
				var bullStop = candle.ClosePrice + offset;
				_takeProfit = bullStop + (bullStop - candle.LowPrice);
			}
			else if (bearEngulfing && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopLoss = candle.HighPrice;
				var bearStop = candle.ClosePrice - offset;
				_takeProfit = bearStop - (candle.HighPrice - bearStop);
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
			{
				SellMarket(Position);
				_stopLoss = 0m;
				_takeProfit = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
			{
				BuyMarket(-Position);
				_stopLoss = 0m;
				_takeProfit = 0m;
			}
		}

		_prevCandle = candle;
	}
}
