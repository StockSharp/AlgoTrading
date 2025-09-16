using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily GBPUSD breakout strategy using EMA, RSI, MACD and ATR filters.
/// </summary>
public class AlexavD1ProfitGbpUsdStrategy : Strategy
{
	private const int OrdersPerSignal = 4;
	private static readonly decimal[] TargetSteps = { 1m, 1.5m, 2m, 2.5m };

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<decimal> _takeMultiplier;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _macdDiffBuy;
	private readonly StrategyParam<decimal> _macdDiffSell;
	private readonly StrategyParam<decimal> _rsiUpperLimit;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<decimal> _rsiLowerLimit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly EMA _emaOnHigh = new();
	private readonly RelativeStrengthIndex _rsi = new();
	private readonly AverageTrueRange _atr = new();
	private readonly MovingAverageConvergenceDivergenceSignal _macd = new();

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;

	private decimal[] _longTargets = Array.Empty<decimal>();
	private decimal[] _shortTargets = Array.Empty<decimal>();
	private int _longTargetsFilled;
	private int _shortTargetsFilled;
	private decimal _longStopPrice;
	private decimal _shortStopPrice;
	private decimal _longVolumePerOrder;
	private decimal _shortVolumePerOrder;

	public AlexavD1ProfitGbpUsdStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume per market order", "Trading");

