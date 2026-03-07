namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-Timeframe Bollinger Bands Strategy.
/// Uses two BB periods: a short-period BB for exit signals
/// and a long-period BB (simulating higher timeframe) for entry signals.
/// Buys when price touches long-period lower BB, exits at short-period upper BB.
/// </summary>
public class MtfBbStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbShortLength;
	private readonly StrategyParam<int> _bbLongLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private BollingerBands _bbShort;
	private BollingerBands _bbLong;

	private int _cooldownRemaining;

	public MtfBbStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbShortLength = Param(nameof(BbShortLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Short Length", "Short-period Bollinger Bands", "Bollinger Bands");

		_bbLongLength = Param(nameof(BbLongLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("BB Long Length", "Long-period Bollinger Bands (MTF proxy)", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int BbShortLength
	{
		get => _bbShortLength.Value;
		set => _bbShortLength.Value = value;
	}

	public int BbLongLength
	{
		get => _bbLongLength.Value;
		set => _bbLongLength.Value = value;
	}

	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bbShort = null;
		_bbLong = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bbShort = new BollingerBands { Length = BbShortLength, Width = BBMultiplier };
		_bbLong = new BollingerBands { Length = BbLongLength, Width = BBMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_bbShort, _bbLong, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bbShort);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bbShortValue, IIndicatorValue bbLongValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bbShort.IsFormed || !_bbLong.IsFormed)
			return;

		if (bbShortValue.IsEmpty || bbLongValue.IsEmpty)
			return;

		var bbShort = (BollingerBandsValue)bbShortValue;
		var bbLong = (BollingerBandsValue)bbLongValue;

		if (bbShort.UpBand is not decimal shortUpper || bbShort.LowBand is not decimal shortLower)
			return;
		if (bbLong.UpBand is not decimal longUpper || bbLong.LowBand is not decimal longLower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Buy: price touches long-period lower BB (oversold on higher timeframe)
		if (candle.ClosePrice <= longLower && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price touches long-period upper BB (overbought on higher timeframe)
		else if (candle.ClosePrice >= longUpper && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long at short-period upper BB
		else if (Position > 0 && candle.ClosePrice >= shortUpper)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short at short-period lower BB
		else if (Position < 0 && candle.ClosePrice <= shortLower)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
