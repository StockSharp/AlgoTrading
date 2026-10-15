using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AK47 A1 strategy - Alligator-like triple SMA crossover.
/// Buys when fast SMA crosses above medium and medium is above slow.
/// Sells when fast SMA crosses below medium and medium is below slow.
/// </summary>
public class AK47A1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _medPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMed;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MedPeriod { get => _medPeriod.Value; set => _medPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AK47A1Strategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast SMA", "Lips period", "Indicators");

		_medPeriod = Param(nameof(MedPeriod), 8)
			.SetDisplay("Medium SMA", "Teeth period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 13)
			.SetDisplay("Slow SMA", "Jaw period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];
	protected override void OnReseted() { base.OnReseted(); _prevFast = 0m; _prevMed = 0m; _hasPrev = false; }

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var med = new SimpleMovingAverage { Length = MedPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, med, slow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal med, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevMed = med;
			_hasPrev = true;
			return;
		}

		if (_prevFast <= _prevMed && fast > med && med > slow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevFast >= _prevMed && fast < med && med < slow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevFast = fast;
		_prevMed = med;
	}
}
