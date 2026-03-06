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
/// Breakout strategy that waits for Donchian channel contraction before trading a break of the previous channel.
/// </summary>
public class DonchianWithVolatilityContractionStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _volatilityFactor;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _donchianHigh;
	private Lowest _donchianLow;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _widthAverage;
	private StandardDeviation _widthStdDev;
	private decimal _previousHigh;
	private decimal _previousLow;
	private decimal _previousWidth;
	private decimal _widthAverageValue;
	private decimal _widthStdDevValue;
	private bool _isInitialized;
	private int _cooldown;

	/// <summary>
	/// Donchian channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for contraction detection.
	/// </summary>
	public decimal VolatilityFactor
	{
		get => _volatilityFactor.Value;
		set => _volatilityFactor.Value = value;
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
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public DonchianWithVolatilityContractionStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetRange(2, 100)
			.SetDisplay("Donchian Period", "Period for the Donchian channel", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("ATR Period", "Period for the ATR", "Indicators");

		_volatilityFactor = Param(nameof(VolatilityFactor), 0.8m)
			.SetRange(0.1m, 5m)
			.SetDisplay("Volatility Factor", "Standard deviation multiplier for contraction detection", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 72)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_donchianHigh = null;
		_donchianLow = null;
		_atr = null;
		_widthAverage = null;
		_widthStdDev = null;
		_previousHigh = 0m;
		_previousLow = 0m;
		_previousWidth = 0m;
		_widthAverageValue = 0m;
		_widthStdDevValue = 0m;
		_isInitialized = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_donchianHigh = new Highest { Length = DonchianPeriod };
		_donchianLow = new Lowest { Length = DonchianPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_widthAverage = new SimpleMovingAverage { Length = DonchianPeriod };
		_widthStdDev = new StandardDeviation { Length = DonchianPeriod };
		_isInitialized = false;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_donchianHigh, _donchianLow, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal donchianHigh, decimal donchianLow, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_donchianHigh.IsFormed || !_donchianLow.IsFormed || !_atr.IsFormed)
			return;

		if (!_isInitialized)
		{
			_previousHigh = donchianHigh;
			_previousLow = donchianLow;
			_previousWidth = donchianHigh - donchianLow;
			_widthAverageValue = _widthAverage.Process(_previousWidth, candle.OpenTime, true).ToDecimal();
			_widthStdDevValue = _widthStdDev.Process(_previousWidth, candle.OpenTime, true).ToDecimal();
			_isInitialized = true;
			return;
		}

		if (!_widthAverage.IsFormed || !_widthStdDev.IsFormed)
		{
			_previousHigh = donchianHigh;
			_previousLow = donchianLow;
			_previousWidth = donchianHigh - donchianLow;
			_widthAverageValue = _widthAverage.Process(_previousWidth, candle.OpenTime, true).ToDecimal();
			_widthStdDevValue = _widthStdDev.Process(_previousWidth, candle.OpenTime, true).ToDecimal();
			return;
		}

		if (ProcessState != ProcessStates.Started)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			UpdateChannelStatistics(candle, donchianHigh, donchianLow);
			return;
		}

		var price = candle.ClosePrice;
		var channelMiddle = (_previousHigh + _previousLow) / 2m;
		var volatilityThreshold = Math.Max(_widthAverageValue - VolatilityFactor * _widthStdDevValue, Security.PriceStep ?? 1m);
		var isVolatilityContracted = _previousWidth <= volatilityThreshold;

		if (Position == 0)
		{
			if (isVolatilityContracted && price >= _previousHigh + atrValue * 0.05m)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isVolatilityContracted && price <= _previousLow - atrValue * 0.05m)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (price <= channelMiddle)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (price >= channelMiddle)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}

		UpdateChannelStatistics(candle, donchianHigh, donchianLow);
	}

	private void UpdateChannelStatistics(ICandleMessage candle, decimal donchianHigh, decimal donchianLow)
	{
		_previousHigh = donchianHigh;
		_previousLow = donchianLow;
		_previousWidth = donchianHigh - donchianLow;
		_widthAverageValue = _widthAverage.Process(_previousWidth, candle.OpenTime, true).ToDecimal();
		_widthStdDevValue = _widthStdDev.Process(_previousWidth, candle.OpenTime, true).ToDecimal();
	}
}
