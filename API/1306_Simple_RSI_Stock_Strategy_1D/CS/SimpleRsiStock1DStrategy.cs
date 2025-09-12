using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple RSI stock strategy using daily candles with ATR stop and three profit targets.
/// </summary>
public class SimpleRsiStock1DStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _takeProfit1;
	private readonly StrategyParam<decimal> _takeProfit2;
	private readonly StrategyParam<decimal> _takeProfit3;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _atrStopLevel;
	private decimal _tp1Level;
	private decimal _tp2Level;
	private decimal _tp3Level;
	private bool _tp1Hit;
	private bool _tp2Hit;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level considered oversold.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Length of the SMA filter.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop level.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// First take profit percentage.
	/// </summary>
	public decimal TakeProfit1
	{
		get => _takeProfit1.Value;
		set => _takeProfit1.Value = value;
	}

	/// <summary>
	/// Second take profit percentage.
	/// </summary>
	public decimal TakeProfit2
	{
		get => _takeProfit2.Value;
		set => _takeProfit2.Value = value;
	}

	/// <summary>
	/// Third take profit percentage.
	/// </summary>
	public decimal TakeProfit3
	{
		get => _takeProfit3.Value;
		set => _takeProfit3.Value = value;
	}

	/// <summary>
	/// Basic percentage stop loss.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SimpleRsiStock1DStrategy"/>.
	/// </summary>
	public SimpleRsiStock1DStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 5)
		.SetRange(2, 30)
		.SetDisplay("RSI Period", "Number of bars for RSI", "Indicator Parameters")
		.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 30)
		.SetRange(10, 50)
		.SetDisplay("Oversold Level", "RSI value considered oversold", "Signal Parameters")
		.SetCanOptimize(true);

		_smaLength = Param(nameof(SmaLength), 200)
		.SetRange(50, 300)
		.SetDisplay("SMA Length", "Period for SMA filter", "Indicator Parameters")
		.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 20)
		.SetRange(5, 40)
		.SetDisplay("ATR Length", "Bars for ATR calculation", "Risk Management")
		.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetRange(1m, 3m)
		.SetDisplay("ATR Multiplier", "Multiplier for ATR stop", "Risk Management")
		.SetCanOptimize(true);

		_takeProfit1 = Param(nameof(TakeProfit1), 5m)
		.SetRange(1m, 10m)
		.SetDisplay("Take Profit 1 %", "First take profit percentage", "Risk Management")
		.SetCanOptimize(true);

		_takeProfit2 = Param(nameof(TakeProfit2), 10m)
		.SetRange(5m, 20m)
		.SetDisplay("Take Profit 2 %", "Second take profit percentage", "Risk Management")
		.SetCanOptimize(true);

		_takeProfit3 = Param(nameof(TakeProfit3), 15m)
		.SetRange(10m, 30m)
		.SetDisplay("Take Profit 3 %", "Third take profit percentage", "Risk Management")
		.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 25m)
		.SetRange(5m, 50m)
		.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk Management")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
		_entryPrice = 0m;
		_atrStopLevel = 0m;
		_tp1Level = 0m;
		_tp2Level = 0m;
		_tp3Level = 0m;
		_tp1Hit = false;
		_tp2Hit = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var sma = new SimpleMovingAverage
		{
			Length = SmaLength
		};

		var atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, sma, atr, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(0m, UnitTypes.Absolute),
			new Unit(StopLossPercent, UnitTypes.Percent),
			false);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi, sma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position <= 0)
		{
			if (rsiValue < OversoldLevel && candle.ClosePrice > smaValue)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_atrStopLevel = _entryPrice - AtrMultiplier * atrValue;
				_tp1Level = _entryPrice * (1 + TakeProfit1 / 100m);
				_tp2Level = _entryPrice * (1 + TakeProfit2 / 100m);
				_tp3Level = _entryPrice * (1 + TakeProfit3 / 100m);
				_tp1Hit = false;
				_tp2Hit = false;
			}
		}
		else
		{
			if (candle.ClosePrice <= _atrStopLevel)
			{
				SellMarket(Position);
				ResetTargets();
				return;
			}

			if (!_tp1Hit && candle.ClosePrice >= _tp1Level)
			{
				SellMarket(Position * 0.33m);
				_tp1Hit = true;
			}
			else if (!_tp2Hit && candle.ClosePrice >= _tp2Level)
			{
				SellMarket(Position * 0.66m);
				_tp2Hit = true;
			}
			else if (candle.ClosePrice >= _tp3Level)
			{
				SellMarket(Position);
				ResetTargets();
			}
		}
	}

	private void ResetTargets()
	{
		_entryPrice = 0m;
		_atrStopLevel = 0m;
		_tp1Level = 0m;
		_tp2Level = 0m;
		_tp3Level = 0m;
		_tp1Hit = false;
		_tp2Hit = false;
	}
}
