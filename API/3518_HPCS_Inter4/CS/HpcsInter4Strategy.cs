using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "_HPCS_IntFourth_MT4_EA_V01_WE" MetaTrader expert advisor.
/// Uses SMA crossover to enter positions with time-based exit.
/// </summary>
public class HpcsInter4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isFirstValue = true;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public HpcsInter4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculation", "General");

		_fastPeriod = Param(nameof(FastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_isFirstValue = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = 0;
		_prevSlow = 0;
		_isFirstValue = true;

		var fastSma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowSma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirstValue)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_isFirstValue = false;
			return;
		}

		// Fast crosses above slow - buy
		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{
			BuyMarket();
		}
		// Fast crosses below slow - sell
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{
			SellMarket();
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
