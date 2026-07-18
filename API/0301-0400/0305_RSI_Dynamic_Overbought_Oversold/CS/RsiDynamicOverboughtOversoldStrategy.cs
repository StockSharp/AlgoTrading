using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy with dynamic overbought and oversold bands derived from the rolling mean and volatility of RSI.
/// </summary>
public class RsiDynamicOverboughtOversoldStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _movingAvgPeriod;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _priceSma;
	private SimpleMovingAverage _rsiSma;
	private StandardDeviation _rsiStdDev;
	private int _cooldown;

	/// <summary>
	/// Period for RSI calculation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Period for moving averages and RSI volatility.
	/// </summary>
	public int MovingAvgPeriod
	{
		get => _movingAvgPeriod.Value;
		set => _movingAvgPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier used for the dynamic RSI bands.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdDevMultiplier.Value;
		set => _stdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public RsiDynamicOverboughtOversoldStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators");

		_movingAvgPeriod = Param(nameof(MovingAvgPeriod), 34)
			.SetRange(5, 200)
			.SetDisplay("Average Period", "Period for moving averages and RSI volatility", "Indicators");

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 1.3m)
			.SetRange(0.1m, 5m)
			.SetDisplay("StdDev Multiplier", "Multiplier for the dynamic RSI bands", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 48)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_priceSma = null;
		_rsiSma = null;
		_rsiStdDev = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_priceSma = new SimpleMovingAverage { Length = MovingAvgPeriod };
		_rsiSma = new SimpleMovingAverage { Length = MovingAvgPeriod };
		_rsiStdDev = new StandardDeviation { Length = MovingAvgPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_rsi, _priceSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _priceSma);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal priceSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiAverageValue = _rsiSma.Process(rsiValue, candle.OpenTime, true).ToDecimal();
		var rsiStdDevValue = _rsiStdDev.Process(rsiValue, candle.OpenTime, true).ToDecimal();

		if (!_rsi.IsFormed || !_priceSma.IsFormed || !_rsiSma.IsFormed || !_rsiStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var dynamicOverbought = Math.Min(rsiAverageValue + StdDevMultiplier * rsiStdDevValue, 85m);
		var dynamicOversold = Math.Max(rsiAverageValue - StdDevMultiplier * rsiStdDevValue, 15m);
		var price = candle.ClosePrice;
		var bullishFilter = price >= priceSmaValue * 0.995m;
		var bearishFilter = price <= priceSmaValue * 1.005m;

		if (Position == 0)
		{
			if (rsiValue <= dynamicOversold && bullishFilter)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (rsiValue >= dynamicOverbought && bearishFilter)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		if (Position > 0 && (rsiValue >= rsiAverageValue || price < priceSmaValue * 0.995m))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (rsiValue <= rsiAverageValue || price > priceSmaValue * 1.005m))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
