using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI based strategy with overbought and oversold level cross signals.
/// Opens long when RSI crosses above oversold level, short when crosses below overbought level.
/// </summary>
public class RsiExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _levelUp;
	private readonly StrategyParam<decimal> _levelDown;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal LevelUp { get => _levelUp.Value; set => _levelUp.Value = value; }
	public decimal LevelDown { get => _levelDown.Value; set => _levelDown.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiExpertStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators");

		_levelUp = Param(nameof(LevelUp), 70m)
			.SetDisplay("RSI Overbought", "Upper RSI level triggering a short", "Indicators");

		_levelDown = Param(nameof(LevelDown), 30m)
			.SetDisplay("RSI Oversold", "Lower RSI level triggering a long", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

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
		_prevRsi = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

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

		if (_prevRsi == 0m)
		{
			_prevRsi = rsiValue;
			return;
		}

		var crossUp = _prevRsi < LevelDown && rsiValue > LevelDown;
		var crossDown = _prevRsi > LevelUp && rsiValue < LevelUp;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevRsi = rsiValue;
	}
}
