
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Exp_Sar_Tm_Plus MQL5 expert advisor.
/// The strategy monitors Parabolic SAR swings on a configurable timeframe and
/// mirrors the original entry/exit automation together with optional time-based protection.
/// </summary>
public class ExpSarTmPlusStrategy : Strategy
{
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MoneyManagementMode> _moneyManagementMode;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _deviationPoints;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<int> _signalBar;

	private decimal?[] _closeBuffer = Array.Empty<decimal?>();
	private decimal?[] _sarBuffer = Array.Empty<decimal?>();
	private int _bufferIndex;
	private int _bufferCount;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private DateTimeOffset? _positionEntryTime;

	/// <summary>
	/// Money management modes replicated from the original expert.
	/// </summary>
	public enum MoneyManagementMode
	{
		FreeMargin,
		Balance,
		LossFreeMargin,
		LossBalance,
		Lot,
	}

	/// <summary>
	/// Portion of the base volume that will be traded.
	/// </summary>
	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	/// <summary>
	/// Interpretation mode for the money management value.
	/// </summary>
	public MoneyManagementMode ManagementMode
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum slippage accepted when executing market orders.
	/// </summary>
	public int DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries when Parabolic SAR flips above price.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Enables short entries when Parabolic SAR flips below price.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allows closing long positions when price falls under the SAR value.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allows closing short positions when price rises above the SAR value.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Enables time-based liquidation of open positions.
	/// </summary>
	public bool UseTimeExit
	{
		get => _useTimeExit.Value;
		set => _useTimeExit.Value = value;
	}

	/// <summary>
	/// Maximum holding time in minutes before a position is closed.
	/// </summary>
	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the Parabolic SAR signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Acceleration step for Parabolic SAR.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration for Parabolic SAR.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Number of closed candles used as signal offset.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Initialize parameters with defaults aligned to the MQL5 implementation.
	/// </summary>
	public ExpSarTmPlusStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
			.SetDisplay("Money Management", "Portion of the base volume used per entry", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_moneyManagementMode = Param(nameof(ManagementMode), MoneyManagementMode.Lot)
			.SetDisplay("Money Management Mode", "Mode used to interpret the money management value", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss (points)", "Stop loss distance measured in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100, 3000, 100)
			.SetGreaterOrEqual(0);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit (points)", "Take profit distance measured in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100, 5000, 100)
			.SetGreaterOrEqual(0);

