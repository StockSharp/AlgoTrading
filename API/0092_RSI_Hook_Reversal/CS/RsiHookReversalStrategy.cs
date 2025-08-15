using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Hook Reversal Strategy.
/// Enters long when RSI forms an upward hook from oversold conditions.
/// Enters short when RSI forms a downward hook from overbought conditions.
/// </summary>
public class RsiHookReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<int> _exitLevel;
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevRsi;

	/// <summary>
	/// Period for RSI calculation.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level for RSI.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level for RSI.
	/// </summary>
	public int OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Exit level for RSI (neutral zone).
	/// </summary>
	public int ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
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
	/// Initializes a new instance of the <see cref="RsiHookReversalStrategy"/>.
	/// </summary>
	public RsiHookReversalStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
			.SetRange(7, 21)
			.SetCanOptimize(true);
			
		_oversoldLevel = Param(nameof(OversoldLevel), 30)
			.SetDisplay("Oversold Level", "Oversold level for RSI", "RSI Settings")
			.SetRange(20, 40)
			.SetCanOptimize(true);
			
		_overboughtLevel = Param(nameof(OverboughtLevel), 70)
			.SetDisplay("Overbought Level", "Overbought level for RSI", "RSI Settings")
			.SetRange(60, 80)
			.SetCanOptimize(true);
			
		_exitLevel = Param(nameof(ExitLevel), 50)
			.SetDisplay("Exit Level", "Exit level for RSI (neutral zone)", "RSI Settings")
			.SetRange(45, 55)
			.SetCanOptimize(true);
			
		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
			.SetRange(1m, 3m)
			.SetCanOptimize(true);
			
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

				_prevRsi = 0;
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

				// Create RSI indicator
				var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

	// Create subscription
	var subscription = SubscribeCandles(CandleType);
	
	// Bind indicator and process candles
	subscription
		.Bind(rsi, ProcessCandle)
		.Start();
		
	// Setup chart visualization
	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, rsi);
		DrawOwnTrades(area);
	}
}

	/// <summary>
	/// Process candle with RSI value.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <param name="rsiValue">RSI value.</param>
	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		// If this is the first calculation, just store the value
		if (_prevRsi == 0)
		{
			_prevRsi = rsiValue;
			return;
		}

		// Check for RSI hooks
		bool oversoldHookUp = _prevRsi < OversoldLevel && rsiValue > _prevRsi;
		bool overboughtHookDown = _prevRsi > OverboughtLevel && rsiValue < _prevRsi;
		
		// Long entry: RSI forms an upward hook from oversold
		if (oversoldHookUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Long entry: RSI upward hook from oversold ({_prevRsi} -> {rsiValue})");
		}
		// Short entry: RSI forms a downward hook from overbought
		else if (overboughtHookDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Short entry: RSI downward hook from overbought ({_prevRsi} -> {rsiValue})");
		}
		
		// Exit conditions based on RSI reaching neutral zone
		if (rsiValue > ExitLevel && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: RSI reached neutral zone ({rsiValue} > {ExitLevel})");
		}
		else if (rsiValue < ExitLevel && Position > 0)
		{
			SellMarket(Position);
			LogInfo($"Exit long: RSI reached neutral zone ({rsiValue} < {ExitLevel})");
		}
		
		// Update previous RSI value
		_prevRsi = rsiValue;
	}
}