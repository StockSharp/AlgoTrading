using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
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

	private decimal? _prevDiff;

	/// <summary>
	/// Length of the simple moving averages.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ColorXccxCandleStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length of the moving averages", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for the strategy", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percent of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit as percent of entry price", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 1m);
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

		_prevDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var openSma = new SimpleMovingAverage { Length = SmaLength, CandlePrice = CandlePrice.Open };
		var closeSma = new SimpleMovingAverage { Length = SmaLength, CandlePrice = CandlePrice.Close };

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(openSma, closeSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, openSma);
			DrawIndicator(area, closeSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal openSma, decimal closeSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = closeSma - openSma;

		if (_prevDiff is decimal prev)
		{
			if (prev <= 0 && diff > 0 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (prev >= 0 && diff < 0 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevDiff = diff;
	}
}
