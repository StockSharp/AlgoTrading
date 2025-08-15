using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR Exhaustion Strategy.
/// Enters long when ATR rises significantly and a bullish candle forms.
/// Enters short when ATR rises significantly and a bearish candle forms.
/// </summary>
public class AtrExhaustionStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrAvgPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private SimpleMovingAverage _atrAvg;

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR average calculation.
	/// </summary>
	public int AtrAvgPeriod
	{
		get => _atrAvgPeriod.Value;
		set => _atrAvgPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier to determine ATR spike.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Period for moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public Unit StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AtrExhaustionStrategy"/>.
	/// </summary>
	public AtrExhaustionStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetRange(7, 21)
			.SetCanOptimize(true);
			
		_atrAvgPeriod = Param(nameof(AtrAvgPeriod), 20)
			.SetDisplay("ATR Average Period", "Period for ATR average calculation", "Indicators")
			.SetRange(10, 30)
			.SetCanOptimize(true);
			
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "Multiplier to determine ATR spike", "Indicators")
			.SetRange(1.3m, 2.0m)
			.SetCanOptimize(true);
			
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Period for moving average", "Indicators")
			.SetRange(10, 50)
			.SetCanOptimize(true);
			
		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
			.SetRange(1m, 3m)
			.SetCanOptimize(true);
			
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		// Enable position protection using stop-loss
		StartProtection(
			takeProfit: null,
			stopLoss: StopLoss,
			isStopTrailing: false,
			useMarketOrders: true
		);

		_atrAvg = new SimpleMovingAverage { Length = AtrAvgPeriod };

		// Create indicators
		var ma = new SimpleMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Create subscription
		var subscription = SubscribeCandles(CandleType);
		
		// Bind indicators to candles
		subscription
			.Bind(ma, atr, ProcessCandle)
			.Start();
			
		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process candle with indicator values.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <param name="maValue">Moving average value.</param>
	/// <param name="atrValue">ATR value.</param>
	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Update ATR average
		var atrAvgValue = _atrAvg.Process(atrValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

		// Determine candle direction
		bool isBullishCandle = candle.ClosePrice > candle.OpenPrice;
		bool isBearishCandle = candle.ClosePrice < candle.OpenPrice;

		// Check for ATR spike
		bool isAtrSpike = atrValue > atrAvgValue * AtrMultiplier;

		if (!isAtrSpike)
			return;

		// Long entry: ATR spike with bullish candle
		if (isAtrSpike && isBullishCandle && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long entry: ATR spike ({atrValue} > {atrAvgValue * AtrMultiplier}) with bullish candle");
		}
		// Short entry: ATR spike with bearish candle
		else if (isAtrSpike && isBearishCandle && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Short entry: ATR spike ({atrValue} > {atrAvgValue * AtrMultiplier}) with bearish candle");
		}
	}
}