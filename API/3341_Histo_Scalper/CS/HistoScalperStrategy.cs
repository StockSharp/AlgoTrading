namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Multi-indicator scalping strategy combining MACD, RSI, and CCI filters.
/// Buys when all indicators agree on bullish signal. Sells on bearish agreement.
/// </summary>
public class HistoScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;

	private decimal _prevMacd;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public HistoScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var macd = new MovingAverageConvergenceDivergence();
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, rsi, cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal rsi, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevMacd = macdLine;
			_hasPrev = true;
			return;
		}

		// Buy: MACD crosses above zero, RSI < 70, CCI > -100
		if (_prevMacd <= 0 && macdLine > 0 && rsi < 70m && cci > -100m && Position <= 0)
			BuyMarket();
		// Sell: MACD crosses below zero, RSI > 30, CCI < 100
		else if (_prevMacd >= 0 && macdLine < 0 && rsi > 30m && cci < 100m && Position >= 0)
			SellMarket();

		_prevMacd = macdLine;
	}
}
