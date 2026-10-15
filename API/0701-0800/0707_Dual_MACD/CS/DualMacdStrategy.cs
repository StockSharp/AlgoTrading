using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual MACD strategy.
/// MACD 1 is the fast MACD and MACD 2 the slow one; a histogram is the MACD line minus its signal line. When the slow histogram
/// crosses above zero while the fast histogram is positive the strategy goes long, and when it crosses below zero while the fast
/// histogram is negative it goes short, reversing an opposite position. A long closes when the fast histogram turns negative and
/// a short when it turns positive. Percent stop loss and take profit protect the position.
/// </summary>
public class DualMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _macd1FastLength;
	private readonly StrategyParam<int> _macd1SlowLength;
	private readonly StrategyParam<int> _macd1SignalLength;
	private readonly StrategyParam<int> _macd2FastLength;
	private readonly StrategyParam<int> _macd2SlowLength;
	private readonly StrategyParam<int> _macd2SignalLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevSlowHist;

	/// <summary>
	/// Fast period of the fast MACD.
	/// </summary>
	public int Macd1FastLength
	{
		get => _macd1FastLength.Value;
		set => _macd1FastLength.Value = value;
	}

	/// <summary>
	/// Slow period of the fast MACD.
	/// </summary>
	public int Macd1SlowLength
	{
		get => _macd1SlowLength.Value;
		set => _macd1SlowLength.Value = value;
	}

	/// <summary>
	/// Signal period of the fast MACD.
	/// </summary>
	public int Macd1SignalLength
	{
		get => _macd1SignalLength.Value;
		set => _macd1SignalLength.Value = value;
	}

	/// <summary>
	/// Fast period of the slow MACD.
	/// </summary>
	public int Macd2FastLength
	{
		get => _macd2FastLength.Value;
		set => _macd2FastLength.Value = value;
	}

	/// <summary>
	/// Slow period of the slow MACD.
	/// </summary>
	public int Macd2SlowLength
	{
		get => _macd2SlowLength.Value;
		set => _macd2SlowLength.Value = value;
	}

	/// <summary>
	/// Signal period of the slow MACD.
	/// </summary>
	public int Macd2SignalLength
	{
		get => _macd2SignalLength.Value;
		set => _macd2SignalLength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DualMacdStrategy()
	{
		_macd1FastLength = Param(nameof(Macd1FastLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Fast", "Fast period of the fast MACD", "MACD 1");

		_macd1SlowLength = Param(nameof(Macd1SlowLength), 144)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Slow", "Slow period of the fast MACD", "MACD 1");

		_macd1SignalLength = Param(nameof(Macd1SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD1 Signal", "Signal period of the fast MACD", "MACD 1");

		_macd2FastLength = Param(nameof(Macd2FastLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Fast", "Fast period of the slow MACD", "MACD 2");

		_macd2SlowLength = Param(nameof(Macd2SlowLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Slow", "Slow period of the slow MACD", "MACD 2");

		_macd2SignalLength = Param(nameof(Macd2SignalLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MACD2 Signal", "Signal period of the slow MACD", "MACD 2");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevSlowHist = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSlowHist = null;

		var fastMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Macd1FastLength },
				LongMa = { Length = Macd1SlowLength },
			},
			SignalMa = { Length = Macd1SignalLength }
		};

		var slowMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Macd2FastLength },
				LongMa = { Length = Macd2SlowLength },
			},
			SignalMa = { Length = Macd2SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMacd, slowMacd, ProcessCandle)
			.Start();

		var take = TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit();
		var stop = StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit();
		StartProtection(take, stop, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, fastMacd);
				DrawIndicator(oscillators, slowMacd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || fastValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal fastMacd, Signal: decimal fastSignal })
			return;

		if (!slowValue.IsFormed || slowValue is not MovingAverageConvergenceDivergenceSignalValue { Macd: decimal slowMacd, Signal: decimal slowSignal })
			return;

		var fastHist = fastMacd - fastSignal;
		var slowHist = slowMacd - slowSignal;

		var prevSlowHist = _prevSlowHist;
		_prevSlowHist = slowHist;

		if (prevSlowHist is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = prev <= 0 && slowHist > 0;
		var crossDown = prev >= 0 && slowHist < 0;

		if (crossUp && fastHist > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && fastHist < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && fastHist < 0)
			SellMarket(Position);
		else if (Position < 0 && fastHist > 0)
			BuyMarket(-Position);
	}
}
