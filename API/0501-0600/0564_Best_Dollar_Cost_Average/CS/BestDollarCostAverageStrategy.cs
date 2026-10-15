using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dollar Cost Average strategy — accumulates position at regular intervals,
/// sells on RSI overbought with forced sell after max accumulation period.
/// </summary>
public class BestDollarCostAverageStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _buyIntervalBars;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _maxAccumulationBars;

	private RelativeStrengthIndex _rsi;
	private int _barsSinceLastBuy;
	private int _totalBarsInPosition;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BuyIntervalBars { get => _buyIntervalBars.Value; set => _buyIntervalBars.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiSellLevel { get => _rsiSellLevel.Value; set => _rsiSellLevel.Value = value; }
	public int MaxAccumulationBars { get => _maxAccumulationBars.Value; set => _maxAccumulationBars.Value = value; }

	public BestDollarCostAverageStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_buyIntervalBars = Param(nameof(BuyIntervalBars), 350)
			.SetGreaterThanZero()
			.SetDisplay("Buy Interval", "Bars between DCA buys", "DCA");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period for sell signal", "Indicators");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 60m)
			.SetDisplay("RSI Sell Level", "RSI level to trigger sell", "Indicators");

		_maxAccumulationBars = Param(nameof(MaxAccumulationBars), 1200)
			.SetGreaterThanZero()
			.SetDisplay("Max Accumulation", "Max bars before forced sell", "DCA");
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
		_barsSinceLastBuy = 0;
		_totalBarsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		var rsiArea = CreateChartArea();
		if (rsiArea != null)
		{
			DrawIndicator(rsiArea, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
			return;

		_barsSinceLastBuy++;

		if (Position > 0)
			_totalBarsInPosition++;

		// Forced sell after max accumulation period
		if (Position > 0 && _totalBarsInPosition >= MaxAccumulationBars)
		{
			SellMarket();
			_totalBarsInPosition = 0;
			_barsSinceLastBuy = 0;
			return;
		}

		// Sell accumulated position when RSI is overbought
		if (Position > 0 && rsiValue >= RsiSellLevel && _totalBarsInPosition >= BuyIntervalBars)
		{
			SellMarket();
			_totalBarsInPosition = 0;
			_barsSinceLastBuy = 0;
			return;
		}

		// DCA buy at regular intervals
		if (_barsSinceLastBuy >= BuyIntervalBars)
		{
			BuyMarket();
			_barsSinceLastBuy = 0;
			if (_totalBarsInPosition == 0)
				_totalBarsInPosition = 1;
		}
	}
}
