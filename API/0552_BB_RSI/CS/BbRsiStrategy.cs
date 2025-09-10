using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands and RSI strategy with trailing exit.
/// </summary>
public class BbRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiExitLevel;
	private readonly StrategyParam<decimal> _trailingStep;

	private BollingerBands _bollingerBands;
	private RelativeStrengthIndex _rsi;

	private bool _inTrade;
	private decimal _peakPrice;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level to enter long.
	/// </summary>
	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	/// <summary>
	/// RSI level to exit long.
	/// </summary>
	public decimal RsiExitLevel
	{
		get => _rsiExitLevel.Value;
		set => _rsiExitLevel.Value = value;
	}

	/// <summary>
	/// Trailing step percentage.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BbRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bbPeriod = Param(nameof(BbPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Bollinger Bands")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Buy Level", "RSI threshold to enter long", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_rsiExitLevel = Param(nameof(RsiExitLevel), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Exit Level", "RSI threshold to exit long", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_trailingStep = Param(nameof(TrailingStep), 1m)
			.SetRange(0.1m, 20m)
			.SetDisplay("Trailing Step %", "Trailing stop step percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
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

		_inTrade = default;
		_peakPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollingerBands = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollingerBands, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollingerBands);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		if (!_inTrade && closePrice < lowerBand && rsiValue < RsiBuyLevel && Position <= 0)
		{
			RegisterBuy();
			_peakPrice = closePrice;
			_inTrade = true;
			return;
		}

		if (!_inTrade)
			return;

		if (closePrice > _peakPrice)
			_peakPrice = closePrice;

		var trailingDrop = _peakPrice * (1 - TrailingStep / 100m);

		if (closePrice <= trailingDrop || rsiValue > RsiExitLevel)
		{
			RegisterSell();
			_inTrade = false;
		}
	}
}
