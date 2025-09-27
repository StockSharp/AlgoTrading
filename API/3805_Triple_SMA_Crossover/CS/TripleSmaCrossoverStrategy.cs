using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple SMA crossover strategy converted from the MQL expert `3sma.mq4`.
/// </summary>
public class TripleSmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _mediumSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _smaSpreadSteps;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal _priceStep;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast SMA length (MQL parameter SMA1).
	/// </summary>
	public int FastSmaLength
	{
		get => _fastSmaLength.Value;
		set => _fastSmaLength.Value = value;
	}

	/// <summary>
	/// Medium SMA length (MQL parameter SMA2).
	/// </summary>
	public int MediumSmaLength
	{
		get => _mediumSmaLength.Value;
		set => _mediumSmaLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length (MQL parameter SMA3).
	/// </summary>
	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	/// <summary>
	/// Minimum spread between SMAs expressed in price steps.
	/// </summary>
	public int SmaSpreadSteps
	{
		get => _smaSpreadSteps.Value;
		set => _smaSpreadSteps.Value = value;
	}

	/// <summary>
	/// Volume for new orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TripleSmaCrossoverStrategy"/>.
	/// </summary>
	public TripleSmaCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for SMA calculations", "General");

		_fastSmaLength = Param(nameof(FastSmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Fast SMA period (SMA1)", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_mediumSmaLength = Param(nameof(MediumSmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Medium SMA Length", "Medium SMA period (SMA2)", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_slowSmaLength = Param(nameof(SlowSmaLength), 29)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Slow SMA period (SMA3)", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_smaSpreadSteps = Param(nameof(SmaSpreadSteps), 0)
			.SetDisplay("SMA Spread Steps", "Required SMA separation in price steps", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);
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
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;

		StartProtection();

		var fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		var mediumSma = new SimpleMovingAverage { Length = MediumSmaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, mediumSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, mediumSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal medium, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var spread = _priceStep > 0m ? SmaSpreadSteps * _priceStep : 0m;

		if (Position > 0 && fast < medium)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && fast > medium)
		{
			BuyMarket(-Position);
		}

		if (Position <= 0 && fast > medium + spread && medium > slow + spread)
		{
			BuyMarket(TradeVolume);
		}
		else if (Position >= 0 && fast < medium - spread && medium < slow - spread)
		{
			SellMarket(TradeVolume);
		}
	}
}

