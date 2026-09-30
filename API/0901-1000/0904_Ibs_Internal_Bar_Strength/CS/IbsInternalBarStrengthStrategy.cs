namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Previous-bar IBS mean reversion with optional EMA alignment, spaced additions,
/// full-basket opposite-threshold exits and a maximum holding period in finished bars.
/// </summary>
public class IbsInternalBarStrengthStrategy : Strategy
{
	private readonly StrategyParam<decimal> _ibsEntryThreshold;
	private readonly StrategyParam<decimal> _ibsExitThreshold;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<decimal> _minEntryPct;
	private readonly StrategyParam<int> _maxTradeDuration;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousIbs;
	private decimal _previousClose;
	private decimal _previousEma;
	private bool _previousEmaReady;
	private decimal _lastEntryPrice;
	private int _barCount;
	private int _firstEntryBar;

	public decimal IbsEntryThreshold { get => _ibsEntryThreshold.Value; set => _ibsEntryThreshold.Value = value; }
	public decimal IbsExitThreshold { get => _ibsExitThreshold.Value; set => _ibsExitThreshold.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public bool UseEmaFilter { get => _useEmaFilter.Value; set => _useEmaFilter.Value = value; }
	public bool AllowLong { get => _allowLong.Value; set => _allowLong.Value = value; }
	public bool AllowShort { get => _allowShort.Value; set => _allowShort.Value = value; }
	public decimal MinEntryPct { get => _minEntryPct.Value; set => _minEntryPct.Value = value; }
	public int MaxTradeDuration { get => _maxTradeDuration.Value; set => _maxTradeDuration.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IbsInternalBarStrengthStrategy()
	{
		_ibsEntryThreshold = Param(nameof(IbsEntryThreshold), 0.09m).SetRange(0m, 1m);
		_ibsExitThreshold = Param(nameof(IbsExitThreshold), 0.985m).SetRange(0m, 1m);
		_emaPeriod = Param(nameof(EmaPeriod), 220).SetGreaterThanZero();
		_useEmaFilter = Param(nameof(UseEmaFilter), true);
		_allowLong = Param(nameof(AllowLong), true);
		_allowShort = Param(nameof(AllowShort), true);
		_minEntryPct = Param(nameof(MinEntryPct), 0m).SetNotNegative();
		_maxTradeDuration = Param(nameof(MaxTradeDuration), 14).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (IbsEntryThreshold >= IbsExitThreshold)
			throw new InvalidOperationException("IbsEntryThreshold must be below IbsExitThreshold.");

		ResetState();
		// A disabled filter must not impose a 220-bar warm-up on the IBS signal.
		var ema = new ExponentialMovingAverage { Length = UseEmaFilter ? EmaPeriod : 1 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, (candle, value) => ProcessCandle(candle, value, ema.IsFormed)).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (UseEmaFilter)
				DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_previousIbs = null;
		_previousClose = _previousEma = _lastEntryPrice = 0m;
		_previousEmaReady = false;
		_barCount = _firstEntryBar = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, bool emaReady)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ibs = _previousIbs;
		var previousClose = _previousClose;
		var previousEma = _previousEma;
		var filterReady = _previousEmaReady;
		var range = candle.HighPrice - candle.LowPrice;
		_previousIbs = range == 0m ? 0.5m : (candle.ClosePrice - candle.LowPrice) / range;
		_previousClose = candle.ClosePrice;
		_previousEma = emaValue;
		_previousEmaReady = emaReady;
		_barCount++;

		if (ibs is null || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
		{
			var oppositeThreshold = Position > 0m ? ibs > IbsExitThreshold : ibs < IbsEntryThreshold;
			if (oppositeThreshold || _barCount - _firstEntryBar >= MaxTradeDuration)
			{
				if (Position > 0m)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));
				_lastEntryPrice = 0m;
				_firstEntryBar = 0;
				return;
			}
		}

		if (UseEmaFilter && !filterReady)
			return;

		if (Position != 0m && _lastEntryPrice > 0m
			&& 100m * Math.Abs(candle.ClosePrice - _lastEntryPrice) / _lastEntryPrice < MinEntryPct)
			return;

		var buy = AllowLong && Position >= 0m && ibs < IbsEntryThreshold
			&& (!UseEmaFilter || previousClose > previousEma);
		var sell = AllowShort && Position <= 0m && ibs > IbsExitThreshold
			&& (!UseEmaFilter || previousClose < previousEma);
		if (!buy && !sell)
			return;

		// Added entries do not extend the lifetime of the existing basket.
		if (Position == 0m)
			_firstEntryBar = _barCount;
		_lastEntryPrice = candle.ClosePrice;
		if (buy)
			BuyMarket();
		else
			SellMarket();
	}
}
