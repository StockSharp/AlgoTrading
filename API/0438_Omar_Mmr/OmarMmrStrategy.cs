namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Omar MMR Strategy
/// </summary>
public class OmarMmrStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _emaA;
	private ExponentialMovingAverage _emaB;
	private ExponentialMovingAverage _emaC;
	private MovingAverageConvergenceDivergence _macd;

	public OmarMmrStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// RSI
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI");

		// Moving Averages
		_emaALength = Param(nameof(EmaALength), 20)
			.SetDisplay("EMA A Length", "First EMA period", "Moving Averages");

		_emaBLength = Param(nameof(EmaBLength), 50)
			.SetDisplay("EMA B Length", "Second EMA period", "Moving Averages");

		_emaCLength = Param(nameof(EmaCLength), 200)
			.SetDisplay("EMA C Length", "Third EMA period", "Moving Averages");

		// MACD
		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast Length", "Fast MA period", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow Length", "Slow MA period", "MACD");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal Length", "Signal period", "MACD");

		// Strategy
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Strategy");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int EmaALength
	{
		get => _emaALength.Value;
		set => _emaALength.Value = value;
	}

	public int EmaBLength
	{
		get => _emaBLength.Value;
		set => _emaBLength.Value = value;
	}

	public int EmaCLength
	{
		get => _emaCLength.Value;
		set => _emaCLength.Value = value;
	}

	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_emaA = new ExponentialMovingAverage { Length = EmaALength };
		_emaB = new ExponentialMovingAverage { Length = EmaBLength };
		_emaC = new ExponentialMovingAverage { Length = EmaCLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _emaA, _emaB, _emaC, _macd, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaA, System.Drawing.Color.Orange);
			DrawIndicator(area, _emaB, System.Drawing.Color.Purple);
			DrawIndicator(area, _emaC, System.Drawing.Color.Green);
			DrawOwnTrades(area);
		}

		// Enable protection
		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void OnProcess(ICandleMessage candle, decimal rsiValue, decimal emaA, decimal emaB, decimal emaC, IIndicatorValue macdValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_rsi.IsFormed || !_emaA.IsFormed || !_emaB.IsFormed || !_emaC.IsFormed || !_macd.IsFormed)
			return;

		// Get MACD values
		var macdData = macdValue.GetValue<MacdValue>();
		var macdLine = macdData.Macd;
		var signalLine = macdData.Signal;

		// Get previous MACD values for crossover detection
		var prevMacdValue = _macd.GetValue<MacdValue>(1);
		var prevMacdLine = prevMacdValue.Macd;
		var prevSignalLine = prevMacdValue.Signal;

		// Check MACD crossover
		var macdCrossover = macdLine > signalLine && prevMacdLine <= prevSignalLine;

		// Entry condition
		var longEntry = candle.ClosePrice > emaC && 
						emaA > emaB && 
						macdCrossover && 
						rsiValue > 29 && 
						rsiValue < 70;

		// Execute trades
		if (longEntry && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
	}
}