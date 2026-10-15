namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 2X SPY TIPS Strategy (single-security adaptation).
/// Uses dual SMA to detect strong uptrend, then buys.
/// Original requires SPY+TIPS, adapted to single security with fast/slow SMA.
/// </summary>
public class TwoXSpyTipsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;

	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastSmaLength
	{
		get => _fastSmaLength.Value;
		set => _fastSmaLength.Value = value;
	}

	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public TwoXSpyTipsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastSmaLength = Param(nameof(FastSmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_slowSmaLength = Param(nameof(SlowSmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastSma = null;
		_slowSma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		_slowSma = new SimpleMovingAverage { Length = SlowSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastSma.IsFormed || !_slowSma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var price = candle.ClosePrice;

		// Strong uptrend: price above both SMAs + fast > slow
		if (price > fastVal && price > slowVal && fastVal > slowVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Strong downtrend: price below both SMAs + fast < slow
		else if (price < fastVal && price < slowVal && fastVal < slowVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price drops below fast SMA
		else if (Position > 0 && price < fastVal)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price rises above fast SMA
		else if (Position < 0 && price > fastVal)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
