namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Bollinger breakout with independently optional RSI, ADX, ATR, EMA and MACD filters.</summary>
public class BigMoverCatcherStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<bool> _isRsiFilterEnabled;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLongThreshold;
	private readonly StrategyParam<decimal> _rsiShortThreshold;
	private readonly StrategyParam<bool> _isAdxFilterEnabled;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _minimumAdx;
	private readonly StrategyParam<bool> _isAtrFilterEnabled;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _minimumAtrPercent;
	private readonly StrategyParam<bool> _isEmaFilterEnabled;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<bool> _isMacdFilterEnabled;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _isForceTakeProfitEnabled;
	private readonly StrategyParam<decimal> _forceTakeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;
	private int _rsiIndex = -1, _adxIndex = -1, _atrIndex = -1, _emaIndex = -1, _macdIndex = -1;
	private Order _pending;

	/// <summary>Rolling close window.</summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>Population deviation multiplier.</summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>Enable directional RSI confirmation.</summary>
	public bool IsRsiFilterEnabled
	{
		get => _isRsiFilterEnabled.Value;
		set => _isRsiFilterEnabled.Value = value;
	}

	/// <summary>Wilder RSI length.</summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>Strict lower RSI bound for longs.</summary>
	public decimal RsiLongThreshold
	{
		get => _rsiLongThreshold.Value;
		set => _rsiLongThreshold.Value = value;
	}

	/// <summary>Strict upper RSI bound for shorts.</summary>
	public decimal RsiShortThreshold
	{
		get => _rsiShortThreshold.Value;
		set => _rsiShortThreshold.Value = value;
	}

	/// <summary>Enable minimum ADX strength.</summary>
	public bool IsAdxFilterEnabled
	{
		get => _isAdxFilterEnabled.Value;
		set => _isAdxFilterEnabled.Value = value;
	}

	/// <summary>Wilder directional movement and ADX lengths.</summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>Minimum ADX strength.</summary>
	public decimal MinimumAdx
	{
		get => _minimumAdx.Value;
		set => _minimumAdx.Value = value;
	}

	/// <summary>Enable minimum ATR percentage.</summary>
	public bool IsAtrFilterEnabled
	{
		get => _isAtrFilterEnabled.Value;
		set => _isAtrFilterEnabled.Value = value;
	}

	/// <summary>Wilder ATR length.</summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>Minimum ATR divided by close in percent.</summary>
	public decimal MinimumAtrPercent
	{
		get => _minimumAtrPercent.Value;
		set => _minimumAtrPercent.Value = value;
	}

	/// <summary>Enable close versus EMA confirmation.</summary>
	public bool IsEmaFilterEnabled
	{
		get => _isEmaFilterEnabled.Value;
		set => _isEmaFilterEnabled.Value = value;
	}

	/// <summary>Trend EMA length.</summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>Enable directional MACD histogram.</summary>
	public bool IsMacdFilterEnabled
	{
		get => _isMacdFilterEnabled.Value;
		set => _isMacdFilterEnabled.Value = value;
	}

	/// <summary>Fast MACD EMA length.</summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>Slow MACD EMA length.</summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>MACD signal EMA length.</summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>Allowed entry side; null allows both.</summary>
	public Sides? TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>Native filled-entry percentage stop.</summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>Enable favorable large-body exits.</summary>
	public bool IsForceTakeProfitEnabled
	{
		get => _isForceTakeProfitEnabled.Value;
		set => _isForceTakeProfitEnabled.Value = value;
	}

	/// <summary>Minimum favorable candle body percentage.</summary>
	public decimal ForceTakeProfitPercent
	{
		get => _forceTakeProfitPercent.Value;
		set => _forceTakeProfitPercent.Value = value;
	}

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Initializes the published bands, filters and protection parameters.</summary>
	public BigMoverCatcherStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 40).SetGreaterThanZero()
			.SetDisplay("BollingerPeriod", "Rolling close window", "Parameters");
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetGreaterThanZero()
			.SetDisplay("BollingerDeviation", "Population deviation multiplier", "Parameters");
		_isRsiFilterEnabled = Param(nameof(IsRsiFilterEnabled), true)
			.SetDisplay("IsRsiFilterEnabled", "Enable directional RSI confirmation", "Parameters");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RsiPeriod", "Wilder RSI length", "Parameters");
		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 50m).SetGreaterThanZero()
			.SetDisplay("RsiLongThreshold", "Strict lower RSI bound for longs", "Parameters");
		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 50m).SetGreaterThanZero()
			.SetDisplay("RsiShortThreshold", "Strict upper RSI bound for shorts", "Parameters");
		_isAdxFilterEnabled = Param(nameof(IsAdxFilterEnabled), true)
			.SetDisplay("IsAdxFilterEnabled", "Enable minimum ADX strength", "Parameters");
		_adxPeriod = Param(nameof(AdxPeriod), 28).SetGreaterThanZero()
			.SetDisplay("AdxPeriod", "Wilder directional movement and ADX lengths", "Parameters");
		_minimumAdx = Param(nameof(MinimumAdx), 20m).SetGreaterThanZero()
			.SetDisplay("MinimumAdx", "Minimum ADX strength", "Parameters");
		_isAtrFilterEnabled = Param(nameof(IsAtrFilterEnabled), true)
			.SetDisplay("IsAtrFilterEnabled", "Enable minimum ATR percentage", "Parameters");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("AtrPeriod", "Wilder ATR length", "Parameters");
		_minimumAtrPercent = Param(nameof(MinimumAtrPercent), 0.05m).SetGreaterThanZero()
			.SetDisplay("MinimumAtrPercent", "Minimum ATR divided by close in percent", "Parameters");
		_isEmaFilterEnabled = Param(nameof(IsEmaFilterEnabled), true)
			.SetDisplay("IsEmaFilterEnabled", "Enable close versus EMA confirmation", "Parameters");
		_emaPeriod = Param(nameof(EmaPeriod), 350).SetGreaterThanZero()
			.SetDisplay("EmaPeriod", "Trend EMA length", "Parameters");
		_isMacdFilterEnabled = Param(nameof(IsMacdFilterEnabled), true)
			.SetDisplay("IsMacdFilterEnabled", "Enable directional MACD histogram", "Parameters");
		_macdFast = Param(nameof(MacdFast), 12).SetGreaterThanZero()
			.SetDisplay("MacdFast", "Fast MACD EMA length", "Parameters");
		_macdSlow = Param(nameof(MacdSlow), 26).SetGreaterThanZero()
			.SetDisplay("MacdSlow", "Slow MACD EMA length", "Parameters");
		_macdSignal = Param(nameof(MacdSignal), 9).SetGreaterThanZero()
			.SetDisplay("MacdSignal", "MACD signal EMA length", "Parameters");
		_tradeDirection = Param(nameof(TradeDirection), (Sides?)null)
			.SetDisplay("TradeDirection", "Allowed entry side; null allows both", "Parameters");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetGreaterThanZero()
			.SetDisplay("StopLossPercent", "Native filled-entry percentage stop", "Parameters");
		_isForceTakeProfitEnabled = Param(nameof(IsForceTakeProfitEnabled), false)
			.SetDisplay("IsForceTakeProfitEnabled", "Enable favorable large-body exits", "Parameters");
		_forceTakeProfitPercent = Param(nameof(ForceTakeProfitPercent), 5m).SetGreaterThanZero()
			.SetDisplay("ForceTakeProfitPercent", "Minimum favorable candle body percentage", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiIndex = _adxIndex = _atrIndex = _emaIndex = _macdIndex = -1;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (MacdFast >= MacdSlow || RsiLongThreshold >= 100m || RsiShortThreshold >= 100m)
			throw new InvalidOperationException("MACD lengths must be ordered and RSI thresholds must lie strictly between zero and 100.");
		base.OnStarted2(time);
		var bb = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		List<IIndicator> indicators = [bb];
		int Add(IIndicator indicator)
		{
			indicators.Add(indicator);
			return indicators.Count - 1;
		}
		_rsiIndex = IsRsiFilterEnabled ? Add(new RelativeStrengthIndex { Length = RsiPeriod }) : -1;
		_adxIndex = IsAdxFilterEnabled ? Add(new AverageDirectionalIndex { Length = AdxPeriod }) : -1;
		_atrIndex = IsAtrFilterEnabled ? Add(new AverageTrueRange { Length = AtrPeriod }) : -1;
		_emaIndex = IsEmaFilterEnabled ? Add(new EMA { Length = EmaPeriod }) : -1;
		_macdIndex = IsMacdFilterEnabled ? Add(new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFast }, LongMa = { Length = MacdSlow } },
			SignalMa = { Length = MacdSignal },
		}) : -1;
		StartProtection(new Unit(), StopLossPercent.Percents(), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.BindEx(indicators.ToArray(), ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, bb);
			if (_emaIndex >= 0) DrawIndicator(area, indicators[_emaIndex]);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || values[0] is not BollingerBandsValue bb
			|| !values[0].IsFormed || bb.MovingAverage is not decimal middle || bb.UpBand is not decimal upper || bb.LowBand is not decimal lower
			|| !IsFormedAndOnlineAndAllowTrading() || _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position != 0m)
		{
			var body = candle.OpenPrice > 0m ? (Position > 0m ? candle.ClosePrice - candle.OpenPrice : candle.OpenPrice - candle.ClosePrice) / candle.OpenPrice * 100m : 0m;
			if ((Position > 0m ? candle.ClosePrice <= middle : candle.ClosePrice >= middle) || IsForceTakeProfitEnabled && body >= ForceTakeProfitPercent)
			{
				if (Position > 0m) SellMarket(Position);
				else BuyMarket(-Position);
			}
			return;
		}
		if (!values.All(v => v.IsFormed && !v.IsEmpty) || candle.ClosePrice <= 0m)
			return;
		var side = candle.ClosePrice > upper ? Sides.Buy : candle.ClosePrice < lower ? Sides.Sell : (Sides?)null;
		if (side is null || TradeDirection is Sides permitted && side != permitted)
			return;
		var buy = side == Sides.Buy;
		if (_rsiIndex >= 0)
		{
			var rsi = values[_rsiIndex].GetValue<decimal>();
			if (buy ? rsi <= RsiLongThreshold : rsi >= RsiShortThreshold) return;
		}
		if (_adxIndex >= 0 && (values[_adxIndex] is not IAverageDirectionalIndexValue adx || adx.MovingAverage is null || adx.MovingAverage < MinimumAdx))
			return;
		if (_atrIndex >= 0 && values[_atrIndex].GetValue<decimal>() / candle.ClosePrice * 100m < MinimumAtrPercent)
			return;
		if (_emaIndex >= 0)
		{
			var ema = values[_emaIndex].GetValue<decimal>();
			if (buy ? candle.ClosePrice <= ema : candle.ClosePrice >= ema) return;
		}
		if (_macdIndex >= 0)
		{
			if (values[_macdIndex] is not IMovingAverageConvergenceDivergenceSignalValue macd || macd.Macd is null || macd.Signal is null)
				return;
			var histogram = macd.Macd.Value - macd.Signal.Value;
			if (buy ? histogram <= 0m : histogram >= 0m) return;
		}
		if (buy) BuyMarket(Volume);
		else SellMarket(Volume);
	}
}
