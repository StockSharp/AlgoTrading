using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Oscillator Threshold strategy.
/// Buys when RSI drops below a fixed or adaptive threshold.
/// Exits after a fixed number of bars or when a dollar stop-loss is hit.
/// </summary>
public class AdaptiveOscillatorThresholdStrategy : Strategy
{
	private readonly StrategyParam<bool> _useAdaptiveThreshold;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _buyLevel;
	private readonly StrategyParam<int> _adaptiveLength;
	private readonly StrategyParam<decimal> _adaptiveCoefficient;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<decimal> _dollarStopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private StandardDeviation _stdDev = null!;
	private LinearRegression _linReg = null!;

	private long _entryBarIndex = -1;
	private decimal _entryPrice;
	private long _currentBar;

	/// <summary>
	/// Enables adaptive threshold (BAT system) when true.
	/// </summary>
	public bool UseAdaptiveThreshold
	{
		get => _useAdaptiveThreshold.Value;
		set => _useAdaptiveThreshold.Value = value;
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
	/// Traditional RSI buy level.
	/// </summary>
	public int BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	/// <summary>
	/// Length for adaptive threshold calculation.
	/// </summary>
	public int AdaptiveLength
	{
		get => _adaptiveLength.Value;
		set => _adaptiveLength.Value = value;
	}

	/// <summary>
	/// Coefficient for adaptive threshold.
	/// </summary>
	public decimal AdaptiveCoefficient
	{
		get => _adaptiveCoefficient.Value;
		set => _adaptiveCoefficient.Value = value;
	}

	/// <summary>
	/// Number of bars after which to exit the position.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}

	/// <summary>
	/// Dollar stop-loss amount.
	/// </summary>
	public decimal DollarStopLoss
	{
		get => _dollarStopLoss.Value;
		set => _dollarStopLoss.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AdaptiveOscillatorThresholdStrategy"/>.
	/// </summary>
	public AdaptiveOscillatorThresholdStrategy()
	{
		_useAdaptiveThreshold = Param(nameof(UseAdaptiveThreshold), true)
			.SetDisplay("Use Adaptive Threshold", "Enable adaptive threshold (BAT system)", "Signal");

		_rsiLength = Param(nameof(RsiLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_buyLevel = Param(nameof(BuyLevel), 14)
			.SetGreaterThanZero()
			.SetDisplay("Buy Level", "Traditional RSI buy level", "Signal")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_adaptiveLength = Param(nameof(AdaptiveLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Adaptive Length", "Length for adaptive threshold", "Adaptive")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 2);

		_adaptiveCoefficient = Param(nameof(AdaptiveCoefficient), 6m)
			.SetGreaterThanZero()
			.SetDisplay("Adaptive Coefficient", "Coefficient for adaptive threshold", "Adaptive")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_exitBars = Param(nameof(ExitBars), 28)
			.SetGreaterThanZero()
			.SetDisplay("Fixed-Bar Exit", "Bars after entry to exit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_dollarStopLoss = Param(nameof(DollarStopLoss), 1600m)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Dollar Stop-Loss", "Maximum dollar loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_stdDev = new StandardDeviation { Length = AdaptiveLength };
		_linReg = new LinearRegression { Length = AdaptiveLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([_rsi, _stdDev, _linReg], ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_currentBar++;

		if (!_rsi.IsFormed || !_stdDev.IsFormed || !_linReg.IsFormed)
			return;

		if (values[0].ToNullableDecimal() is not decimal rsiValue)
			return;

		if (values[1].ToNullableDecimal() is not decimal sd || sd == 0)
			return;

		var reg = (LinearRegressionValue)values[2];
		if (reg.LinearRegSlope is not decimal slope)
			return;

		decimal threshold = BuyLevel;

		if (UseAdaptiveThreshold)
		{
			var bat = Math.Min(0.5m, Math.Max(-0.5m, slope / sd));
			threshold = BuyLevel * AdaptiveCoefficient * bat;
		}

		if (Position <= 0 && rsiValue < threshold)
		{
			BuyMarket();
			_entryBarIndex = _currentBar;
			_entryPrice = candle.ClosePrice;
			return;
		}

		if (Position > 0)
		{
			if (ExitBars > 0 && _entryBarIndex >= 0 && _currentBar - _entryBarIndex >= ExitBars)
			{
				SellMarket();
				return;
			}

			if (DollarStopLoss > 0)
			{
				var profit = (candle.ClosePrice - _entryPrice) * Position;
				if (profit <= -DollarStopLoss)
				{
					SellMarket();
				}
			}
		}
	}
}

