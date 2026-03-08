using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on comparison of smoothed open and close prices.
/// Buys when the SMA of close crosses above the SMA of open.
/// Sells when the SMA of close crosses below the SMA of open.
/// </summary>
public class ColorXccxCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private ExponentialMovingAverage _openEma;
	private ExponentialMovingAverage _closeEma;
	private decimal _prevDiff;
	private bool _hasPrev;

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public ColorXccxCandleStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 5)
			.SetDisplay("SMA Length", "Length of the moving averages", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for the strategy", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss as percent of entry price", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit %", "Take profit as percent of entry price", "Risk Management");
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
		_openEma = default;
		_closeEma = default;
		_prevDiff = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openEma = new ExponentialMovingAverage { Length = SmaLength };
		_closeEma = new ExponentialMovingAverage { Length = SmaLength };
		_prevDiff = 0;
		_hasPrev = false;

		Indicators.Add(_openEma);
		Indicators.Add(_closeEma);

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openResult = _openEma.Process(candle.OpenPrice, candle.OpenTime, true);
		var closeResult = _closeEma.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!openResult.IsFormed || !closeResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var openVal = openResult.ToDecimal();
		var closeVal = closeResult.ToDecimal();
		var diff = closeVal - openVal;

		if (!_hasPrev)
		{
			_prevDiff = diff;
			_hasPrev = true;
			return;
		}

		if (_prevDiff <= 0 && diff > 0 && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevDiff >= 0 && diff < 0 && Position >= 0)
		{
			SellMarket();
		}

		_prevDiff = diff;
	}
}
