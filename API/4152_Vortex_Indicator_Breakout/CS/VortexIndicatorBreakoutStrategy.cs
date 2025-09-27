using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex crossover strategy converted from the MetaTrader expert Vortex Indicator System.
/// Detects VI+/VI- crossovers, arms breakout triggers on the crossover bar high/low,
/// and submits market orders when subsequent candles confirm the breakout level.
/// </summary>
public class VortexIndicatorBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _vortexLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;

	private VortexIndicator _vortex = null!;

	private decimal? _previousPlus;
	private decimal? _previousMinus;

	private decimal? _pendingLongTrigger;
	private decimal? _pendingShortTrigger;

	/// <summary>
	/// Initializes parameters for the Vortex Indicator System conversion.
	/// </summary>
public VortexIndicatorBreakoutStrategy()
	{
		_vortexLength = Param(nameof(VortexLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period applied to the Vortex indicator.", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(7, 35, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for candle and indicator calculations.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Market order size for new entries.", "Trading");
	}

	/// <summary>
	/// Vortex indicator length.
	/// </summary>
	public int VortexLength
	{
		get => _vortexLength.Value;
		set => _vortexLength.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator updates and breakout checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Market order volume used when entering new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_vortex = new VortexIndicator
		{
			Length = VortexLength
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_vortex, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal viPlus, decimal viMinus)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check whether a previously armed breakout level has been reached by the latest candle.
		if (_pendingLongTrigger is decimal longTrigger && candle.HighPrice >= longTrigger)
		{
			if (Position <= 0m)
			{
				var volume = Volume;
				if (Position < 0m)
					volume += Math.Abs(Position);

				BuyMarket(volume);
			}

			_pendingLongTrigger = null;
		}
		else if (_pendingShortTrigger is decimal shortTrigger && candle.LowPrice <= shortTrigger)
		{
			if (Position >= 0m)
			{
				var volume = Volume;
				if (Position > 0m)
					volume += Position;

				SellMarket(volume);
			}

			_pendingShortTrigger = null;
		}

		if (!_vortex.IsFormed)
		{
			_previousPlus = viPlus;
			_previousMinus = viMinus;
			return;
		}

		if (_previousPlus is not decimal prevPlus || _previousMinus is not decimal prevMinus)
		{
			_previousPlus = viPlus;
			_previousMinus = viMinus;
			return;
		}

		// Detect a bullish crossover where VI+ rises above VI- after being below it.
		if (prevPlus <= prevMinus && viPlus > viMinus)
		{
			if (Position < 0m)
				BuyMarket(Math.Abs(Position));

			_pendingLongTrigger = candle.HighPrice;
			_pendingShortTrigger = null;
		}
		// Detect a bearish crossover where VI- rises above VI+ after being below it.
		else if (prevMinus <= prevPlus && viMinus > viPlus)
		{
			if (Position > 0m)
				SellMarket(Math.Abs(Position));

			_pendingShortTrigger = candle.LowPrice;
			_pendingLongTrigger = null;
		}

		_previousPlus = viPlus;
		_previousMinus = viMinus;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_pendingLongTrigger = null;
		_pendingShortTrigger = null;

		base.OnStopped();
	}
}
