namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving Average Crossover with Parabolic SAR filter and ATR stop.
/// Buys when fast MA > slow MA + price > fast MA + price > PSAR.
/// Sells when fast MA < slow MA + price < fast MA + price < PSAR.
/// Uses ATR-based stop loss.
/// </summary>
public class MaPsarAtrTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private AverageTrueRange _atr;
	private ParabolicSar _psar;

	private decimal _stopPrice;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public MaPsarAtrTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Fast EMA period", "MA");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Slow EMA period", "MA");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "ATR");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR stop multiplier", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null;
		_slowMa = null;
		_atr = null;
		_psar = null;
		_stopPrice = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_psar = new ParabolicSar();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _atr, _psar, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal atrVal, decimal psarVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_atr.IsFormed || !_psar.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var price = candle.ClosePrice;
		var bullishTrend = fastVal > slowVal && price > fastVal;
		var bearishTrend = fastVal < slowVal && price < fastVal;
		var psarBull = price > psarVal;
		var psarBear = price < psarVal;

		// Check stop loss
		if (Position > 0 && price <= _stopPrice)
		{
			SellMarket(Math.Abs(Position));
			_stopPrice = 0;
			_cooldownRemaining = CooldownBars;
			return;
		}
		else if (Position < 0 && price >= _stopPrice)
		{
			BuyMarket(Math.Abs(Position));
			_stopPrice = 0;
			_cooldownRemaining = CooldownBars;
			return;
		}

		// Entry long
		if (bullishTrend && psarBull && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_stopPrice = price - atrVal * AtrMultiplier;
			_cooldownRemaining = CooldownBars;
		}
		// Entry short
		else if (bearishTrend && psarBear && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_stopPrice = price + atrVal * AtrMultiplier;
			_cooldownRemaining = CooldownBars;
		}
		// Exit long on trend reversal
		else if (Position > 0 && bearishTrend)
		{
			SellMarket(Math.Abs(Position));
			_stopPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Exit short on trend reversal
		else if (Position < 0 && bullishTrend)
		{
			BuyMarket(Math.Abs(Position));
			_stopPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
	}
}
