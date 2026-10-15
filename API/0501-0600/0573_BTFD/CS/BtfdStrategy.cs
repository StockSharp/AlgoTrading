using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTFD (buy the dip) strategy.
/// Long only: when flat, buys a candle whose volume exceeds VolumeMultiplier times the SMA(VolumeLength) of volume while RSI(RsiLength)
/// is below RsiOversold. Five take-profit levels Tp1..Tp5 percent above the entry each close Q1..Q5 percent of the position still
/// open (Q5 = 100 closes the rest), and a StopLossPercent stop closes everything.
/// </summary>
public class BtfdStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _tp1;
	private readonly StrategyParam<decimal> _tp2;
	private readonly StrategyParam<decimal> _tp3;
	private readonly StrategyParam<decimal> _tp4;
	private readonly StrategyParam<decimal> _tp5;
	private readonly StrategyParam<decimal> _q1;
	private readonly StrategyParam<decimal> _q2;
	private readonly StrategyParam<decimal> _q3;
	private readonly StrategyParam<decimal> _q4;
	private readonly StrategyParam<decimal> _q5;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private decimal _entryPrice;
	private int _nextTarget;

	/// <summary>
	/// Period of the volume SMA.
	/// </summary>
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }

	/// <summary>
	/// Multiple of the volume SMA that marks a spike.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI level below which the market is oversold.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// First take profit in percent.
	/// </summary>
	public decimal Tp1 { get => _tp1.Value; set => _tp1.Value = value; }

	/// <summary>
	/// Second take profit in percent.
	/// </summary>
	public decimal Tp2 { get => _tp2.Value; set => _tp2.Value = value; }

	/// <summary>
	/// Third take profit in percent.
	/// </summary>
	public decimal Tp3 { get => _tp3.Value; set => _tp3.Value = value; }

	/// <summary>
	/// Fourth take profit in percent.
	/// </summary>
	public decimal Tp4 { get => _tp4.Value; set => _tp4.Value = value; }

	/// <summary>
	/// Fifth take profit in percent.
	/// </summary>
	public decimal Tp5 { get => _tp5.Value; set => _tp5.Value = value; }

	/// <summary>
	/// Percent of the open position closed at the first target.
	/// </summary>
	public decimal Q1 { get => _q1.Value; set => _q1.Value = value; }

	/// <summary>
	/// Percent of the open position closed at the second target.
	/// </summary>
	public decimal Q2 { get => _q2.Value; set => _q2.Value = value; }

	/// <summary>
	/// Percent of the open position closed at the third target.
	/// </summary>
	public decimal Q3 { get => _q3.Value; set => _q3.Value = value; }

	/// <summary>
	/// Percent of the open position closed at the fourth target.
	/// </summary>
	public decimal Q4 { get => _q4.Value; set => _q4.Value = value; }

	/// <summary>
	/// Percent of the open position closed at the fifth target.
	/// </summary>
	public decimal Q5 { get => _q5.Value; set => _q5.Value = value; }

	/// <summary>
	/// Stop loss in percent below the entry price.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BtfdStrategy()
	{
		_volumeLength = Param(nameof(VolumeLength), 70)
			.SetGreaterThanZero()
			.SetDisplay("Volume Length", "Period of the volume SMA", "Entry");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiple of the volume SMA that marks a spike", "Entry");

		_rsiLength = Param(nameof(RsiLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Entry");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level below which the market is oversold", "Entry");

		_tp1 = Param(nameof(Tp1), 0.4m).SetNotNegative().SetDisplay("TP1 %", "First take profit", "Targets");
		_tp2 = Param(nameof(Tp2), 0.6m).SetNotNegative().SetDisplay("TP2 %", "Second take profit", "Targets");
		_tp3 = Param(nameof(Tp3), 0.8m).SetNotNegative().SetDisplay("TP3 %", "Third take profit", "Targets");
		_tp4 = Param(nameof(Tp4), 1.0m).SetNotNegative().SetDisplay("TP4 %", "Fourth take profit", "Targets");
		_tp5 = Param(nameof(Tp5), 1.2m).SetNotNegative().SetDisplay("TP5 %", "Fifth take profit", "Targets");

		_q1 = Param(nameof(Q1), 20m).SetRange(0m, 100m).SetDisplay("Q1 %", "Share of the open position closed at TP1", "Targets");
		_q2 = Param(nameof(Q2), 40m).SetRange(0m, 100m).SetDisplay("Q2 %", "Share of the open position closed at TP2", "Targets");
		_q3 = Param(nameof(Q3), 60m).SetRange(0m, 100m).SetDisplay("Q3 %", "Share of the open position closed at TP3", "Targets");
		_q4 = Param(nameof(Q4), 80m).SetRange(0m, 100m).SetDisplay("Q4 %", "Share of the open position closed at TP4", "Targets");
		_q5 = Param(nameof(Q5), 100m).SetRange(0m, 100m).SetDisplay("Q5 %", "Share of the open position closed at TP5", "Targets");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss below the entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(3).TimeFrame())
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
		_volumeSma = null;
		_entryPrice = 0;
		_nextTarget = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_nextTarget = 0;
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

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

		var volumeAverage = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_volumeSma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			ManagePosition(candle);
			return;
		}

		if (Position == 0 && candle.TotalVolume > volumeAverage * VolumeMultiplier && rsi < RsiOversold)
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_nextTarget = 0;
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (_entryPrice <= 0)
			return;

		if (StopLossPercent > 0 && candle.LowPrice <= _entryPrice * (1m - StopLossPercent / 100m))
		{
			SellMarket(Position);
			_entryPrice = 0;
			return;
		}

		var targets = new[] { Tp1, Tp2, Tp3, Tp4, Tp5 };
		var shares = new[] { Q1, Q2, Q3, Q4, Q5 };

		var remaining = Position;

		while (_nextTarget < targets.Length && remaining > 0)
		{
			var level = _entryPrice * (1m + targets[_nextTarget] / 100m);
			if (candle.HighPrice < level)
				break;

			var isLast = _nextTarget == targets.Length - 1;
			var quantity = isLast ? remaining : RoundVolume(remaining * shares[_nextTarget] / 100m);
			if (quantity >= remaining)
				quantity = remaining;

			if (quantity > 0)
			{
				SellMarket(quantity);
				remaining -= quantity;
			}

			_nextTarget++;
		}

		if (remaining <= 0)
			_entryPrice = 0;
	}

	private decimal RoundVolume(decimal volume)
	{
		if (Security?.VolumeStep is decimal step && step > 0)
			return Math.Floor(volume / step) * step;

		return volume;
	}
}
