using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity and Engulfment combination strategy.
/// </summary>
public class LiquidityEngulfmentStrategy : Strategy
{
	public enum TradeModes { Both, BullishOnly, BearishOnly }

	private readonly StrategyParam<TradeModes> _mode;
	private readonly StrategyParam<int> _upperLookback;
	private readonly StrategyParam<int> _lowerLookback;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private ICandleMessage _prev;
	private decimal? _lastBullOpen;
	private decimal? _lastBearOpen;
	private int _lastBullIndex;
	private int _lastBearIndex;
	private string _lastSignal;
	private bool _touchedLower;
	private bool _touchedUpper;
	private bool _lockedBull;
	private bool _lockedBear;
	private int _sinceTouch;
	private decimal _entryPrice;
	private DateTimeOffset _entryTime;
	private int _index;
	private int _barsFromTrade;

	public TradeModes Mode { get => _mode.Value; set => _mode.Value = value; }
	public int UpperLookback { get => _upperLookback.Value; set => _upperLookback.Value = value; }
	public int LowerLookback { get => _lowerLookback.Value; set => _lowerLookback.Value = value; }
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }
	public bool EnableTakeProfit { get => _enableTakeProfit.Value; set => _enableTakeProfit.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityEngulfmentStrategy()
	{
		_mode = Param(nameof(Mode), TradeModes.Both).SetDisplay("Mode", "Trading mode", "General");
		_upperLookback = Param(nameof(UpperLookback), 14).SetGreaterThanZero().SetDisplay("Upper Lookback", "Upper liquidity", "Indicators");
		_lowerLookback = Param(nameof(LowerLookback), 14).SetGreaterThanZero().SetDisplay("Lower Lookback", "Lower liquidity", "Indicators");
		_stopLossPips = Param(nameof(StopLossPips), 25).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop in pips", "Risk");
		_takeProfitPips = Param(nameof(TakeProfitPips), 50).SetGreaterThanZero().SetDisplay("Take Profit", "Target in pips", "Risk");
		_enableTakeProfit = Param(nameof(EnableTakeProfit), true).SetDisplay("Enable TP", "Use take profit", "Risk");
		_cooldownBars = Param(nameof(CooldownBars), 12).SetGreaterThanZero().SetDisplay("Cooldown Bars", "Bars between trade actions", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null;
		_lowest = null;
		_prev = null;
		_lastBullOpen = null;
		_lastBearOpen = null;
		_lastBullIndex = 0;
		_lastBearIndex = 0;
		_lastSignal = string.Empty;
		_touchedLower = false;
		_touchedUpper = false;
		_lockedBull = false;
		_lockedBear = false;
		_sinceTouch = -1;
		_entryPrice = 0m;
		_entryTime = DateTimeOffset.MinValue;
		_index = 0;
		_barsFromTrade = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = UpperLookback };
		_lowest = new Lowest { Length = LowerLookback };
		_prev = null;
		_lastBullOpen = null;
		_lastBearOpen = null;
		_lastBullIndex = 0;
		_lastBearIndex = 0;
		_lastSignal = string.Empty;
		_touchedLower = false;
		_touchedUpper = false;
		_lockedBull = false;
		_lockedBear = false;
		_sinceTouch = -1;
		_entryPrice = 0m;
		_entryTime = DateTimeOffset.MinValue;
		_index = 0;
		_barsFromTrade = int.MaxValue;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highVal = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.ServerTime) { IsFinal = true });
		var lowVal = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.ServerTime) { IsFinal = true });

		if (!_highest.IsFormed || !_lowest.IsFormed)
		{
			_prev = candle;
			_index++;
			return;
		}

		_barsFromTrade++;

		var highest = highVal.ToDecimal();
		var lowest = lowVal.ToDecimal();

		if (candle.LowPrice <= lowest)
			_touchedLower = true;
		if (candle.HighPrice >= highest)
			_touchedUpper = true;

		var bull = candle.ClosePrice > candle.OpenPrice;
		var bear = candle.ClosePrice < candle.OpenPrice;

		if (bull)
		{
			_lastBullOpen = candle.OpenPrice;
			_lastBullIndex = _index;
		}
		else if (bear)
		{
			_lastBearOpen = candle.OpenPrice;
			_lastBearIndex = _index;
		}

		var bullEngulf = _lastBearOpen.HasValue && _prev != null && candle.ClosePrice > _lastBearOpen && candle.ClosePrice > _prev.LowPrice && _index > _lastBearIndex;
		var bearEngulf = _lastBullOpen.HasValue && _prev != null && candle.ClosePrice < _lastBullOpen && candle.ClosePrice < _prev.HighPrice && _index > _lastBullIndex;

		var bullSignal = bullEngulf && _lastSignal != "bullish" && _touchedLower && !_lockedBull;
		var bearSignal = bearEngulf && _lastSignal != "bearish" && _touchedUpper && !_lockedBear;

		if (bullEngulf) _lastSignal = "bullish";
		if (bearEngulf) _lastSignal = "bearish";

		if (bullSignal)
		{
			_lockedBull = true;
			_touchedLower = false;
			_sinceTouch = 0;
		}
		if (bearSignal)
		{
			_lockedBear = true;
			_touchedUpper = false;
			_sinceTouch = 0;
		}

		if (_sinceTouch >= 0) _sinceTouch++;
		if (_sinceTouch >= 3)
		{
			_lockedBull = false;
			_lockedBear = false;
		}
		if (_touchedLower) _lockedBull = false;
		if (_touchedUpper) _lockedBear = false;

		var step = Security.PriceStep ?? 1m;
		var canLong = Mode != TradeModes.BearishOnly;
		var canShort = Mode != TradeModes.BullishOnly;
		var canTradeNow = _barsFromTrade >= CooldownBars;

		if (canTradeNow && canShort && bearSignal && Position == 0)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
			_barsFromTrade = 0;
		}
		else if (canTradeNow && canLong && bullSignal && Position == 0)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_entryTime = candle.OpenTime;
			_barsFromTrade = 0;
		}
		else if (canTradeNow)
		{
			if (Position < 0 && bullSignal && candle.OpenTime > _entryTime)
			{
				BuyMarket(Math.Abs(Position));
				_barsFromTrade = 0;
			}
			else if (Position > 0 && bearSignal && candle.OpenTime > _entryTime)
			{
				SellMarket(Position);
				_barsFromTrade = 0;
			}
		}

		if (canTradeNow && Position > 0)
		{
			var stop = _entryPrice - StopLossPips * step;
			var tp = _entryPrice + TakeProfitPips * step;
			if (candle.ClosePrice <= stop || (EnableTakeProfit && candle.ClosePrice >= tp))
			{
				SellMarket(Position);
				_barsFromTrade = 0;
			}
		}
		else if (canTradeNow && Position < 0)
		{
			var stop = _entryPrice + StopLossPips * step;
			var tp = _entryPrice - TakeProfitPips * step;
			if (candle.ClosePrice >= stop || (EnableTakeProfit && candle.ClosePrice <= tp))
			{
				BuyMarket(Math.Abs(Position));
				_barsFromTrade = 0;
			}
		}

		_prev = candle;
		_index++;
	}
}
