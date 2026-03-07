namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Liquidity Swings Strategy.
/// Uses recent pivot highs/lows as resistance/support levels.
/// Enters on bounce from support/resistance with risk-reward.
/// </summary>
public class LiquiditySwingsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;

	private readonly List<decimal> _highBuffer = new();
	private readonly List<decimal> _lowBuffer = new();

	private decimal _resistance;
	private decimal _support;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
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

	public LiquiditySwingsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lookback = Param(nameof(Lookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Lookback", "Pivot detection lookback", "Parameters");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

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

		_ema = null;
		_highBuffer.Clear();
		_lowBuffer.Clear();
		_resistance = 0;
		_support = 0;
		_entryPrice = 0;
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

		UpdatePivotLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (_resistance == 0 || _support == 0)
			return;

		var price = candle.ClosePrice;

		// Buy: price near support, bounce up, trend filter (price > ema)
		if (price > _support && price < (_support + (_resistance - _support) * 0.3m) && price > emaVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = price;
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price near resistance, drop, trend filter (price < ema)
		else if (price < _resistance && price > (_resistance - (_resistance - _support) * 0.3m) && price < emaVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = price;
			_cooldownRemaining = CooldownBars;
		}
		// Exit long at resistance
		else if (Position > 0 && price >= _resistance)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Exit short at support
		else if (Position < 0 && price <= _support)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Stop loss long: price breaks below support
		else if (Position > 0 && price < _support)
		{
			SellMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Stop loss short: price breaks above resistance
		else if (Position < 0 && price > _resistance)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
	}

	private void UpdatePivotLevels(ICandleMessage candle)
	{
		var size = Lookback * 2 + 1;

		_highBuffer.Add(candle.HighPrice);
		_lowBuffer.Add(candle.LowPrice);

		if (_highBuffer.Count > size)
			_highBuffer.RemoveAt(0);

		if (_lowBuffer.Count > size)
			_lowBuffer.RemoveAt(0);

		if (_highBuffer.Count == size)
		{
			var center = Lookback;
			var candidate = _highBuffer[center];
			var isPivot = true;

			for (var i = 0; i < size; i++)
			{
				if (i == center)
					continue;
				if (_highBuffer[i] >= candidate)
				{
					isPivot = false;
					break;
				}
			}

			if (isPivot)
				_resistance = candidate;
		}

		if (_lowBuffer.Count == size)
		{
			var center = Lookback;
			var candidate = _lowBuffer[center];
			var isPivot = true;

			for (var i = 0; i < size; i++)
			{
				if (i == center)
					continue;
				if (_lowBuffer[i] <= candidate)
				{
					isPivot = false;
					break;
				}
			}

			if (isPivot)
				_support = candidate;
		}
	}
}
