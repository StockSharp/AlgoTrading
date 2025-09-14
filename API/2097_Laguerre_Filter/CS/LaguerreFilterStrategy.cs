using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Laguerre filter and FIR crossover.
/// </summary>
public class LaguerreFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFir;
	private decimal? _prevLaguerre;

	/// <summary>
	/// Laguerre filter gamma parameter.
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes a new instance of the <see cref="LaguerreFilterStrategy"/>.
	/// </summary>
	public LaguerreFilterStrategy()
	{
		_gamma = Param(nameof(Gamma), 0.7m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Gamma", "Laguerre filter smoothing factor", "Indicators")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevFir = _prevLaguerre = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var laguerre = new LaguerreFilter { Gamma = Gamma };
		var fir = new WeightedMovingAverage { Length = 4 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(laguerre, fir, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, laguerre);
			DrawIndicator(area, fir);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal laguerreValue, decimal firValue)
	{
		// Ignore unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure trading is allowed and data is formed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Initialize previous values
		if (_prevFir is null || _prevLaguerre is null)
		{
			_prevFir = firValue;
			_prevLaguerre = laguerreValue;
			return;
		}

		// Determine relation between lines on previous bar
		var firWasAbove = _prevFir > _prevLaguerre;
		var firIsAbove = firValue > laguerreValue;

		// Close opposite positions when relation flips
		if (firWasAbove && Position < 0)
			BuyMarket(Math.Abs(Position));
		else if (!firWasAbove && Position > 0)
			SellMarket(Math.Abs(Position));

		var volume = Volume + Math.Abs(Position);

		// Entry signals based on crossover
		if (firWasAbove && !firIsAbove && Position <= 0)
		{
			// FIR crossed below Laguerre - go long
			BuyMarket(volume);
			LogInfo("FIR crossed below Laguerre. Entering long.");
		}
		else if (!firWasAbove && firIsAbove && Position >= 0)
		{
			// FIR crossed above Laguerre - go short
			SellMarket(volume);
			LogInfo("FIR crossed above Laguerre. Entering short.");
		}

		_prevFir = firValue;
		_prevLaguerre = laguerreValue;
	}
}
