namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Elliott Wave strategy with Supertrend exits and fixed percentage stop-loss.
/// </summary>
public class ElliottWaveSupertrendExitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _waveLength;
	private readonly StrategyParam<int> _supertrendLength;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;

	private SuperTrend _supertrend;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _longStop;
	private decimal _shortStop;
	private decimal _prevSupertrendDir;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int WaveLength
	{
		get => _waveLength.Value;
		set => _waveLength.Value = value;
	}

	public int SupertrendLength
	{
		get => _supertrendLength.Value;
		set => _supertrendLength.Value = value;
	}

	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	public ElliottWaveSupertrendExitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");

		_waveLength = Param(nameof(WaveLength), 4)
			.SetDisplay("Wave Length", "Period for highest/lowest detection", "Wave");

		_supertrendLength = Param(nameof(SupertrendLength), 10)
			.SetDisplay("Supertrend Length", "ATR period for Supertrend", "Supertrend");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend", "Supertrend");

		_stopLossPercent = Param(nameof(StopLossPercent), 10m)
			.SetDisplay("Stop Loss Percent", "Fixed stop-loss percentage", "Risk");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Long Entries", "Enable long trades", "Strategy");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Short Entries", "Enable short trades", "Strategy");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_longStop = default;
		_shortStop = default;
		_prevSupertrendDir = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = WaveLength };
		_lowest = new Lowest { Length = WaveLength };
		_supertrend = new SuperTrend { Length = SupertrendLength, Multiplier = SupertrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, _supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal supertrend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed || !_supertrend.IsFormed)
			return;

		var price = candle.ClosePrice;

		var isPivotLow = price <= lowest;
		var isPivotHigh = price >= highest;

		if (EnableLong && isPivotLow && Position <= 0)
		{
			_longStop = price * (1 - StopLossPercent / 100m);
			BuyMarket();
		}
		else if (EnableShort && isPivotHigh && Position >= 0)
		{
			_shortStop = price * (1 + StopLossPercent / 100m);
			SellMarket();
		}

		if (Position > 0 && candle.LowPrice <= _longStop)
			ClosePosition();
		else if (Position < 0 && candle.HighPrice >= _shortStop)
			ClosePosition();

		var dir = price > supertrend ? -1m : 1m;
		if (Position > 0 && dir > 0 && _prevSupertrendDir < 0)
			ClosePosition();
		else if (Position < 0 && dir < 0 && _prevSupertrendDir > 0)
			ClosePosition();

		_prevSupertrendDir = dir;
	}
}
