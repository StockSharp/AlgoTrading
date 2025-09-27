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
/// Port of the "Vlado" MetaTrader expert advisor that trades Williams %R level breakouts.
/// </summary>
public class VladoWilliamsPercentRangeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprLength;
	private readonly StrategyParam<decimal> _wprLevel;
	private readonly StrategyParam<bool> _useRiskMoneyManagement;
	private readonly StrategyParam<decimal> _maximumRiskPercent;

	private bool _buySignal;
	private bool _sellSignal;
	private int _lastSignal;

	private WilliamsR _williamsR = null!;

	/// <summary>
	/// Initializes a new instance of the <see cref="VladoWilliamsPercentRangeStrategy"/> class.
	/// </summary>
	public VladoWilliamsPercentRangeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");

		_wprLength = Param(nameof(WprLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Lookback period for Williams %R", "Indicators")
			.SetCanOptimize(true);

		_wprLevel = Param(nameof(WprLevel), -50m)
			.SetDisplay("Williams %R Level", "Threshold that flips the bias", "Signals")
			.SetCanOptimize(true);

		_useRiskMoneyManagement = Param(nameof(UseRiskMoneyManagement), false)
			.SetDisplay("Risk Money Management", "Recalculate volume from equity before entries", "Risk")
			.SetCanOptimize(true);

		_maximumRiskPercent = Param(nameof(MaximumRiskPercent), 10m)
			.SetDisplay("Maximum Risk Percent", "Equity percentage used when sizing orders", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Williams %R lookback length.
	/// </summary>
	public int WprLength
	{
		get => _wprLength.Value;
		set => _wprLength.Value = value;
	}

	/// <summary>
	/// Threshold that toggles bullish or bearish bias.
	/// </summary>
	public decimal WprLevel
	{
		get => _wprLevel.Value;
		set => _wprLevel.Value = value;
	}

	/// <summary>
	/// Enables risk based volume sizing similar to the MetaTrader version.
	/// </summary>
	public bool UseRiskMoneyManagement
	{
		get => _useRiskMoneyManagement.Value;
		set => _useRiskMoneyManagement.Value = value;
	}

	/// <summary>
	/// Fraction of the current equity used to size entries when risk management is enabled.
	/// </summary>
	public decimal MaximumRiskPercent
	{
		get => _maximumRiskPercent.Value;
		set => _maximumRiskPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buySignal = false;
		_sellSignal = false;
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williamsR = new WilliamsR
		{
			Length = WprLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williamsR);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSignals(wprValue);

		if (Position != 0)
		{
			if (Position > 0 && _sellSignal)
			{
				// Exit long positions when the bearish Williams %R regime appears.
				ClosePosition();
				return;
			}

			if (Position < 0 && _buySignal)
			{
				// Exit short positions when the bullish Williams %R regime appears.
				ClosePosition();
				return;
			}

			return;
		}

		// No open position - evaluate fresh entries.
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		if (_sellSignal && _lastSignal != -1)
		{
			// Enter short once Williams %R falls below the chosen level.
			SellMarket(volume);
			_lastSignal = -1;
			return;
		}

		if (_buySignal && _lastSignal != 1)
		{
			// Enter long once Williams %R rises above the chosen level.
			BuyMarket(volume);
			_lastSignal = 1;
		}
	}

	private void UpdateSignals(decimal wprValue)
	{
		// Williams %R values are negative: less negative indicates bullish momentum.
		if (wprValue > WprLevel)
		{
			_buySignal = true;
			_sellSignal = false;
		}
		else if (wprValue < WprLevel)
		{
			_sellSignal = true;
			_buySignal = false;
		}
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		var volume = Volume;

		if (UseRiskMoneyManagement && MaximumRiskPercent > 0m && referencePrice > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? 0m;
			if (equity > 0m)
			{
				// Convert risk capital to volume using the latest close price as approximation.
				volume = equity * (MaximumRiskPercent / 100m) / referencePrice;
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var normalized = volume;

		if (Security?.VolumeStep is decimal step && step > 0m)
		{
			var steps = decimal.Floor(normalized / step);
			normalized = steps * step;

			if (normalized <= 0m)
				normalized = step;
		}

		if (Security?.MinVolume is decimal minVolume && minVolume > 0m && normalized < minVolume)
			normalized = minVolume;

		if (Security?.MaxVolume is decimal maxVolume && maxVolume > 0m && normalized > maxVolume)
			normalized = maxVolume;

		if (normalized <= 0m && volume > 0m)
			normalized = volume;

		return normalized;
	}
}

