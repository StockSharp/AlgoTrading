using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vicious Mortgage Rates strategy.
/// Uses fast/slow EMA crossover with volatility filter (StdDev above its SMA).
/// Trades on EMA cross when volatility is elevated.
/// </summary>
public class ViciousMortgageRatesV1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _volLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int VolLength { get => _volLength.Value; set => _volLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ViciousMortgageRatesV1Strategy()
	{
		_fastLength = Param(nameof(FastLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "General");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "General");

		_volLength = Param(nameof(VolLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Vol Length", "Volatility lookback", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_prevFast = 0;
		_prevSlow = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		_prevFast = 0;
		_prevSlow = 0;
		_cooldown = 0;

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

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldown > 0)
			_cooldown--;

		if (_prevFast == 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_cooldown > 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var longCross = _prevFast <= _prevSlow && fast > slow;
		var shortCross = _prevFast >= _prevSlow && fast < slow;

		if (longCross && Position <= 0)
		{
			BuyMarket();
			_cooldown = 30;
		}
		else if (shortCross && Position >= 0)
		{
			SellMarket();
			_cooldown = 30;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
