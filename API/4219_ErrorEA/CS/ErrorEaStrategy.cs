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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "errorEA" MetaTrader strategy that compares +DI and -DI lines of ADX.
/// </summary>
public class ErrorEaStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _enableRiskControl;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _riskDivider;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private int _longTrades;
	private int _shortTrades;

	/// <summary>
	/// ADX averaging period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of scale-in entries per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Enables dynamic position sizing based on the portfolio value.
	/// </summary>
	public bool EnableRiskControl
	{
		get => _enableRiskControl.Value;
		set => _enableRiskControl.Value = value;
	}

	/// <summary>
	/// Maximum order volume allowed by the strategy.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Minimum order volume that should be used.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Base volume multiplier that matches the MiniLots parameter from MQL.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Divider applied to the portfolio value when risk control is enabled.
	/// </summary>
	public decimal RiskDivider
	{
		get => _riskDivider.Value;
		set => _riskDivider.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enables the scalping take-profit mode from the original EA.
	/// </summary>
	public bool EnableTakeProfit
	{
		get => _enableTakeProfit.Value;
		set => _enableTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for data subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErrorEaStrategy"/> class.
	/// </summary>
	public ErrorEaStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ADX Period", "Smoothing period for the Average Directional Index", "Indicators")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 9)
			.SetRange(1, 15)
			.SetDisplay("Max Trades", "Maximum number of simultaneous entries per direction", "Risk")
			.SetCanOptimize(true);

		_enableRiskControl = Param(nameof(EnableRiskControl), true)
			.SetDisplay("Enable Risk Control", "Adjust volume by portfolio value similar to the MQL version", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 3m)
			.SetNotNegative()
			.SetDisplay("Max Volume", "Upper limit for market orders", "Risk");

		_minVolume = Param(nameof(MinVolume), 0.01m)
			.SetNotNegative()
			.SetDisplay("Min Volume", "Lower limit for market orders", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.15m)
			.SetNotNegative()
			.SetDisplay("Base Volume", "Base lot used before applying risk control", "Risk")
			.SetCanOptimize(true);

		_riskDivider = Param(nameof(RiskDivider), 10000m)
			.SetNotNegative()
			.SetDisplay("Risk Divider", "Portfolio divider used to scale volume when risk control is enabled", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Stop distance converted to price steps", "Protection")
			.SetCanOptimize(true);

		_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
			.SetDisplay("Enable Take Profit", "Activate the small scalping take profit from the EA", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Take-profit distance converted to price steps", "Protection")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");
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

		_adx = null;
		_longTrades = 0;
		_shortTrades = 0;

		Volume = BaseVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		// Subscribe to the configured candle series and calculate ADX on the fly.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var takeProfitUnit = EnableTakeProfit && TakeProfitPoints > 0
			? new Unit(TakeProfitPoints, UnitTypes.Step)
			: null;
		var stopLossUnit = StopLossPoints > 0
			? new Unit(StopLossPoints, UnitTypes.Step)
			: null;

		// Mirror the original stop-loss and scalping take-profit distances.
		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			useMarketOrders: true);

		// Preload the base volume so manual actions in the UI use the same size.
		var adjustedVolume = AdjustVolume(BaseVolume);
		Volume = adjustedVolume > 0m ? adjustedVolume : BaseVolume;

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_adx != null)
				DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		// Only evaluate completed candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until all subscriptions are online and indicators formed.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Ensure ADX produced a final value for this bar.
		if (adxValue is not AverageDirectionalIndexValue adx || !adxValue.IsFinal)
			return;

		var plusDi = adx.Dx.Plus;
		var minusDi = adx.Dx.Minus;

		// Compare +DI and -DI components to determine the signal.
		var direction = CalculateDirection(plusDi, minusDi);

		switch (direction)
		{
			case > 0:
				HandleLongSignal();
				break;
			case < 0:
				HandleShortSignal();
				break;
			default:
				break;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		// Reset scaling counters once the net position flips or becomes flat.
		if (Position == 0)
		{
			_longTrades = 0;
			_shortTrades = 0;
		}
		else if (Position > 0)
		{
			_shortTrades = 0;
		}
		else
		{
			_longTrades = 0;
		}
	}

	private int CalculateDirection(decimal plusDi, decimal minusDi)
	{
		if (plusDi > minusDi)
			return 1;

		if (minusDi > plusDi)
			return -1;

		return 0;
	}

	private void HandleLongSignal()
	{
		if (Security is null)
			return;

		// Netting accounts cannot keep opposite positions, so close shorts first.
		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_shortTrades = 0;
		}

		// Respect the scaling cap inherited from the original EA.
		if (_longTrades >= MaxTrades)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		// Add one more market order using the calculated lot size.
		BuyMarket(volume);
		_longTrades++;
	}

	private void HandleShortSignal()
	{
		if (Security is null)
			return;

		// Flat the long exposure before opening new short trades.
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_longTrades = 0;
		}

		if (_shortTrades >= MaxTrades)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortTrades++;
	}

	private decimal CalculateOrderVolume()
	{
		// Start from the base lot size defined by BaseVolume.
		var volume = BaseVolume;

		if (EnableRiskControl)
		{
			// Reproduce the PowerRisk logic: balance / divider with a floor of 1.
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			if (portfolioValue <= 0m)
				portfolioValue = 0m;

			var riskFactor = RiskDivider > 0m ? portfolioValue / RiskDivider : 0m;

			if (riskFactor < 1m)
				riskFactor = 1m;

			volume *= riskFactor;
		}

		// Apply user-defined caps before exchange-specific adjustments.
		if (MaxVolume > 0m && volume > MaxVolume)
			volume = MaxVolume;

		if (MinVolume > 0m && volume < MinVolume)
			volume = MinVolume;

		// Align with exchange volume constraints.
		var adjusted = AdjustVolume(volume);
		if (MaxVolume > 0m && adjusted > MaxVolume)
			adjusted = MaxVolume;

		if (adjusted <= 0m && MinVolume > 0m)
			adjusted = MinVolume;

		return adjusted;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			// Round the value to the nearest allowed volume step.
			var rounded = step * Math.Floor(volume / step);
			volume = rounded > 0m ? rounded : step;
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}
}
