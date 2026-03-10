using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LiquidityInternalMarketShiftStrategy : Strategy
{
	public enum TradeModes { Both, BullishOnly, BearishOnly }

	private readonly StrategyParam<TradeModes> _mode;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _upperLB;
	private readonly StrategyParam<int> _lowerLB;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private int _bullCnt, _bearCnt;
	private decimal? _lowBullP, _highBearP, _prevBullP, _prevBearP;
	private int _lastShift;
	private bool _touchLow, _touchUp, _lockBull, _lockBear;
	private int? _barSince;
	private decimal _entryPrice;
	private bool _hasEntry, _isLong;
	private DateTimeOffset _entryTime;
	private int _barsFromTrade;

	public TradeModes Mode { get => _mode.Value; set => _mode.Value = value; }
	public bool EnableTakeProfit { get => _enableTakeProfit.Value; set => _enableTakeProfit.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public int UpperLiquidityLookback { get => _upperLB.Value; set => _upperLB.Value = value; }
	public int LowerLiquidityLookback { get => _lowerLB.Value; set => _lowerLB.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityInternalMarketShiftStrategy()
	{
		_mode = Param(nameof(Mode), TradeModes.Both).SetDisplay("Mode", "Trading mode", "General");
		_enableTakeProfit = Param(nameof(EnableTakeProfit), true).SetDisplay("Enable TP", "Use TP", "Risk");
		_stopLossPips = Param(nameof(StopLossPips), 20).SetGreaterThanZero().SetDisplay("SL pips", "SL", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 40).SetGreaterThanZero().SetDisplay("TP pips", "TP", "Risk");
		_upperLB = Param(nameof(UpperLiquidityLookback), 14).SetGreaterThanZero().SetDisplay("Upper LB", "Upper", "Signals");
		_lowerLB = Param(nameof(LowerLiquidityLookback), 14).SetGreaterThanZero().SetDisplay("Lower LB", "Lower", "Signals");
		_cooldownBars = Param(nameof(CooldownBars), 10).SetGreaterThanZero().SetDisplay("Cooldown Bars", "Bars between trade actions", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null;
		_lowest = null;
		_bullCnt = 0;
		_bearCnt = 0;
		_lowBullP = null;
		_highBearP = null;
		_prevBullP = null;
		_prevBearP = null;
		_lastShift = 0;
		_touchLow = false;
		_touchUp = false;
		_lockBull = false;
		_lockBear = false;
		_barSince = null;
		_entryPrice = 0m;
		_hasEntry = false;
		_isLong = false;
		_entryTime = DateTimeOffset.MinValue;
		_barsFromTrade = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_highest = new Highest { Length = UpperLiquidityLookback };
		_lowest = new Lowest { Length = LowerLiquidityLookback };
		_bullCnt = 0; _bearCnt = 0;
		_lowBullP = null; _highBearP = null; _prevBullP = null; _prevBearP = null;
		_lastShift = 0; _touchLow = false; _touchUp = false;
		_lockBull = false; _lockBear = false; _barSince = null;
		_hasEntry = false; _entryPrice = 0m; _isLong = false;
		_barsFromTrade = int.MaxValue;

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(dummyEma1, dummyEma2, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, sub); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished) return;

		var hi = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.ServerTime) { IsFinal = true }).ToDecimal();
		var lo = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.ServerTime) { IsFinal = true }).ToDecimal();
		if (!_highest.IsFormed || !_lowest.IsFormed) return;
		_barsFromTrade++;

		if (candle.LowPrice <= lo) _touchLow = true;
		if (candle.HighPrice >= hi) _touchUp = true;

		bool isBull = candle.ClosePrice > candle.OpenPrice;
		bool isBear = candle.ClosePrice < candle.OpenPrice;
		if (isBull) { _bullCnt++; _bearCnt = 0; if (_bullCnt == 1 || candle.LowPrice < _lowBullP) _lowBullP = candle.LowPrice; }
		else if (isBear) { _bearCnt++; _bullCnt = 0; if (_bearCnt == 1 || candle.HighPrice > _highBearP) _highBearP = candle.HighPrice; }
		else { _bullCnt = 0; _bearCnt = 0; _lowBullP = null; _highBearP = null; }

		if (_bullCnt >= 1) _prevBullP = _lowBullP;
		if (_bearCnt >= 1) _prevBearP = _highBearP;

		bool shBear = _prevBullP.HasValue && _lowBullP.HasValue && candle.ClosePrice < _prevBullP && candle.ClosePrice < _lowBullP;
		bool shBull = _prevBearP.HasValue && _highBearP.HasValue && candle.ClosePrice > _prevBearP && candle.ClosePrice > _highBearP;

		var bullSig = shBull && _lastShift != 1 && _touchLow && !_lockBull;
		var bearSig = shBear && _lastShift != -1 && _touchUp && !_lockBear;

		if (bullSig) { _lockBull = true; _touchLow = false; _barSince = 0; _lastShift = 1; }
		if (bearSig) { _lockBear = true; _touchUp = false; _barSince = 0; _lastShift = -1; }
		if (_barSince.HasValue) _barSince++;
		if (_barSince >= 3) { _lockBull = false; _lockBear = false; _barSince = null; }
		if (_touchLow) _lockBull = false;
		if (_touchUp) _lockBear = false;
		var canTradeNow = _barsFromTrade >= CooldownBars;

		if (canTradeNow && Mode != TradeModes.BearishOnly && bullSig && !_hasEntry)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice; _entryTime = candle.OpenTime; _isLong = true; _hasEntry = true;
			_barsFromTrade = 0;
		}
		else if (canTradeNow && Mode != TradeModes.BullishOnly && bearSig && !_hasEntry)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice; _entryTime = candle.OpenTime; _isLong = false; _hasEntry = true;
			_barsFromTrade = 0;
		}

		if (_hasEntry && canTradeNow)
		{
			if (_isLong && bearSig && candle.OpenTime > _entryTime) { SellMarket(); _hasEntry = false; _barsFromTrade = 0; }
			else if (!_isLong && bullSig && candle.OpenTime > _entryTime) { BuyMarket(); _hasEntry = false; _barsFromTrade = 0; }

			if (_hasEntry)
			{
				var pip = Security.PriceStep ?? 0.01m;
				if (_isLong && (candle.ClosePrice <= _entryPrice - StopLossPips * pip || (EnableTakeProfit && candle.ClosePrice >= _entryPrice + TakeProfitPips * pip)))
				{ SellMarket(); _hasEntry = false; _barsFromTrade = 0; }
				else if (!_isLong && (candle.ClosePrice >= _entryPrice + StopLossPips * pip || (EnableTakeProfit && candle.ClosePrice <= _entryPrice - TakeProfitPips * pip)))
				{ BuyMarket(); _hasEntry = false; _barsFromTrade = 0; }
			}
		}
	}
}
