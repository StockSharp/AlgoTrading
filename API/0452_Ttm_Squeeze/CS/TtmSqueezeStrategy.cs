using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TTM Squeeze Strategy - uses TTM Squeeze momentum with RSI filter
/// </summary>
public class TtmSqueezeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _squeezeLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useTP;
	private readonly StrategyParam<decimal> _tpPercent;

	private BollingerBands _bollingerBands;
	private KeltnerChannels _keltnerChannels;
	private Highest _highest;
	private Lowest _lowest;
	private SimpleMovingAverage _closeSma;
	private LinearRegression _momentum;
	private RelativeStrengthIndex _rsi;

	private decimal _previousMomentum;
	private decimal _currentMomentum;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// TTM Squeeze calculation length.
	/// </summary>
	public int SqueezeLength
	{
		get => _squeezeLength.Value;
		set => _squeezeLength.Value = value;
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
	/// Use take profit.
	/// </summary>
	public bool UseTP
	{
		get => _useTP.Value;
		set => _useTP.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TtmSqueezeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_squeezeLength = Param(nameof(SqueezeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Squeeze Length", "TTM Squeeze calculation length", "TTM Squeeze")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 2);

		_useTP = Param(nameof(UseTP), false)
			.SetDisplay("Enable Take Profit", "Use take profit", "Take Profit");

		_tpPercent = Param(nameof(TpPercent), 1.2m)
			.SetRange(0.1m, 10.0m)
			.SetDisplay("TP Percent", "Take profit percentage", "Take Profit")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3.0m, 0.3m);
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

		_previousMomentum = default;
		_currentMomentum = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_bollingerBands = new BollingerBands { Length = SqueezeLength, Width = 2.0m };
		_keltnerChannels = new KeltnerChannels { Length = SqueezeLength, Multiplier = 1.5m };
		_highest = new Highest { Length = SqueezeLength };
		_lowest = new Lowest { Length = SqueezeLength };
		_closeSma = new SimpleMovingAverage { Length = SqueezeLength };
		_momentum = new LinearRegression { Length = SqueezeLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([_rsi, _highest, _lowest, _closeSma, _momentum], ProcessCandle)
			.Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		// Setup protection if take profit is enabled
		if (UseTP)
		{
			StartProtection(new Unit(TpPercent / 100m, UnitTypes.Percent), new Unit());
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_momentum.IsFormed || !_rsi.IsFormed)
			return;

		// Extract values from array

		if (values[0].ToNullableDecimal() is not decimal rsiValue)
			return; // Skip if RSI value is null

		if (values[1].ToNullableDecimal() is not decimal highestValue)
			return; // Skip if highest value is null

		if (values[2].ToNullableDecimal() is not decimal lowestValue)
			return; // Skip if lowest value is null

		if (values[3].ToNullableDecimal() is not decimal closeSmaValue)
			return; // Skip if close SMA value is null

		var linRegValue = (LinearRegressionValue)values[4];

		if (linRegValue.LinearRegSlope is not decimal momentumValue)
			return; // Skip if momentum value is not available

		// Process additional indicators manually
		var bollingerValue = _bollingerBands.Process(candle);
		var keltnerValue = _keltnerChannels.Process(candle);

		// Check if indicators are formed
		if (!_bollingerBands.IsFormed || !_keltnerChannels.IsFormed)
			return;

		// Cast to typed values to access properties
		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		var keltnerTyped = (KeltnerChannelsValue)keltnerValue;

		// Calculate TTM Squeeze - check if Bollinger Bands are inside Keltner Channels
		if (bollingerTyped.UpBand is not decimal bbUpper ||
			bollingerTyped.LowBand is not decimal bbLower ||
			keltnerTyped.Upper is not decimal kcUpper ||
			keltnerTyped.Lower is not decimal kcLower)
		{
			return; // Skip if any band is null
		}

		var squeezOn = bbUpper < kcUpper && bbLower > kcLower;

		// Calculate TTM Squeeze momentum oscillator
		// e1 = (highest + lowest) / 2 + sma(close)
		var e1 = (highestValue + lowestValue) / 2 + closeSmaValue;
		_currentMomentum = momentumValue; // LinearRegression of (close - e1/2)

		CheckEntryConditions(candle, rsiValue, squeezOn);

		// Store previous momentum
		_previousMomentum = _currentMomentum;
	}

	private void CheckEntryConditions(ICandleMessage candle, decimal rsiValue, bool squeezOn)
	{
		var currentPrice = candle.ClosePrice;

		// Only trade when squeeze is off (expansion phase)
		if (squeezOn)
			return;

		// Long entry: momentum < 0, momentum increasing for 2 bars, RSI > 30
		if (_currentMomentum < 0 && 
			_previousMomentum != 0 && 
			_currentMomentum > _previousMomentum && 
			rsiValue > 30 && 
			Position == 0)
		{
			RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
		}

		// Short entry: momentum > 0, momentum decreasing for 2 bars, RSI < 70
		if (_currentMomentum > 0 && 
			_previousMomentum != 0 && 
			_currentMomentum < _previousMomentum && 
			rsiValue < 70 && 
			Position == 0)
		{
			RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Volume));
		}
	}
}