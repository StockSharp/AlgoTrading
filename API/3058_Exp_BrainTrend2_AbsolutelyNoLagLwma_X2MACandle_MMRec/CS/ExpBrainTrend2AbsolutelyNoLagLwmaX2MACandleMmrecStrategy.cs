namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Hybrid strategy that combines ATR based breakout confirmation with LWMA and EMA filters.
/// </summary>
public class ExpBrainTrend2AbsolutelyNoLagLwmaX2MACandleMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _lwmaLength;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;

	private AverageTrueRange _atr;
	private LinearWeightedMovingAverage _lwma;
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;

	private bool _allowLongSignal;
	private bool _allowShortSignal;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="ExpBrainTrend2AbsolutelyNoLagLwmaX2MACandleMmrecStrategy"/>.
	/// </summary>
	public ExpBrainTrend2AbsolutelyNoLagLwmaX2MACandleMmrecStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series for the strategy", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range lookback", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_lwmaLength = Param(nameof(LwmaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("LWMA Length", "Linear weighted moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 1);

		_fastMaLength = Param(nameof(FastMaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length used by the candle filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length used by the candle filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 2);

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (ATR)", "Multiplier applied to ATR for protective stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (ATR)", "Multiplier applied to ATR for profit target", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 6m, 0.5m);
	}

	/// <summary>
	/// Working candle series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// LWMA period.
	/// </summary>
	public int LwmaLength
	{
		get => _lwmaLength.Value;
		set => _lwmaLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period used by the candle filter.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by the candle filter.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier expressed in ATR units.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit multiplier expressed in ATR units.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
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

			_allowLongSignal = false;
			_allowShortSignal = false;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_lwma = new LinearWeightedMovingAverage
		{
			Length = LwmaLength
		};

		_fastEma = new ExponentialMovingAverage
		{
			Length = FastMaLength
		};

		_slowEma = new ExponentialMovingAverage
		{
			Length = SlowMaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, _lwma, _fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _lwma);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal lwmaValue, decimal fastEmaValue, decimal slowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_atr.IsFormed || !_lwma.IsFormed || !_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var bullishFilter = close > lwmaValue && fastEmaValue > slowEmaValue;
		var bearishFilter = close < lwmaValue && fastEmaValue < slowEmaValue;

		if (!bullishFilter)
			_allowLongSignal = true;

		if (!bearishFilter)
			_allowShortSignal = true;

		if (bullishFilter && Position <= 0 && _allowLongSignal)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_allowLongSignal = false;
			_allowShortSignal = false;
		}
		else if (bearishFilter && Position >= 0 && _allowShortSignal)
		{
			SellMarket(Volume + Math.Abs(Position));
			_allowShortSignal = false;
			_allowLongSignal = false;
		}

		var atr = atrValue;

		if (Position > 0 && _longEntryPrice is decimal longPrice)
		{
			var stopPrice = longPrice - atr * StopLossAtrMultiplier;
			var targetPrice = longPrice + atr * TakeProfitAtrMultiplier;

			if (low <= stopPrice)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}
			else if (high >= targetPrice)
			{
				SellMarket(Position);
				_longEntryPrice = null;
			}
		}
		else if (Position < 0 && _shortEntryPrice is decimal shortPrice)
		{
			var stopPrice = shortPrice + atr * StopLossAtrMultiplier;
			var targetPrice = shortPrice - atr * TakeProfitAtrMultiplier;

			if (high >= stopPrice)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
			}
			else if (low <= targetPrice)
			{
				BuyMarket(-Position);
				_shortEntryPrice = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order?.Direction == Sides.Buy)
		{
			_longEntryPrice = trade.Trade?.Price;
			if (Position <= 0)
				_shortEntryPrice = null;
		}
		else if (trade.Order?.Direction == Sides.Sell)
		{
			_shortEntryPrice = trade.Trade?.Price;
			if (Position >= 0)
				_longEntryPrice = null;
		}

		if (Position == 0)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
		}
	}
}
