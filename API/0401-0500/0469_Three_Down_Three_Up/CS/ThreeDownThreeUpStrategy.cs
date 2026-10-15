namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 3 Down, 3 Up Strategy.
/// Buys after N consecutive down closes (mean reversion).
/// Sells after N consecutive up closes.
/// Optional EMA trend filter.
/// </summary>
public class ThreeDownThreeUpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _buyTrigger;
	private readonly StrategyParam<int> _sellTrigger;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;

	private int _upCount;
	private int _downCount;
	private decimal _prevClose;
	private bool _hasPrevClose;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BuyTrigger
	{
		get => _buyTrigger.Value;
		set => _buyTrigger.Value = value;
	}

	public int SellTrigger
	{
		get => _sellTrigger.Value;
		set => _sellTrigger.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeDownThreeUpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_buyTrigger = Param(nameof(BuyTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Buy Trigger", "Consecutive down closes for entry", "Trading");

		_sellTrigger = Param(nameof(SellTrigger), 3)
			.SetGreaterThanZero()
			.SetDisplay("Sell Trigger", "Consecutive up closes for exit", "Trading");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_upCount = 0;
		_downCount = 0;
		_prevClose = 0;
		_hasPrevClose = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		// Track consecutive up/down closes
		if (_hasPrevClose)
		{
			if (candle.ClosePrice > _prevClose)
			{
				_upCount++;
				_downCount = 0;
			}
			else if (candle.ClosePrice < _prevClose)
			{
				_downCount++;
				_upCount = 0;
			}
			else
			{
				_upCount = 0;
				_downCount = 0;
			}
		}
		_prevClose = candle.ClosePrice;
		_hasPrevClose = true;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Buy after consecutive down closes (mean reversion)
		if (_downCount >= BuyTrigger && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_upCount = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Sell short after consecutive up closes
		else if (_upCount >= SellTrigger && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_downCount = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price below EMA
		else if (Position > 0 && candle.ClosePrice < emaVal && _upCount >= 2)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price above EMA
		else if (Position < 0 && candle.ClosePrice > emaVal && _downCount >= 2)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
