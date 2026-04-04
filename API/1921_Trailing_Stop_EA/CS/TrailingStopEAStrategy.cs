using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop strategy with EMA crossover entry.
/// Opens positions based on fast/slow EMA crossover and manages them with trailing stop protection.
/// </summary>
public class TrailingStopEAStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _trailingPct;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _slowEma;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirst = true;
	private DateTimeOffset _lastTradeTime;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TrailingPct { get => _trailingPct.Value; set => _trailingPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrailingStopEAStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_trailingPct = Param(nameof(TrailingPct), 2m)
			.SetDisplay("Trailing %", "Trailing stop percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_slowEma = default;
		_prevFast = 0;
		_prevSlow = 0;
		_isFirst = true;
		_lastTradeTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isFirst = true;

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TrailingPct * 2, UnitTypes.Percent),
			new Unit(TrailingPct, UnitTypes.Percent),
			isStopTrailing: true
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var slowResult = _slowEma.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!slowResult.IsFormed)
			return;

		var slow = slowResult.ToDecimal();

		if (_isFirst)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isFirst = false;
			return;
		}

		var cooldown = TimeSpan.FromHours(24);
		var canTrade = _lastTradeTime == default || (candle.OpenTime - _lastTradeTime) >= cooldown;

		// EMA cross up -> buy
		if (_prevFast <= _prevSlow && fast > slow && Position <= 0 && canTrade)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_lastTradeTime = candle.OpenTime;
		}
		// EMA cross down -> sell
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0 && canTrade)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_lastTradeTime = candle.OpenTime;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
