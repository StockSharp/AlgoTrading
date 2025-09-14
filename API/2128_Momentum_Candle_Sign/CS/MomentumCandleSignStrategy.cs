using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossover between momentum calculated on candle open and close prices.
/// A long position is opened when open momentum crosses below close momentum, and a short position on the opposite cross.
/// </summary>
public class MomentumCandleSignStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Momentum _openMomentum;
	private readonly Momentum _closeMomentum;

	private decimal _prevOpenMomentum;
	private decimal _prevCloseMomentum;
	private bool _isFormed;

	/// <summary>
	/// Period of the momentum indicators.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MomentumCandleSignStrategy"/>.
	/// </summary>
	public MomentumCandleSignStrategy()
	{
		_momentumPeriod = Param(nameof(MomentumPeriod), 12)
			.SetDisplay("Momentum Period", "Indicator period", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of candles", "General");

		_openMomentum = new Momentum { Length = MomentumPeriod };
		_closeMomentum = new Momentum { Length = MomentumPeriod };
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

		_openMomentum.Length = MomentumPeriod;
		_closeMomentum.Length = MomentumPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _openMomentum);
			DrawIndicator(area, _closeMomentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openMom = _openMomentum.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		var closeMom = _closeMomentum.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		if (!_isFormed)
		{
			_prevOpenMomentum = openMom;
			_prevCloseMomentum = closeMom;
			_isFormed = true;
			return;
		}

		var buySignal = _prevOpenMomentum >= _prevCloseMomentum && openMom < closeMom;
		var sellSignal = _prevOpenMomentum <= _prevCloseMomentum && openMom > closeMom;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sellSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevOpenMomentum = openMom;
		_prevCloseMomentum = closeMom;
	}
}
