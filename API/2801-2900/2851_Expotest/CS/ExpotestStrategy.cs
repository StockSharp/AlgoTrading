namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Parabolic SAR based strategy converted from the Expotest MQL expert advisor.
/// Enters long when SAR is below price, short when SAR is above price.
/// </summary>
public class ExpotestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;

	private bool _prevSarBelow;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	public ExpotestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for signal generation", "General");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators");

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSarBelow = false;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSarBelow = false;
		_initialized = false;

		var sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sar, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sarBelow = sarValue < candle.ClosePrice;

		if (!_initialized)
		{
			_prevSarBelow = sarBelow;
			_initialized = true;
			return;
		}

		// SAR flipped from above to below price => Buy signal
		if (sarBelow && !_prevSarBelow && Position <= 0)
		{
			BuyMarket();
		}
		// SAR flipped from below to above price => Sell signal
		else if (!sarBelow && _prevSarBelow && Position >= 0)
		{
			SellMarket();
		}

		_prevSarBelow = sarBelow;
	}
}
