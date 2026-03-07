using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using the slope of a linear regression with a shifted trigger line.
/// </summary>
public class LinearRegressionSlopeV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _triggerShift;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;

	private decimal[] _slopeHistory = Array.Empty<decimal>();
	private int _filled;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public int TriggerShift { get => _triggerShift.Value; set => _triggerShift.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }

	public LinearRegressionSlopeV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bars for regression", "Parameters");

		_triggerShift = Param(nameof(TriggerShift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Shift", "Lag for trigger line", "Parameters");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
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
		_slopeHistory = Array.Empty<decimal>();
		_filled = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_slopeHistory = new decimal[TriggerShift + 3];
		_filled = 0;

		var slope = new LinearReg { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(slope, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slope);
			DrawOwnTrades(area);
		}
	}

	private void Shift(decimal value)
	{
		for (var i = 0; i < _slopeHistory.Length - 1; i++)
			_slopeHistory[i] = _slopeHistory[i + 1];

		_slopeHistory[^1] = value;

		if (_filled < _slopeHistory.Length)
			_filled++;
	}

	private void ProcessCandle(ICandleMessage candle, decimal slopeVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		Shift(slopeVal);

		if (_filled < _slopeHistory.Length)
			return;

		var s2 = _slopeHistory[_slopeHistory.Length - 3];
		var s1 = _slopeHistory[_slopeHistory.Length - 2];
		var t2 = _slopeHistory[0];
		var t1 = _slopeHistory[1];

		if (s2 > t2)
		{
			if (Position < 0)
				BuyMarket();

			if (s1 <= t1 && Position <= 0)
				BuyMarket();
		}
		else if (t2 > s2)
		{
			if (Position > 0)
				SellMarket();

			if (t1 <= s1 && Position >= 0)
				SellMarket();
		}
	}
}
