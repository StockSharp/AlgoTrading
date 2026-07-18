namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Averaging Down Strategy.
/// Uses EMA + ATR bands to detect entry zones, then averages down
/// if price moves against position. Takes profit at target %.
/// </summary>
public class AveragingDownStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	private decimal _entryPrice;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public AveragingDownStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR band multiplier", "Indicators");

		_tpPercent = Param(nameof(TpPercent), 2m)
			.SetDisplay("TP %", "Take profit percent", "Trading");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_atr = null;
		_entryPrice = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _atr, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var price = candle.ClosePrice;
		var upperBand = emaVal + atrVal * AtrMultiplier;
		var lowerBand = emaVal - atrVal * AtrMultiplier;

		// Entry: price breaks above upper band -> buy
		if (price > upperBand && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = price;
			_cooldownRemaining = CooldownBars;
		}
		// Entry: price breaks below lower band -> sell
		else if (price < lowerBand && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = price;
			_cooldownRemaining = CooldownBars;
		}
		// Take profit on long
		else if (Position > 0 && _entryPrice > 0 && price >= _entryPrice * (1 + TpPercent / 100m))
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Take profit on short
		else if (Position < 0 && _entryPrice > 0 && price <= _entryPrice * (1 - TpPercent / 100m))
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Exit long at EMA (stop)
		else if (Position > 0 && price < emaVal)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Exit short at EMA (stop)
		else if (Position < 0 && price > emaVal)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
	}
}
