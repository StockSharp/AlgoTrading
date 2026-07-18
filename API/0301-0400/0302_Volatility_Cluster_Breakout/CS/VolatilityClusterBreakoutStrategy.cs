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
/// Breakout strategy that trades only when ATR expands into a high-volatility cluster.
/// </summary>
public class VolatilityClusterBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _priceAvgPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private StandardDeviation _stdDev;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrAvg;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private int _cooldown;

	/// <summary>
	/// Period for the moving average and standard deviation.
	/// </summary>
	public int PriceAvgPeriod
	{
		get => _priceAvgPeriod.Value;
		set => _priceAvgPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier used for breakout levels.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdDevMultiplier.Value;
		set => _stdDevMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier used for stop distance.
	/// </summary>
	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
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
	public VolatilityClusterBreakoutStrategy()
	{
		_priceAvgPeriod = Param(nameof(PriceAvgPeriod), 20)
			.SetRange(5, 100)
			.SetDisplay("Price Average Period", "Period for moving average and standard deviation", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 1.3m)
			.SetRange(0.25m, 5m)
			.SetDisplay("StdDev Multiplier", "Multiplier for breakout levels", "Signals");

		_stopMultiplier = Param(nameof(StopMultiplier), 1.8m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop ATR Multiplier", "ATR multiplier used for stop distance", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 60)
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

		_sma = null;
		_stdDev = null;
		_atr = null;
		_atrAvg = null;
		_entryPrice = 0m;
		_entryAtr = 0m;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_sma = new SimpleMovingAverage { Length = PriceAvgPeriod };
		_stdDev = new StandardDeviation { Length = PriceAvgPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrAvg = new SimpleMovingAverage { Length = Math.Max(AtrPeriod * 2, 10) };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_sma, _stdDev, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopMultiplier, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrAvgValue = _atrAvg.Process(atrValue, candle.OpenTime, true).ToDecimal();

		if (!_sma.IsFormed || !_stdDev.IsFormed || !_atr.IsFormed || !_atrAvg.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var upperLevel = smaValue + StdDevMultiplier * stdDevValue;
		var lowerLevel = smaValue - StdDevMultiplier * stdDevValue;
		var isHighVolatility = atrValue >= atrAvgValue * 1.15m;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (!isHighVolatility)
				return;

			if (price >= upperLevel)
			{
				_entryPrice = price;
				_entryAtr = atrValue;
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (price <= lowerLevel)
			{
				_entryPrice = price;
				_entryAtr = atrValue;
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		var stopDistance = _entryAtr * StopMultiplier;

		if (Position > 0)
		{
			if (price <= smaValue || !isHighVolatility || price <= _entryPrice - stopDistance)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (price >= smaValue || !isHighVolatility || price >= _entryPrice + stopDistance)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
	}
}
