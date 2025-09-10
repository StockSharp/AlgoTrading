using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands Long strategy - buys when price closes below the lower band and RSI is oversold.
/// </summary>
public class BollingerBandsLongStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _inTrade;

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
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
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Bollinger Bands Long strategy.
	/// </summary>
	public BollingerBandsLongStrategy()
	{
		_bbLength = Param(nameof(BbLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "Bollinger Bands");

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Bollinger Bands");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_inTrade = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bb = new BollingerBands
		{
			Length = BbLength,
			Width = BbDeviation
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bb, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.ClosePrice < lower && rsiValue < RsiOversold && Position <= 0)
		{
			BuyMarket();
			_inTrade = true;
			return;
		}

		if (!_inTrade)
			return;

		if (candle.ClosePrice >= middle && Position > 0)
		{
			SellMarket();
			_inTrade = false;
		}
	}
}