		_emaPeriod = Param(nameof(EmaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "EMA length applied to candle highs", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "RSI averaging period", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 28)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR averaging period", "Indicators");

		_stopMultiplier = Param(nameof(StopLossMultiplier), 1.6m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Multiplier", "ATR multiplier for stop loss", "Risk");

		_takeMultiplier = Param(nameof(TakeProfitMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit Multiplier", "ATR multiplier for take profits", "Risk");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period for MACD", "Indicators");

		_macdDiffBuy = Param(nameof(MacdDiffBuyThreshold), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("MACD Diff Buy", "Minimum MACD acceleration for buys", "Filters");

		_macdDiffSell = Param(nameof(MacdDiffSellThreshold), 0.15m)
		.SetGreaterThanZero()
		.SetDisplay("MACD Diff Sell", "Minimum MACD acceleration for sells", "Filters");

		_rsiUpperLimit = Param(nameof(RsiUpperLimit), 80m)
		.SetDisplay("RSI Upper Limit", "Maximum RSI allowed for longs", "Filters");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 60m)
		.SetDisplay("RSI Upper Level", "Minimum RSI required for longs", "Filters");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 39m)
		.SetDisplay("RSI Lower Level", "Maximum RSI allowed for shorts", "Filters");

		_rsiLowerLimit = Param(nameof(RsiLowerLimit), 25m)
		.SetDisplay("RSI Lower Limit", "Minimum RSI required for shorts", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal StopLossMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	public decimal TakeProfitMultiplier
	{
		get => _takeMultiplier.Value;
		set => _takeMultiplier.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public decimal MacdDiffBuyThreshold
	{
		get => _macdDiffBuy.Value;
		set => _macdDiffBuy.Value = value;
	}

	public decimal MacdDiffSellThreshold
	{
		get => _macdDiffSell.Value;
		set => _macdDiffSell.Value = value;
	}

	public decimal RsiUpperLimit
	{
		get => _rsiUpperLimit.Value;
		set => _rsiUpperLimit.Value = value;
	}

	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	public decimal RsiLowerLimit
	{
		get => _rsiLowerLimit.Value;
		set => _rsiLowerLimit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_emaOnHigh.Reset();
		_rsi.Reset();
		_atr.Reset();
		_macd.Reset();

		_macdPrev1 = null;
		_macdPrev2 = null;

		ResetLongState();
		ResetShortState();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_emaOnHigh.Length = EmaPeriod;
		_rsi.Length = RsiPeriod;
		_atr.Length = AtrPeriod;
		_macd.Macd.ShortMa.Length = MacdFastPeriod;
		_macd.Macd.LongMa.Length = MacdSlowPeriod;
		_macd.SignalMa.Length = MacdSignalPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _rsi, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal || !rsiValue.IsFinal || !atrValue.IsFinal)
		return;

		var macdCurrent = ((MovingAverageConvergenceDivergenceSignalValue)macdValue).Macd;
		var macd1 = _macdPrev1;
		var macd2 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdCurrent;

		var emaValue = _emaOnHigh.Process(candle.HighPrice, candle.OpenTime, true);
		if (!emaValue.IsFinal || macd1 is null || macd2 is null)
		return;

		var ema = emaValue.GetValue<decimal>();
		var rsi = rsiValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var macdPrev1 = macd1.Value;
		var macdPrev2 = macd2.Value;

		if (atr <= 0m)
		return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var longSignal = open < ema && close > ema && rsi < RsiUpperLimit && rsi > RsiUpperLevel;
		var shortSignal = open > ema && close < ema && rsi > RsiLowerLimit && rsi < RsiLowerLevel;

		var macdAbs1 = Math.Abs(macdPrev1);
		var macdAbs2 = Math.Abs(macdPrev2);
		var macdDelta = macdAbs1 > 0m ? (macdAbs1 - macdAbs2) / macdAbs1 : 0m;

		if (longSignal && (macdPrev2 < 0m || macdDelta > MacdDiffBuyThreshold))
		TryEnterLong(candle, atr);
		else if (shortSignal && (macdPrev2 > 0m || macdDelta > MacdDiffSellThreshold))
		TryEnterShort(candle, atr);

		if (Position > 0)
		ManageLongPosition(candle);
		else if (Position < 0)
		ManageShortPosition(candle);
		else
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private void TryEnterLong(ICandleMessage candle, decimal atr)
	{
		if (OrderVolume <= 0m)
		return;

		if (Position < 0)
		BuyMarket(Math.Abs(Position));

		if (Position > 0)
		return;

		var perOrder = OrderVolume;
		for (var i = 0; i < OrdersPerSignal; i++)
		BuyMarket(perOrder);

		_longVolumePerOrder = perOrder;
		_longStopPrice = candle.ClosePrice - atr * StopLossMultiplier;
		_longTargets = new decimal[OrdersPerSignal];
		for (var i = 0; i < OrdersPerSignal; i++)
		{
			var step = TargetSteps[i];
			_longTargets[i] = candle.ClosePrice + atr * TakeProfitMultiplier * step;
		}
		_longTargetsFilled = 0;
	}

	private void TryEnterShort(ICandleMessage candle, decimal atr)
	{
		if (OrderVolume <= 0m)
		return;

		if (Position > 0)
		SellMarket(Position);

		if (Position < 0)
		return;

		var perOrder = OrderVolume;
		for (var i = 0; i < OrdersPerSignal; i++)
		SellMarket(perOrder);

		_shortVolumePerOrder = perOrder;
		_shortStopPrice = candle.ClosePrice + atr * StopLossMultiplier;
		_shortTargets = new decimal[OrdersPerSignal];
		for (var i = 0; i < OrdersPerSignal; i++)
		{
			var step = TargetSteps[i];
			_shortTargets[i] = candle.ClosePrice - atr * TakeProfitMultiplier * step;
		}
		_shortTargetsFilled = 0;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		if (_longTargets.Length == 0)
		return;

		var position = Position;
		if (position <= 0)
		{
			ResetLongState();
			return;
		}

		if (candle.LowPrice <= _longStopPrice)
		{
			SellMarket(position);
			ResetLongState();
			return;
		}

		for (var i = _longTargetsFilled; i < _longTargets.Length; i++)
		{
			var target = _longTargets[i];
			if (candle.HighPrice < target)
			break;

			var exitVolume = Math.Min(_longVolumePerOrder, Position);
			if (exitVolume <= 0m)
			{
				ResetLongState();
				return;
			}

			SellMarket(exitVolume);
			_longTargetsFilled++;
			_longStopPrice = Math.Max(_longStopPrice, target);

			if (Position <= 0)
			{
				ResetLongState();
				return;
			}
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		if (_shortTargets.Length == 0)
		return;

		var position = Position;
		if (position >= 0)
		{
			ResetShortState();
			return;
		}

		if (candle.HighPrice >= _shortStopPrice)
		{
			BuyMarket(Math.Abs(position));
			ResetShortState();
			return;
		}

		for (var i = _shortTargetsFilled; i < _shortTargets.Length; i++)
		{
			var target = _shortTargets[i];
			if (candle.LowPrice > target)
			break;

			var exitVolume = Math.Min(_shortVolumePerOrder, Math.Abs(Position));
			if (exitVolume <= 0m)
			{
				ResetShortState();
				return;
			}

			BuyMarket(exitVolume);
			_shortTargetsFilled++;
			_shortStopPrice = Math.Min(_shortStopPrice, target);

			if (Position >= 0)
			{
				ResetShortState();
				return;
			}
		}
	}

	private void ResetLongState()
	{
		_longTargets = Array.Empty<decimal>();
		_longTargetsFilled = 0;
		_longStopPrice = 0m;
		_longVolumePerOrder = 0m;
	}

	private void ResetShortState()
	{
		_shortTargets = Array.Empty<decimal>();
		_shortTargetsFilled = 0;
		_shortStopPrice = 0m;
		_shortVolumePerOrder = 0m;
	}
}
