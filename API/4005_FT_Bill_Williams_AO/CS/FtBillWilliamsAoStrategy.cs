using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Awesome Oscillator strategy.
/// Buys when AO crosses above zero and sells when AO crosses below zero,
/// filtered by Alligator teeth alignment.
/// </summary>
public class FtBillWilliamsAoStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _lipsPeriod;

	private decimal _prevAo;
	private decimal _prevTeeth;
	private bool _isReady;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	public FtBillWilliamsAoStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetDisplay("Jaw Period", "Alligator jaw SMA period", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetDisplay("Teeth Period", "Alligator teeth SMA period", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetDisplay("Lips Period", "Alligator lips SMA period", "Alligator");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAo = 0;
		_prevTeeth = 0;
		_isReady = false;

		var ao = new AwesomeOscillator();
		var teeth = new SMA { Length = TeethPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ao, teeth, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ao);
			DrawIndicator(area, teeth);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal aoVal, decimal teethVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevAo = aoVal;
			_prevTeeth = teethVal;
			_isReady = true;
			return;
		}

		var close = candle.ClosePrice;

		// Buy signal: AO crosses above zero, price above teeth
		if (_prevAo <= 0 && aoVal > 0 && close > teethVal)
		{
			if (Position < 0)
				BuyMarket(); // close short

			if (Position <= 0)
				BuyMarket();
		}
		// Sell signal: AO crosses below zero, price below teeth
		else if (_prevAo >= 0 && aoVal < 0 && close < teethVal)
		{
			if (Position > 0)
				SellMarket(); // close long

			if (Position >= 0)
				SellMarket();
		}

		_prevAo = aoVal;
		_prevTeeth = teethVal;
	}
}
