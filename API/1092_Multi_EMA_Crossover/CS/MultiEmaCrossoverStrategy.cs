namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi EMA crossover strategy.
/// Opens long on bullish crossover and exits on bearish crossover.
/// </summary>
public class MultiEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrevValues;
	private int _cooldownRemaining;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiEmaCrossoverStrategy()
	{
		_fastLength = Param(nameof(FastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Parameters");

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Parameters");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrevValues = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevValues)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrevValues = true;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (Position > 0 && _prevFast >= _prevSlow && fastValue < slowValue)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (Position == 0 && _cooldownRemaining == 0 && _prevFast <= _prevSlow && fastValue > slowValue)
		{
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
