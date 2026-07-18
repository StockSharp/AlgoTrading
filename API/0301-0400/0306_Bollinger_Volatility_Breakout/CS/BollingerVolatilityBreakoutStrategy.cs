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
/// Breakout strategy that trades Bollinger band breaks only when ATR expands beyond its recent regime.
/// </summary>
public class BollingerVolatilityBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrDeviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bollingerBands;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrSma;
	private StandardDeviation _atrStdDev;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private int _cooldown;

	/// <summary>
	/// Period for Bollinger bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	/// ATR standard deviation multiplier used for volatility confirmation.
	/// </summary>
	public decimal AtrDeviationMultiplier
	{
		get => _atrDeviationMultiplier.Value;
		set => _atrDeviationMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier used for stop distance.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
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
	public BollingerVolatilityBreakoutStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(5, 100)
			.SetDisplay("Bollinger Period", "Period for Bollinger band calculation", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger bands", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_atrDeviationMultiplier = Param(nameof(AtrDeviationMultiplier), 1.6m)
			.SetRange(0.1m, 5m)
			.SetDisplay("ATR Deviation Multiplier", "ATR regime threshold multiplier", "Signals");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 1.8m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss Multiplier", "ATR multiplier used for stop distance", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 84)
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

		_bollingerBands = null;
		_atr = null;
		_atrSma = null;
		_atrStdDev = null;
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

		_bollingerBands = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation,
		};
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrSma = new SimpleMovingAverage { Length = AtrPeriod };
		_atrStdDev = new StandardDeviation { Length = AtrPeriod };
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_bollingerBands, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollingerBands);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossMultiplier, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typedBands = (BollingerBandsValue)bollingerValue;

		if (typedBands.UpBand is not decimal upperBand ||
			typedBands.LowBand is not decimal lowerBand ||
			typedBands.MovingAverage is not decimal middleBand)
			return;

		var atrValue = _atr.Process(candle).ToDecimal();
		var atrAverageValue = _atrSma.Process(atrValue, candle.OpenTime, true).ToDecimal();
		var atrStdDevValue = _atrStdDev.Process(atrValue, candle.OpenTime, true).ToDecimal();

		if (!_bollingerBands.IsFormed || !_atr.IsFormed || !_atrSma.IsFormed || !_atrStdDev.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var volatilityThreshold = atrAverageValue + AtrDeviationMultiplier * atrStdDevValue;
		var isHighVolatility = atrValue >= volatilityThreshold;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (!isHighVolatility)
				return;

			if (price >= upperBand)
			{
				_entryPrice = price;
				_entryAtr = atrValue;
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (price <= lowerBand)
			{
				_entryPrice = price;
				_entryAtr = atrValue;
				SellMarket();
				_cooldown = CooldownBars;
			}

			return;
		}

		var stopDistance = _entryAtr * StopLossMultiplier;

		if (Position > 0)
		{
			if (price <= middleBand || !isHighVolatility || price <= _entryPrice - stopDistance)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (price >= middleBand || !isHighVolatility || price >= _entryPrice + stopDistance)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
	}
}
