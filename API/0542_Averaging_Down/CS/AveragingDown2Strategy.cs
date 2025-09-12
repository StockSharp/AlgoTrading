using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Averaging Down strategy based on RSI oversold levels.
/// </summary>
public class AveragingDown2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuyThreshold;

	private RelativeStrengthIndex _rsi;
	private decimal _previousHigh;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level that triggers long entries.
	/// </summary>
	public decimal RsiBuyThreshold
	{
		get => _rsiBuyThreshold.Value;
		set => _rsiBuyThreshold.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AveragingDown2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_rsiBuyThreshold = Param(nameof(RsiBuyThreshold), 33m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Buy Threshold", "Buy when RSI is below this level", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 50m, 5m);
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
		_previousHigh = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

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
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
		{
			_previousHigh = candle.HighPrice;
			return;
		}

		if (rsiValue < RsiBuyThreshold)
			BuyMarket(Volume);

		if (Position > 0 && candle.ClosePrice > _previousHigh)
			SellMarket(Math.Abs(Position));

		_previousHigh = candle.HighPrice;
	}
}

