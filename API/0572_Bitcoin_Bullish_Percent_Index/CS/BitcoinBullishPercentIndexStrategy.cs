using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin Bullish Percent Index strategy based on RSI indicator.
/// Buys when RSI crosses above oversold level and sells when RSI crosses below overbought level.
/// </summary>
public class BitcoinBullishPercentIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousRsi;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BitcoinBullishPercentIndexStrategy"/>.
	/// </summary>
	public BitcoinBullishPercentIndexStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Upper RSI threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Lower RSI threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_previousRsi = 50m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, OnProcess)
			.Start();
	}

	private void OnProcess(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousRsi < Oversold && rsiValue > Oversold && Position <= 0)
		{
			RegisterBuy();
		}
		else if (_previousRsi > Overbought && rsiValue < Overbought && Position >= 0)
		{
			RegisterSell();
		}

		_previousRsi = rsiValue;
	}
}
