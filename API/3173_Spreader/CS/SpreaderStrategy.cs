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

using Ecng.ComponentModel;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Classic spread trading strategy inspired by the MetaTrader "Spreader" expert advisor.
/// It trades two positively correlated instruments on short-term pullbacks,
/// rebalancing the legs once the combined profit target is reached.
/// </summary>
public class SpreaderStrategy : Strategy
{
	private readonly StrategyParam<Security> _secondSecurityParam;
	private readonly StrategyParam<decimal> _primaryVolumeParam;
	private readonly StrategyParam<decimal> _targetProfitParam;
	private readonly StrategyParam<int> _shiftParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private readonly List<decimal> _primaryCloses = new();
	private readonly List<decimal> _secondaryCloses = new();

	private ICandleMessage _lastPrimaryCandle;
	private ICandleMessage _lastSecondaryCandle;

	private decimal _primaryEntryPrice;
	private decimal _secondaryEntryPrice;

	private bool _contractsMatch = true;

	/// <summary>
	/// Secondary security that forms the hedge leg of the spread.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurityParam.Value;
		set => _secondSecurityParam.Value = value;
	}

	/// <summary>
	/// Base order volume for the primary security.
	/// </summary>
	public decimal PrimaryVolume
	{
		get => _primaryVolumeParam.Value;
		set => _primaryVolumeParam.Value = value;
	}

	/// <summary>
	/// Desired absolute profit for the combined position.
	/// </summary>
	public decimal TargetProfit
	{
		get => _targetProfitParam.Value;
		set => _targetProfitParam.Value = value;
	}

	/// <summary>
	/// Number of bars between comparison points used to estimate short-term swings.
	/// </summary>
	public int ShiftLength
	{
		get => _shiftParam.Value;
		set => _shiftParam.Value = value;
	}

	/// <summary>
	/// Candle type that defines the timeframe for spread calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpreaderStrategy"/> class.
	/// </summary>
	public SpreaderStrategy()
	{
		_secondSecurityParam = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Symbol", "Secondary instrument for the spread", "General")
			.SetRequired();

		_primaryVolumeParam = Param(nameof(PrimaryVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Primary Volume", "Order volume for the primary symbol", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_targetProfitParam = Param(nameof(TargetProfit), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Target Profit", "Total profit target for the pair", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_shiftParam = Param(nameof(ShiftLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Shift Length", "Number of bars between comparison points", "Logic")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 10);

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for spread calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (SecondSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_primaryCloses.Clear();
		_secondaryCloses.Clear();

		_lastPrimaryCandle = null;
		_lastSecondaryCandle = null;

		_primaryEntryPrice = 0m;
		_secondaryEntryPrice = 0m;

		_contractsMatch = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (SecondSecurity == null)
			throw new InvalidOperationException("Second security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		if (Security.Multiplier != null && SecondSecurity.Multiplier != null && Security.Multiplier != SecondSecurity.Multiplier)
		{
			LogWarning($"Contract size mismatch between {Security.Code} and {SecondSecurity.Code}. Trading disabled.");
			_contractsMatch = false;
		}

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		var secondarySubscription = SubscribeCandles(CandleType, security: SecondSecurity);
		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrimaryCandle = candle;
		_primaryCloses.Add(candle.ClosePrice);
		TrimCloses(_primaryCloses);

		TryEvaluateSpread();
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastSecondaryCandle = candle;
		_secondaryCloses.Add(candle.ClosePrice);
		TrimCloses(_secondaryCloses);

		TryEvaluateSpread();
	}

	private void TrimCloses(List<decimal> closes)
	{
		var maxCount = Math.Max(1, ShiftLength * 2 + 1);
		while (closes.Count > maxCount)
			closes.RemoveAt(0);
	}

	private void TryEvaluateSpread()
	{
		if (!_contractsMatch)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_lastPrimaryCandle == null || _lastSecondaryCandle == null)
			return;

		if (_lastPrimaryCandle.OpenTime != _lastSecondaryCandle.OpenTime)
			return;

		if (Position == 0m)
			_primaryEntryPrice = 0m;

		var secondaryPosition = GetPositionVolume(SecondSecurity);
		if (secondaryPosition == 0m)
			_secondaryEntryPrice = 0m;

		var hasPrimary = Position != 0m;
		var hasSecondary = secondaryPosition != 0m;

		var firstClose = _lastPrimaryCandle.ClosePrice;
		var secondClose = _lastSecondaryCandle.ClosePrice;

		if (!hasSecondary && hasPrimary)
		{
			LogInfo("Secondary leg missing. Closing primary position to avoid imbalance.");
			ClosePosition();
			return;
		}

		if (hasPrimary && hasSecondary)
		{
			if (_primaryEntryPrice == 0m || _secondaryEntryPrice == 0m)
				return;

			var primaryVolume = Math.Abs(Position);
			var secondaryVolume = Math.Abs(secondaryPosition);

			if (primaryVolume == 0m || secondaryVolume == 0m)
				return;

			var primaryProfit = Position > 0m
				? (firstClose - _primaryEntryPrice) * primaryVolume
				: (_primaryEntryPrice - firstClose) * primaryVolume;

			var secondaryProfit = secondaryPosition > 0m
				? (secondClose - _secondaryEntryPrice) * secondaryVolume
				: (_secondaryEntryPrice - secondClose) * secondaryVolume;

			var totalProfit = primaryProfit + secondaryProfit;

			if (totalProfit >= TargetProfit)
			{
				LogInfo($"Target profit reached ({totalProfit:F2}). Closing both legs.");
				CloseSecondaryPosition();
				ClosePosition();
			}

			return;
		}

		if (hasSecondary && !hasPrimary)
		{
			var primarySide = secondaryPosition > 0m ? Sides.Sell : Sides.Buy;
			LogInfo("Primary leg missing. Opening opposite trade to balance the spread.");
			OpenPrimary(primarySide, PrimaryVolume);
			return;
		}

		EvaluateEntry(firstClose, secondClose);
	}

	private void EvaluateEntry(decimal firstClose, decimal secondClose)
	{
		var shift = ShiftLength;
		if (shift <= 0)
			return;

		var required = shift * 2 + 1;
		if (_primaryCloses.Count < required || _secondaryCloses.Count < required)
			return;

		var firstIdx = _primaryCloses.Count - 1;
		var firstShiftIdx = firstIdx - shift;
		var firstDoubleShiftIdx = firstIdx - shift * 2;

		var secondIdx = _secondaryCloses.Count - 1;
		var secondShiftIdx = secondIdx - shift;
		var secondDoubleShiftIdx = secondIdx - shift * 2;

		if (firstShiftIdx < 0 || firstDoubleShiftIdx < 0 || secondShiftIdx < 0 || secondDoubleShiftIdx < 0)
			return;

		var x1 = _primaryCloses[firstIdx] - _primaryCloses[firstShiftIdx];
		var x2 = _primaryCloses[firstShiftIdx] - _primaryCloses[firstDoubleShiftIdx];
		var y1 = _secondaryCloses[secondIdx] - _secondaryCloses[secondShiftIdx];
		var y2 = _secondaryCloses[secondShiftIdx] - _secondaryCloses[secondDoubleShiftIdx];

		if (x1 * x2 > 0m)
		{
			LogDebug($"Trend detected on {Security?.Code}. Skipping spread entry.");
			return;
		}

		if (y1 * y2 > 0m)
		{
			LogDebug($"Trend detected on {SecondSecurity?.Code}. Skipping spread entry.");
			return;
		}

		if (x1 * y1 <= 0m)
		{
			LogDebug("Negative correlation detected. Spread entry rejected.");
			return;
		}

		var a = Math.Abs(x1) + Math.Abs(x2);
		var b = Math.Abs(y1) + Math.Abs(y2);

		if (a <= 0m || b <= 0m)
			return;

		var ratio = a / b;

		if (ratio > 3m || ratio < 0.3m)
			return;

		var primaryVolume = NormalizeVolume(Security, PrimaryVolume);
		if (primaryVolume <= 0m)
			return;

		var secondaryVolume = NormalizeVolume(SecondSecurity, primaryVolume * ratio);
		if (secondaryVolume <= 0m)
			return;

		var primarySide = x1 * b > y1 * a ? Sides.Buy : Sides.Sell;
		var secondarySide = primarySide == Sides.Buy ? Sides.Sell : Sides.Buy;

		LogInfo($"Opening spread: {primarySide} {primaryVolume} {Security?.Code}, {secondarySide} {secondaryVolume} {SecondSecurity?.Code}.");

		OpenSecondary(secondarySide, secondaryVolume, secondClose);
		OpenPrimary(primarySide, primaryVolume, firstClose);
	}

	private void OpenPrimary(Sides side, decimal requestedVolume, decimal? entryPrice = null)
	{
		var volume = NormalizeVolume(Security, requestedVolume);
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_primaryEntryPrice = entryPrice ?? (_lastPrimaryCandle?.ClosePrice ?? 0m);
	}

	private void OpenPrimary(Sides side, decimal requestedVolume)
	{
		OpenPrimary(side, requestedVolume, _lastPrimaryCandle?.ClosePrice);
	}

	private void OpenSecondary(Sides side, decimal requestedVolume, decimal? entryPrice = null)
	{
		if (SecondSecurity == null)
			return;

		var volume = NormalizeVolume(SecondSecurity, requestedVolume);
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
			BuyMarket(volume, SecondSecurity);
		else
			SellMarket(volume, SecondSecurity);

		_secondaryEntryPrice = entryPrice ?? (_lastSecondaryCandle?.ClosePrice ?? 0m);
	}

	private void CloseSecondaryPosition()
	{
		if (SecondSecurity == null)
			return;

		var position = GetPositionVolume(SecondSecurity);
		if (position > 0m)
		{
			SellMarket(position, SecondSecurity);
			_secondaryEntryPrice = 0m;
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position), SecondSecurity);
			_secondaryEntryPrice = 0m;
		}
	}

	private decimal GetPositionVolume(Security security)
	{
		return security == null || Portfolio == null ? 0m : GetPositionValue(security, Portfolio) ?? 0m;
	}

	private static decimal NormalizeVolume(Security security, decimal volume)
	{
		if (security == null)
			return volume;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		if (security.VolumeMin is decimal min && min > 0m && volume < min)
			volume = min;

		if (security.VolumeMax is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}
}

