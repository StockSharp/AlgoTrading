using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two timeframe moving average crossover strategy.
/// </summary>
public class DiffTfMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<bool> _reverseSignals;

	private SMA _baseMa;
	private SMA _higherMa;

	private decimal? _higherMaLast;
	private decimal? _higherMaPrev;
	private decimal? _baseMaLast;
	private decimal? _baseMaPrev;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public DiffTfMaStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length on the higher timeframe", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Base Candle", "Trading timeframe", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Higher Candle", "Higher timeframe for confirmation", "General");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert the crossover logic", "General");

		Volume = 1m;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherCandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_higherMaLast = null;
		_higherMaPrev = null;
		_baseMaLast = null;
		_baseMaPrev = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (CandleType.Arg is not TimeSpan baseSpan || baseSpan <= TimeSpan.Zero)
			throw new InvalidOperationException("CandleType must contain a positive TimeSpan argument.");

		if (HigherCandleType.Arg is not TimeSpan higherSpan || higherSpan <= TimeSpan.Zero)
			throw new InvalidOperationException("HigherCandleType must contain a positive TimeSpan argument.");

		var ratio = higherSpan.TotalMinutes / baseSpan.TotalMinutes;
		var baseLength = Math.Max(1, (int)(MaPeriod * ratio));

		_baseMa = new SMA { Length = baseLength };
		_higherMa = new SMA { Length = MaPeriod };

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(_higherMa, ProcessHigher).Start();

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.Bind(_baseMa, ProcessBase).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigher(ICandleMessage candle, decimal higherMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_higherMa.IsFormed)
			return;

		// Store the last two higher timeframe MA values for crossover comparison.
		_higherMaPrev = _higherMaLast;
		_higherMaLast = higherMaValue;
	}

	private void ProcessBase(ICandleMessage candle, decimal baseMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_baseMa.IsFormed)
			return;

		// Track the last two base timeframe MA values.
		_baseMaPrev = _baseMaLast;
		_baseMaLast = baseMaValue;

		if (_higherMaPrev is not decimal higherPrev || _higherMaLast is not decimal higherLast)
			return;

		if (_baseMaPrev is not decimal basePrev || _baseMaLast is not decimal baseLast)
			return;

		var crossUp = higherPrev < basePrev && higherLast > baseLast;
		var crossDown = higherPrev > basePrev && higherLast < baseLast;

		if (ReverseSignals)
			(crossUp, crossDown) = (crossDown, crossUp);

		// Execute orders according to the detected crossover direction.
		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