		_deviationPoints = Param(nameof(DeviationPoints), 10)
			.SetDisplay("Execution Deviation", "Maximum allowed deviation in points", "Orders")
			.SetGreaterOrEqual(0);

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Execution");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Execution");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions on SAR cross", "Execution");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions on SAR cross", "Execution");

		_useTimeExit = Param(nameof(UseTimeExit), true)
			.SetDisplay("Enable Time Exit", "Close positions after the holding period", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 240)
			.SetDisplay("Holding Minutes", "Maximum position holding time in minutes", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(60, 720, 60)
			.SetGreaterOrEqual(0);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Parabolic SAR", "Data");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration step for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m)
			.SetGreaterThanZero();

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m)
			.SetGreaterThanZero();

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar Offset", "Number of closed candles used for signal confirmation", "Data")
			.SetGreaterOrEqual(0);
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

		InitializeBuffers();
		ResetRiskLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
		InitializeBuffers();

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EnsureBufferSize();
		UpdateBuffers(candle.ClosePrice, sarValue);

		if (_bufferCount <= Math.Max(0, SignalBar) + 1)
			return;

		var (currentClose, currentSar, previousClose, previousSar) = GetSignalValues();
		if (currentClose is null || currentSar is null || previousClose is null || previousSar is null)
			return;

		var isPriceAboveCurrentSar = currentClose.Value > currentSar.Value;
		var wasPriceAbovePreviousSar = previousClose.Value > previousSar.Value;

		HandleExits(candle, isPriceAboveCurrentSar);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossedUp = !wasPriceAbovePreviousSar && isPriceAboveCurrentSar;
		var crossedDown = wasPriceAbovePreviousSar && !isPriceAboveCurrentSar;

		if (crossedUp && AllowLongEntry && Position <= 0)
		{
			EnterLong(candle);
		}
		else if (crossedDown && AllowShortEntry && Position >= 0)
		{
			EnterShort(candle);
		}
	}

	private void InitializeBuffers()
	{
		var size = Math.Max(2, Math.Max(0, SignalBar) + 2);

		_closeBuffer = new decimal?[size];
		_sarBuffer = new decimal?[size];
		_bufferIndex = 0;
		_bufferCount = 0;
	}

	private void EnsureBufferSize()
	{
		var size = Math.Max(2, Math.Max(0, SignalBar) + 2);
		if (_closeBuffer.Length == size)
			return;

		_closeBuffer = new decimal?[size];
		_sarBuffer = new decimal?[size];
		_bufferIndex = 0;
		_bufferCount = 0;
	}

	private void UpdateBuffers(decimal close, decimal sar)
	{
		var size = _closeBuffer.Length;
		if (size == 0)
			return;

		_closeBuffer[_bufferIndex] = close;
		_sarBuffer[_bufferIndex] = sar;

		_bufferIndex = (_bufferIndex + 1) % size;
		if (_bufferCount < size)
			_bufferCount++;
	}

	private (decimal? currentClose, decimal? currentSar, decimal? previousClose, decimal? previousSar) GetSignalValues()
	{
		var size = _closeBuffer.Length;
		if (size == 0)
			return (null, null, null, null);

		var signalOffset = Math.Max(0, SignalBar);
		var currentIndex = (_bufferIndex - 1 - signalOffset + size) % size;
		var previousIndex = (_bufferIndex - 2 - signalOffset + size) % size;

		return (
			_closeBuffer[currentIndex],
			_sarBuffer[currentIndex],
			_closeBuffer[previousIndex],
			_sarBuffer[previousIndex]);
	}

	private void HandleExits(ICandleMessage candle, bool isPriceAboveCurrentSar)
	{
		if (Position > 0)
		{
			if (ShouldExitByTime(candle))
			{
				CloseLong();
				return;
			}

			if (AllowLongExit && !isPriceAboveCurrentSar)
			{
				CloseLong();
				return;
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				CloseLong();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				CloseLong();
			}
		}
		else if (Position < 0)
		{
			if (ShouldExitByTime(candle))
			{
				CloseShort();
				return;
			}

			if (AllowShortExit && isPriceAboveCurrentSar)
			{
				CloseShort();
				return;
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				CloseShort();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				CloseShort();
			}
		}
	}

	private bool ShouldExitByTime(ICandleMessage candle)
	{
		if (!UseTimeExit || !_positionEntryTime.HasValue)
			return false;

		var holdingPeriod = TimeSpan.FromMinutes(Math.Max(0, HoldingMinutes));
		if (holdingPeriod <= TimeSpan.Zero)
			return false;

		var closeTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		return closeTime - _positionEntryTime.Value >= holdingPeriod;
	}

	private void EnterLong(ICandleMessage candle)
	{
		ResetRiskLevels();

		var volume = GetOrderVolume() + Math.Abs(Math.Min(0m, Position));
		if (volume <= 0)
			return;

		BuyMarket(volume);

		_positionEntryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0)
			priceStep = 1m;

		_stopPrice = StopLossPoints > 0 ? candle.ClosePrice - priceStep * StopLossPoints : null;
		_takePrice = TakeProfitPoints > 0 ? candle.ClosePrice + priceStep * TakeProfitPoints : null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		ResetRiskLevels();

		var volume = GetOrderVolume() + Math.Abs(Math.Max(0m, Position));
		if (volume <= 0)
			return;

		SellMarket(volume);

		_positionEntryTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0)
			priceStep = 1m;

		_stopPrice = StopLossPoints > 0 ? candle.ClosePrice + priceStep * StopLossPoints : null;
		_takePrice = TakeProfitPoints > 0 ? candle.ClosePrice - priceStep * TakeProfitPoints : null;
	}

	private void CloseLong()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
			return;

		SellMarket(volume);
		ResetRiskLevels();
	}

	private void CloseShort()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		ResetRiskLevels();
	}

	private decimal GetOrderVolume()
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var baseVolume = Volume * MoneyManagement;
		if (baseVolume <= 0)
			baseVolume = Volume;

		var normalized = Math.Round(baseVolume / step) * step;
		if (normalized <= 0)
			normalized = step;

		return normalized;
	}

	private void ResetRiskLevels()
	{
		_stopPrice = null;
		_takePrice = null;
		_positionEntryTime = null;
	}
}
