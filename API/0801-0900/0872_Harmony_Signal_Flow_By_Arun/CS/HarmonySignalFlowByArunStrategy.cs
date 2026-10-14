using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Harmony Signal Flow By Arun strategy.
/// An RSI crossing above LowerThreshold buys and an RSI crossing below UpperThreshold sells, reversing an opposite position.
/// Longs use BuyStopLoss and BuyTarget, shorts SellStopLoss and SellTarget, all in price steps from the entry. Any open position
/// is closed once a day at 15:25 (UTC candle open time).
/// </summary>
public class HarmonySignalFlowByArunStrategy : Strategy
{
	private static readonly TimeSpan _sessionClose = new(15, 25, 0);

	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _buyStopLoss;
	private readonly StrategyParam<decimal> _buyTarget;
	private readonly StrategyParam<decimal> _sellStopLoss;
	private readonly StrategyParam<decimal> _sellTarget;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;
	private decimal _entryPrice;
	private DateTime _lastSessionClose;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level whose upward cross buys.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// RSI level whose downward cross sells.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Long stop loss in price steps.
	/// </summary>
	public decimal BuyStopLoss
	{
		get => _buyStopLoss.Value;
		set => _buyStopLoss.Value = value;
	}

	/// <summary>
	/// Long target in price steps.
	/// </summary>
	public decimal BuyTarget
	{
		get => _buyTarget.Value;
		set => _buyTarget.Value = value;
	}

	/// <summary>
	/// Short stop loss in price steps.
	/// </summary>
	public decimal SellStopLoss
	{
		get => _sellStopLoss.Value;
		set => _sellStopLoss.Value = value;
	}

	/// <summary>
	/// Short target in price steps.
	/// </summary>
	public decimal SellTarget
	{
		get => _sellTarget.Value;
		set => _sellTarget.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HarmonySignalFlowByArunStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_lowerThreshold = Param(nameof(LowerThreshold), 30m)
			.SetDisplay("Lower Threshold", "RSI level whose upward cross buys", "Indicators");

		_upperThreshold = Param(nameof(UpperThreshold), 70m)
			.SetDisplay("Upper Threshold", "RSI level whose downward cross sells", "Indicators");

		_buyStopLoss = Param(nameof(BuyStopLoss), 100m)
			.SetNotNegative()
			.SetDisplay("Buy Stop Loss", "Long stop loss in price steps", "Risk");

		_buyTarget = Param(nameof(BuyTarget), 150m)
			.SetNotNegative()
			.SetDisplay("Buy Target", "Long target in price steps", "Risk");

		_sellStopLoss = Param(nameof(SellStopLoss), 100m)
			.SetNotNegative()
			.SetDisplay("Sell Stop Loss", "Short stop loss in price steps", "Risk");

		_sellTarget = Param(nameof(SellTarget), 150m)
			.SetNotNegative()
			.SetDisplay("Sell Target", "Short target in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevRsi = null;
		_entryPrice = 0m;
		_lastSessionClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security?.PriceStep ?? 1m;

		// Daily close at 15:25.
		var openTime = candle.OpenTime;
		if (openTime.TimeOfDay >= _sessionClose && _lastSessionClose != openTime.Date)
		{
			_lastSessionClose = openTime.Date;

			if (Position > 0)
			{
				SellMarket(Position);
				return;
			}

			if (Position < 0)
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (Position > 0)
		{
			var stop = BuyStopLoss > 0 && candle.LowPrice <= _entryPrice - BuyStopLoss * step;
			var target = BuyTarget > 0 && candle.HighPrice >= _entryPrice + BuyTarget * step;

			if (stop || target)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			var stop = SellStopLoss > 0 && candle.HighPrice >= _entryPrice + SellStopLoss * step;
			var target = SellTarget > 0 && candle.LowPrice <= _entryPrice - SellTarget * step;

			if (stop || target)
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (prevRsi is not decimal lastRsi)
			return;

		if (lastRsi <= LowerThreshold && rsi > LowerThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
		else if (lastRsi >= UpperThreshold && rsi < UpperThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
	}
}
