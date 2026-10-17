using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MFI level-crossing reversal with stop loss/take profit distances in instrument price steps.
/// A crossover signal is acted on only after a completed, formed indicator bar.
/// </summary>
public class MfiHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<Unit> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMfi;
	private Order _pendingEntry;

	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	/// <summary>Number of price steps to the protective stop.</summary>
	public Unit StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>Number of price steps to the profit target.</summary>
	public Unit TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MfiHistogramStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("MFI Period", "Period for Money Flow Index", "MFI");
		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Upper MFI crossing threshold", "MFI");
		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetDisplay("Low Level", "Lower MFI crossing threshold", "MFI");
		// Preserve the existing public Unit parameter API; its numeric value denotes PRICE STEPS.
		_stopLoss = Param(nameof(StopLoss), new Unit(1000m, UnitTypes.Absolute))
			.SetDisplay("Stop Loss", "Distance measured in instrument price steps", "Risk");
		_takeProfit = Param(nameof(TakeProfit), new Unit(2000m, UnitTypes.Absolute))
			.SetDisplay("Take Profit", "Distance measured in instrument price steps", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMfi = null;
		_pendingEntry = null;
	}

	private Unit ConvertTicksToPrice(Unit ticks, decimal step, string name)
	{
		if (ticks is null || ticks.Type != UnitTypes.Absolute || ticks.Value <= 0)
			throw new InvalidOperationException($"{name} must be a positive number of price steps (absolute numeric unit).");
		return new Unit(ticks.Value * step, UnitTypes.Absolute);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMfi = null;
		_pendingEntry = null;
		var step = Security?.PriceStep;
		if (step is null || step <= 0m)
			throw new InvalidOperationException("MFI Histogram requires a positive Security.PriceStep for tick-based protection.");

		StartProtection(
			takeProfit: ConvertTicksToPrice(TakeProfit, step.Value, nameof(TakeProfit)),
			stopLoss: ConvertTicksToPrice(StopLoss, step.Value, nameof(StopLoss)),
			useMarketOrders: true, isLocalStop: true);

		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mfi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			var indicatorArea = CreateChartArea();
			if (indicatorArea is not null)
				DrawIndicator(indicatorArea, mfi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevMfi is decimal previous && IsFormedAndOnlineAndAllowTrading()
			&& (_pendingEntry is null || _pendingEntry.State is OrderStates.Done or OrderStates.Failed))
		{
			if (previous <= HighLevel && mfiValue > HighLevel && Position <= 0m)
				_pendingEntry = BuyMarket(Volume + Math.Abs(Position));
			else if (previous >= LowLevel && mfiValue < LowLevel && Position >= 0m)
				_pendingEntry = SellMarket(Volume + Math.Abs(Position));
		}

		// Always advance the baseline, even while trading is disabled or an order is pending.
		_prevMfi = mfiValue;
	}
}
