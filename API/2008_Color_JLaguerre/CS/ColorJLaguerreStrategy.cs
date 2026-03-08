using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on color-coded Laguerre oscillator approximated by RSI.
/// </summary>
public class ColorJLaguerreStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal MiddleLevel { get => _middleLevel.Value; set => _middleLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorJLaguerreStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Period for RSI", "Indicators");

		_highLevel = Param(nameof(HighLevel), 85m)
			.SetDisplay("High Level", "Upper threshold", "Levels");

		_middleLevel = Param(nameof(MiddleLevel), 50m)
			.SetDisplay("Middle Level", "Central threshold", "Levels");

		_lowLevel = Param(nameof(LowLevel), 15m)
			.SetDisplay("Low Level", "Lower threshold", "Levels");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		SubscribeCandles(CandleType)
			.Bind(rsi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(4, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevRsi = rsiValue;
			_hasPrev = true;
			return;
		}

		// Open long when RSI crosses above middle level
		if (_prevRsi <= MiddleLevel && rsiValue > MiddleLevel && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Open short when RSI crosses below middle level
		else if (_prevRsi >= MiddleLevel && rsiValue < MiddleLevel && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		// Exit long at high level
		if (Position > 0 && rsiValue >= HighLevel)
		{
			SellMarket();
		}
		// Exit short at low level
		else if (Position < 0 && rsiValue <= LowLevel)
		{
			BuyMarket();
		}

		_prevRsi = rsiValue;
	}
}
