using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Volume strategy with zero-line crossings and volume confirmation.
/// Opens reversal trades when MACD crosses zero with volume oscillator and MACD state filter.
/// </summary>
public class MacdVolumeBboReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeShortLength;
	private readonly StrategyParam<int> _volumeLongLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private Lowest _lowest;
	private Highest _highest;

	/// <summary>
	/// Volume short EMA length.
	/// </summary>
	public int VolumeShortLength
	{
		get => _volumeShortLength.Value;
		set => _volumeShortLength.Value = value;
	}

	/// <summary>
	/// Volume long EMA length.
	/// </summary>
	public int VolumeLongLength
	{
		get => _volumeLongLength.Value;
		set => _volumeLongLength.Value = value;
	}

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal smoothing length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Lookback period for recent high/low.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MacdVolumeBboReversalStrategy"/>.
	/// </summary>
	public MacdVolumeBboReversalStrategy()
	{
		_volumeShortLength = Param(nameof(VolumeShortLength), 6)
			.SetDisplay("Vol Short", "Volume short EMA length", "Volume");
		_volumeLongLength = Param(nameof(VolumeLongLength), 12)
			.SetDisplay("Vol Long", "Volume long EMA length", "Volume");
		_macdFastLength = Param(nameof(MacdFastLength), 11)
			.SetDisplay("MACD Fast", "MACD fast length", "MACD");
		_macdSlowLength = Param(nameof(MacdSlowLength), 21)
			.SetDisplay("MACD Slow", "MACD slow length", "MACD");
		_macdSignalLength = Param(nameof(MacdSignalLength), 10)
			.SetDisplay("MACD Signal", "MACD signal length", "MACD");
		_lookbackPeriod = Param(nameof(LookbackPeriod), 10)
			.SetDisplay("Lookback", "Bars for recent high/low", "Risk");
		_riskReward = Param(nameof(RiskReward), 1.5m)
			.SetDisplay("Risk Reward", "Take profit to stop ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevMacd = 0m;
		_lowest = default;
		_highest = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lowest = new Lowest { Length = LookbackPeriod };
		_highest = new Highest { Length = LookbackPeriod };
		var shortVol = new ExponentialMovingAverage { Length = VolumeShortLength };
		var longVol = new ExponentialMovingAverage { Length = VolumeLongLength };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortVol, longVol, macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle,
	decimal shortVol, decimal longVol,
	decimal macd, decimal signal, decimal _)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var lowValue = _lowest.Process(candle.LowPrice);
	var highValue = _highest.Process(candle.HighPrice);

	if (!lowValue.IsFinal || !highValue.IsFinal)
	{
	_prevMacd = macd;
	return;
	}

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_prevMacd = macd;
	return;
	}

	var lastLow = lowValue.ToDecimal();
	var lastHigh = highValue.ToDecimal();
	var osc = longVol == 0m ? 0m : 100m * (shortVol - longVol) / longVol;
	var macdAbove = macd > signal;
	var macdBelow = macd < signal;
	var bboBuy = _prevMacd <= 0m && macd > 0m && osc > 0m;
	var bboSell = _prevMacd >= 0m && macd < 0m && osc > 0m;
	var longSignal = bboBuy && macdAbove;
	var shortSignal = bboSell && macdBelow;

	if (longSignal && Position <= 0)
	{
	var volume = Volume + Math.Abs(Position);
	var entryPrice = candle.ClosePrice;
	var sl = lastLow;
	var tp = entryPrice + (entryPrice - sl) * RiskReward;

	BuyMarket(volume);
	SellStop(volume, sl);
	SellLimit(volume, tp);
	}
	else if (shortSignal && Position >= 0)
	{
	var volume = Volume + Math.Abs(Position);
	var entryPrice = candle.ClosePrice;
	var sl = lastHigh;
	var tp = entryPrice - (sl - entryPrice) * RiskReward;

	SellMarket(volume);
	BuyStop(volume, sl);
	BuyLimit(volume, tp);
	}

	_prevMacd = macd;
	}
}
