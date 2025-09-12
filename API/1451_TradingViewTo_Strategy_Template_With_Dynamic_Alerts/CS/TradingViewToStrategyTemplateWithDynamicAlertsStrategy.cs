using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Template strategy reacting to RSI levels with stop loss and take profit.
/// </summary>
public class TradingViewToStrategyTemplateWithDynamicAlertsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _upperLevel;
	private readonly StrategyParam<int> _lowerLevel;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Upper RSI trigger level.
	/// </summary>
	public int UpperLevel { get => _upperLevel.Value; set => _upperLevel.Value = value; }

	/// <summary>
	/// Lower RSI trigger level.
	/// </summary>
	public int LowerLevel { get => _lowerLevel.Value; set => _lowerLevel.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TradingViewToStrategyTemplateWithDynamicAlertsStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI length", "RSI lookback", "Indicators");

		_upperLevel = Param(nameof(UpperLevel), 60)
			.SetDisplay("Upper level", "RSI overbought level", "Strategy");

		_lowerLevel = Param(nameof(LowerLevel), 40)
			.SetDisplay("Lower level", "RSI oversold level", "Strategy");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop loss %", "Percent stop loss", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take profit %", "Percent take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");
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

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position > 0)
		{
		if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
		SellMarket(Position);
		}
		else if (Position < 0)
		{
		if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
		BuyMarket(-Position);
		}

		if (rsiValue > UpperLevel && Position <= 0)
		{
		if (Position < 0)
		BuyMarket(Math.Abs(Position));
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice * (1 - StopLossPct / 100m);
		_takePrice = _entryPrice * (1 + TakeProfitPct / 100m);
		BuyMarket(Volume);
		}
		else if (rsiValue < LowerLevel && Position >= 0)
		{
		if (Position > 0)
		SellMarket(Position);
		_entryPrice = candle.ClosePrice;
		_stopPrice = _entryPrice * (1 + StopLossPct / 100m);
		_takePrice = _entryPrice * (1 - TakeProfitPct / 100m);
		SellMarket(Volume);
		}
	}
}
