using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Long Position strategy.
/// </summary>
public class RsiStrategyLongPositionDax2HoursDowJones1HourStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _hasPrev;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// Take profit level.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss level.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="RsiStrategyLongPositionDax2HoursDowJones1HourStrategy"/>.
	/// </summary>
	public RsiStrategyLongPositionDax2HoursDowJones1HourStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI", "General")
			.SetCanOptimize(true);

		_oversold = Param(nameof(Oversold), 35m)
			.SetDisplay("Oversold", "Level to enter", "General")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 55m)
			.SetDisplay("Take Profit", "Exit level", "General")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Cross-under exit level", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevRsi = default;
		_hasPrev = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var enterLong = _hasPrev && _prevRsi <= Oversold && rsi > Oversold;
		var exitLong = rsi > TakeProfit || (_hasPrev && _prevRsi >= StopLoss && rsi < StopLoss);

		if (enterLong && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (exitLong && Position > 0)
		{
			SellMarket(Position);
		}

		_prevRsi = rsi;
		_hasPrev = true;
	}
}
