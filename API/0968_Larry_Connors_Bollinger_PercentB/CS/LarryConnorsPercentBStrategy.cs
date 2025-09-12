using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Connors %B strategy using Bollinger Bands.
/// Buys when price is above SMA200 and %B stays below a threshold for three consecutive candles.
/// Exits when %B rises above the upper threshold.
/// </summary>
public class LarryConnorsPercentBStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _lowPercentB;
	private readonly StrategyParam<decimal> _highPercentB;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevPercentB1;
	private decimal? _prevPercentB2;

	/// <summary>
	/// SMA period for trend filter (default: 200).
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Period for Bollinger Bands (default: 20).
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation for Bollinger Bands (default: 2.0).
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Lower threshold for %B (default: 0.2).
	/// </summary>
	public decimal LowPercentB
	{
		get => _lowPercentB.Value;
		set => _lowPercentB.Value = value;
	}

	/// <summary>
	/// Upper threshold for %B to exit (default: 0.8).
	/// </summary>
	public decimal HighPercentB
	{
		get => _highPercentB.Value;
		set => _highPercentB.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price (default: 2%).
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy.
	/// </summary>
	public LarryConnorsPercentBStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Period for long-term trend filter", "General")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation for Bollinger Bands", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_lowPercentB = Param(nameof(LowPercentB), 0.2m)
			.SetDisplay("Low %B", "Lower threshold for %B", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.3m, 0.05m);

		_highPercentB = Param(nameof(HighPercentB), 0.8m)
			.SetDisplay("High %B", "Upper threshold for %B to exit", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.7m, 0.9m, 0.05m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 5.0m, 1.0m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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

		_prevPercentB1 = null;
		_prevPercentB2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = SmaPeriod };
		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(0),
			new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (upper == lower)
			return;

		var percentB = (candle.ClosePrice - lower) / (upper - lower);

		if (_prevPercentB1 is not decimal prev1 || _prevPercentB2 is not decimal prev2)
		{
			_prevPercentB2 = _prevPercentB1;
			_prevPercentB1 = percentB;
			return;
		}

		var condition1 = candle.ClosePrice > smaValue;
		var condition2 = prev2 < LowPercentB && prev1 < LowPercentB && percentB < LowPercentB;

		if (Position <= 0 && condition1 && condition2)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && percentB > HighPercentB)
		{
			SellMarket(Position);
		}

		_prevPercentB2 = _prevPercentB1;
		_prevPercentB1 = percentB;
	}
}
