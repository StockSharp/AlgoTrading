using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R momentum strategy known as Vlado.
/// Buys when %R drops below the oversold level and sells when %R rises above the overbought level.
/// </summary>
public class VladoStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	private WilliamsR _williams;

	/// <summary>
	/// Number of candles used to calculate Williams %R.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold for %R (values are negative in the oscillator scale).
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold for %R (values are negative in the oscillator scale).
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
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
	/// Initializes <see cref="VladoStrategy"/>.
	/// </summary>
	public VladoStrategy()
	{
		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Number of candles used in %R calculation", "Williams %R")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), -25m)
			.SetDisplay("Overbought Level", "Threshold to consider %R overbought", "Williams %R")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), -75m)
			.SetDisplay("Oversold Level", "Threshold to consider %R oversold", "Williams %R")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williams = new WilliamsR
		{
			Length = WilliamsPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_williams, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);

			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _williams);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_williams?.IsFormed != true)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (wprValue <= OversoldLevel && Position <= 0)
		{
			// Oversold environment detected, buy or reverse to long position.
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: %R={wprValue:F2} <= {OversoldLevel}. Volume={volume}.");
		}
		else if (wprValue >= OverboughtLevel && Position >= 0)
		{
			// Overbought environment detected, sell or reverse to short position.
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: %R={wprValue:F2} >= {OverboughtLevel}. Volume={volume}.");
		}
	}
}
