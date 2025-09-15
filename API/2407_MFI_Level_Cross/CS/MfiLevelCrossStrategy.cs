namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Money Flow Index based strategy that opens positions when the indicator crosses predefined levels.
/// The strategy can trade in the direction of the crossing or in the opposite direction based on Trend Mode.
/// </summary>
public class MfiLevelCrossStrategy : Strategy
{
	private enum TrendMode
	{
		Direct,
		Against
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _prevMfi;
	private bool _isFirst;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Period for the Money Flow Index indicator.
	/// </summary>
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }

	/// <summary>
	/// Oversold threshold for the MFI.
	/// </summary>
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

	/// <summary>
	/// Overbought threshold for the MFI.
	/// </summary>
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }

	/// <summary>
	/// Trading mode selection.
	/// </summary>
	public TrendMode Trend { get => _trend.Value; set => _trend.Value = value; }

	/// <summary>
	/// Stop loss in percent from entry price.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Take profit in percent from entry price.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MfiLevelCrossStrategy"/>.
	/// </summary>
	public MfiLevelCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of candles used", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Period of the Money Flow Index indicator", "Indicator");

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetRange(0m, 100m)
			.SetDisplay("Low Level", "Oversold threshold for MFI", "Signal");

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetRange(0m, 100m)
			.SetDisplay("High Level", "Overbought threshold for MFI", "Signal");

		_trend = Param(nameof(Trend), TrendMode.Direct)
			.SetDisplay("Trend Mode", "Trade with trend (Direct) or against it (Against)", "Signal");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetRange(0m, 100m)
			.SetDisplay("Stop Loss %", "Stop loss as percent from entry price", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetRange(0m, 100m)
			.SetDisplay("Take Profit %", "Take profit as percent from entry price", "Risk");
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

		_prevMfi = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var mfi = new MoneyFlowIndex { Length = MfiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(mfi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mfi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirst)
		{
			_prevMfi = mfiValue;
			_isFirst = false;
			return;
		}

		var crossBelowLow = _prevMfi > LowLevel && mfiValue <= LowLevel;
		var crossAboveHigh = _prevMfi < HighLevel && mfiValue >= HighLevel;

		if (Trend == TrendMode.Direct)
		{
			if (crossBelowLow && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossAboveHigh && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else
		{
			if (crossBelowLow && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (crossAboveHigh && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		_prevMfi = mfiValue;
	}
}
