namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Three EMA Cross Strategy.
/// Uses fast/slow EMA crossover with trend EMA filter.
/// Buys when fast EMA crosses above slow EMA while above trend EMA.
/// Sells when fast EMA crosses below slow EMA while below trend EMA.
/// </summary>
public class ThreeEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _trendEmaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _trendEma;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	public int TrendEmaLength
	{
		get => _trendEmaLength.Value;
		set => _trendEmaLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeEmaCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Moving Averages");

		_slowEmaLength = Param(nameof(SlowEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Moving Averages");

		_trendEmaLength = Param(nameof(TrendEmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend EMA length", "Moving Averages");

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

		_fastEma = null;
		_slowEma = null;
		_trendEma = null;
		_prevFastEma = 0;
		_prevSlowEma = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_trendEma = new ExponentialMovingAverage { Length = TrendEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _trendEma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _trendEma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal trendEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_trendEma.IsFormed)
		{
			_prevFastEma = fastEma;
			_prevSlowEma = slowEma;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFastEma = fastEma;
			_prevSlowEma = slowEma;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevFastEma = fastEma;
			_prevSlowEma = slowEma;
			return;
		}

		if (_prevFastEma == 0 || _prevSlowEma == 0)
		{
			_prevFastEma = fastEma;
			_prevSlowEma = slowEma;
			return;
		}

		// EMA crossovers
		var crossUp = fastEma > slowEma && _prevFastEma <= _prevSlowEma;
		var crossDown = fastEma < slowEma && _prevFastEma >= _prevSlowEma;

		// Buy: fast crosses above slow + price above trend EMA
		if (crossUp && candle.ClosePrice > trendEma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: fast crosses below slow + price below trend EMA
		else if (crossDown && candle.ClosePrice < trendEma && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: fast crosses below slow
		else if (Position > 0 && crossDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: fast crosses above slow
		else if (Position < 0 && crossUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevFastEma = fastEma;
		_prevSlowEma = slowEma;
	}
}
