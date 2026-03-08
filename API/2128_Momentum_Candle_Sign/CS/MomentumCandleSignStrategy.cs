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
/// Strategy based on crossover between momentum calculated on candle open and close prices.
/// A long position is opened when open momentum crosses below close momentum, and a short position on the opposite cross.
/// </summary>
public class MomentumCandleSignStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Momentum _openMomentum;
	private Momentum _closeMomentum;

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
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame of candles", "General");
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
		_openMomentum = default;
		_closeMomentum = default;
		_prevOpenMomentum = 0;
		_prevCloseMomentum = 0;
		_isFormed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openMomentum = new Momentum { Length = MomentumPeriod };
		_closeMomentum = new Momentum { Length = MomentumPeriod };
		_isFormed = false;

		Indicators.Add(_openMomentum);
		Indicators.Add(_closeMomentum);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(null, null);

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

		var openMom = _openMomentum.Process(new DecimalIndicatorValue(_openMomentum, candle.OpenPrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var closeMom = _closeMomentum.Process(new DecimalIndicatorValue(_closeMomentum, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

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
			BuyMarket();
		else if (sellSignal && Position >= 0)
			SellMarket();

		_prevOpenMomentum = openMom;
		_prevCloseMomentum = closeMom;
	}
}