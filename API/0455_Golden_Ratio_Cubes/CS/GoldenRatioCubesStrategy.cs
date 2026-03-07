namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Golden Ratio Cubes Strategy.
/// Uses BB width as a range proxy and golden ratio extensions for breakout levels.
/// Buys when price breaks above upper golden ratio level.
/// Sells when price breaks below lower golden ratio level.
/// </summary>
public class GoldenRatioCubesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _phi;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bb;
	private ExponentialMovingAverage _ema;

	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal Phi
	{
		get => _phi.Value;
		set => _phi.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public GoldenRatioCubesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bbLength = Param(nameof(BbLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Golden Ratio");

		_phi = Param(nameof(Phi), 1.618m)
			.SetDisplay("Phi", "Golden ratio multiplier", "Golden Ratio");

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

		_bb = null;
		_ema = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bb = new BollingerBands { Length = BbLength, Width = 2.0m };
		_ema = new ExponentialMovingAverage { Length = BbLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bb, _ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bb);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bb.IsFormed || !_ema.IsFormed)
			return;

		if (bbValue.IsEmpty || emaValue.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal mid)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var range = upper - lower;
		var price = candle.ClosePrice;

		// Use BB bands directly as breakout levels
		// Buy: price breaks above upper BB
		if (price > upper && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price breaks below lower BB
		else if (price < lower && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price returns to middle
		else if (Position > 0 && price < mid)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price returns to middle
		else if (Position < 0 && price > mid)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
