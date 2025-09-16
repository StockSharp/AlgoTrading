using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Polarized Fractal Efficiency strategy.
/// Generates trades based on trend reversals in the PFE indicator.
/// </summary>
public class PolarizedFractalEfficiencyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _pfePeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private decimal?[] _pfeBuffer = Array.Empty<decimal?>();

	/// <summary>
	/// Candle series type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// PFE calculation period.
	/// </summary>
	public int PfePeriod { get => _pfePeriod.Value; set => _pfePeriod.Value = value; }

	/// <summary>
	/// Shift of the bar used for signal detection.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Initialize <see cref="PolarizedFractalEfficiencyStrategy"/>.
	/// </summary>
	public PolarizedFractalEfficiencyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_pfePeriod = Param(nameof(PfePeriod), 5)
			.SetDisplay("PFE Period", "Indicator calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Offset for signal", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetDisplay("Take Profit", "Take profit in ticks", "Protection");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Protection");
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
		_pfeBuffer = new decimal?[SignalBar + 3];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var pfe = new PolarizedFractalEfficiency { Length = PfePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(pfe, ProcessCandle).Start();

		if (TakeProfit > 0 || StopLoss > 0)
			StartProtection(new Unit(TakeProfit, UnitTypes.Step), new Unit(StopLoss, UnitTypes.Step));
		else
			StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, pfe);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal pfeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = _pfeBuffer.Length - 1; i > 0; i--)
			_pfeBuffer[i] = _pfeBuffer[i - 1];
		_pfeBuffer[0] = pfeValue;

		var sb = SignalBar;
		if (_pfeBuffer[sb] is not decimal current ||
			_pfeBuffer[sb + 1] is not decimal prev1 ||
			_pfeBuffer[sb + 2] is not decimal prev2)
			return;

		if (prev1 < prev2)
		{
			if (Position < 0)
				BuyMarket();

			if (current > prev1 && Position <= 0)
				BuyMarket();
		}
		else if (prev1 > prev2)
		{
			if (Position > 0)
				SellMarket();

			if (current < prev1 && Position >= 0)
				SellMarket();
		}
	}
}

