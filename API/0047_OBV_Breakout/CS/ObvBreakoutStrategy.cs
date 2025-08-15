using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// On-Balance Volume (OBV) Breakout strategy
/// Long entry: OBV breaks above its highest level over N periods
/// Short entry: OBV breaks below its lowest level over N periods
/// Exit when OBV crosses below/above its moving average
/// </summary>
public class OBVBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _obvMAPeriod;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _highestOBV;
	private decimal? _lowestOBV;
	private bool _isFirstCandle;

	/// <summary>
	/// Lookback Period for OBV highest/lowest
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// OBV MA Period
	/// </summary>
	public int OBVMAPeriod
	{
		get => _obvMAPeriod.Value;
		set => _obvMAPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="OBVBreakoutStrategy"/>.
	/// </summary>
	public OBVBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for calculating OBV highest/lowest levels", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 10);

		_obvMAPeriod = Param(nameof(OBVMAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("OBV MA Period", "Period for OBV Moving Average calculation", "Strategy Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
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
		_highestOBV = null;
		_lowestOBV = null;
		_isFirstCandle = true;

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var obv = new OnBalanceVolume();
		
		// Create a custom moving average for OBV
		var obvMA = new SimpleMovingAverage { Length = OBVMAPeriod };
		
		// Create highest and lowest indicators for OBV values
		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		
		// We need to process OBV first, then calculate MA and highest/lowest from it
		subscription
			.BindEx(obv, (candle, obvValue) => {
				// Skip unfinished candles
				if (candle.State != CandleStates.Finished)
					return;
				
				// Process the OBV value through other indicators
				var obvMAValue = obvMA.Process(obvValue).ToDecimal();
				var obvVal = obvValue.ToDecimal();
				
				if (!_isFirstCandle)
				{
					var highestValue = highest.IsFormed && _highestOBV.HasValue ? highest.Process(obvValue).ToDecimal() : (_highestOBV.HasValue ? Math.Max(_highestOBV.Value, obvVal) : obvVal);
					var lowestValue = lowest.IsFormed && _lowestOBV.HasValue ? lowest.Process(obvValue).ToDecimal() : (_lowestOBV.HasValue ? Math.Min(_lowestOBV.Value, obvVal) : obvVal);
					
					ProcessCandle(candle, obvVal, obvMAValue, highestValue, lowestValue);
					
					if (!highest.IsFormed)
						_highestOBV = highestValue;
					if (!lowest.IsFormed)
						_lowestOBV = lowestValue;
				}
				else
				{
					_highestOBV = obvVal;
					_lowestOBV = obvVal;
					_isFirstCandle = false;
				}
			})
			.Start();

		// Configure protection
		StartProtection(
			takeProfit: new Unit(3, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent)
		);

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, obv);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal obvValue, decimal obvMAValue, decimal highestValue, decimal lowestValue)
	{
		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		// Log current values
		LogInfo($"Candle Close: {candle.ClosePrice}, OBV: {obvValue}, OBV MA: {obvMAValue}");
		LogInfo($"Highest OBV: {highestValue}, Lowest OBV: {lowestValue}");

		// Trading logic:
		// Long: OBV breaks above highest level
		if (_highestOBV.HasValue && obvValue > highestValue && obvValue > _highestOBV.Value && Position <= 0)
		{
			LogInfo($"Buy Signal: OBV ({obvValue}) breaking above highest level ({highestValue})");
			BuyMarket(Volume + Math.Abs(Position));
		}
		// Short: OBV breaks below lowest level
		else if (_lowestOBV.HasValue && obvValue < lowestValue && obvValue < _lowestOBV.Value && Position >= 0)
		{
			LogInfo($"Sell Signal: OBV ({obvValue}) breaking below lowest level ({lowestValue})");
			SellMarket(Volume + Math.Abs(Position));
		}
		
		// Exit logic: OBV crosses below/above its moving average
		if (Position > 0 && obvValue < obvMAValue)
		{
			LogInfo($"Exit Long: OBV ({obvValue}) < OBV MA ({obvMAValue})");
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && obvValue > obvMAValue)
		{
			LogInfo($"Exit Short: OBV ({obvValue}) > OBV MA ({obvMAValue})");
			BuyMarket(Math.Abs(Position));
		}
	}
}