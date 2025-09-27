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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI and EMA based strategy converted from the original MQL implementation.
/// Combines a custom RSI*EMA momentum oscillator with basic risk management.
/// </summary>
public class RsiMaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldActivationLevel;
	private readonly StrategyParam<decimal> _oversoldExtremeLevel;
	private readonly StrategyParam<decimal> _overboughtActivationLevel;
	private readonly StrategyParam<decimal> _overboughtExtremeLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;
	
	private decimal? _previousEmaValue;
	private decimal? _previousIndicatorValue;
	
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryPrice;
	
	public RsiMaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", string.Empty, "Oscillator")
		.SetCanOptimize(true);
		
		_oversoldActivationLevel = Param(nameof(OversoldActivationLevel), 20m)
		.SetDisplay("Oversold Activation", string.Empty, "Oscillator")
		.SetCanOptimize(true);
		
		_oversoldExtremeLevel = Param(nameof(OversoldExtremeLevel), 5m)
		.SetDisplay("Oversold Extreme", string.Empty, "Oscillator");
		
		_overboughtActivationLevel = Param(nameof(OverboughtActivationLevel), 80m)
		.SetDisplay("Overbought Activation", string.Empty, "Oscillator")
		.SetCanOptimize(true);
		
		_overboughtExtremeLevel = Param(nameof(OverboughtExtremeLevel), 95m)
		.SetDisplay("Overbought Extreme", string.Empty, "Oscillator");
		
		_stopLossPips = Param(nameof(StopLossPips), 399m)
		.SetDisplay("Stop Loss (pips)", string.Empty, "Risk");
		
		_takeProfitPips = Param(nameof(TakeProfitPips), 999m)
		.SetDisplay("Take Profit (pips)", string.Empty, "Risk");
		
		_trailingStopPips = Param(nameof(TrailingStopPips), 299m)
		.SetDisplay("Trailing Stop (pips)", string.Empty, "Risk");
		
		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", string.Empty, "Risk");
		
		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", string.Empty, "Risk");
		
		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing Stop", string.Empty, "Risk");
		
		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Use Risk Percent Position Sizing", string.Empty, "Position");
		
		_riskPercent = Param(nameof(RiskPercent), 10m)
		.SetDisplay("Risk Percent", string.Empty, "Position");
		
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Fixed Volume", string.Empty, "Position");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle TimeFrame", string.Empty, "General");
	}
	
	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// Activation threshold after an oversold extreme.
	/// </summary>
	public decimal OversoldActivationLevel
	{
		get => _oversoldActivationLevel.Value;
		set => _oversoldActivationLevel.Value = value;
	}
	
	/// <summary>
	/// Oversold extreme required before a long setup becomes valid.
	/// </summary>
	public decimal OversoldExtremeLevel
	{
		get => _oversoldExtremeLevel.Value;
		set => _oversoldExtremeLevel.Value = value;
	}
	
	/// <summary>
	/// Activation threshold after an overbought extreme.
	/// </summary>
	public decimal OverboughtActivationLevel
	{
		get => _overboughtActivationLevel.Value;
		set => _overboughtActivationLevel.Value = value;
	}
	
	/// <summary>
	/// Overbought extreme required before a short setup becomes valid.
	/// </summary>
	public decimal OverboughtExtremeLevel
	{
		get => _overboughtExtremeLevel.Value;
		set => _overboughtExtremeLevel.Value = value;
	}
	
	/// <summary>
	/// Stop-loss distance measured in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}
	
	/// <summary>
	/// Take-profit distance measured in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}
	
	/// <summary>
	/// Trailing-stop distance measured in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}
	
	/// <summary>
	/// Enable or disable stop-loss management.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}
	
	/// <summary>
	/// Enable or disable take-profit management.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}
	
	/// <summary>
	/// Enable or disable trailing stop adjustments.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}
	
	/// <summary>
	/// Enable or disable percent based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}
	
	/// <summary>
	/// Portfolio risk percentage when money management is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}
	
	/// <summary>
	/// Fixed volume used when money management is disabled.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}
	
	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};
		
		_ema = new ExponentialMovingAverage
		{
			Length = RsiPeriod
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		var weightedPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m;
		var emaValue = _ema.Process(weightedPrice, candle.OpenTime, true).ToDecimal();
		
		if (!_ema.IsFormed)
		{
			_previousEmaValue = emaValue;
			return;
		}
		
		if (!_rsi.IsFormed || _previousEmaValue is null)
		{
			_previousEmaValue = emaValue;
			return;
		}
		
		var pipSize = GetPipSize();
		if (pipSize == 0m)
			pipSize = 0.0001m;
		
		var emaDiff = (emaValue - _previousEmaValue.Value) / pipSize;
		var indicatorValue = rsiValue * emaDiff;
		indicatorValue = Math.Max(1m, Math.Min(99m, indicatorValue));
		
		if (_previousIndicatorValue is decimal previousValue)
		{
			ManageOpenPosition(candle, previousValue, indicatorValue);
			EvaluateEntries(candle, previousValue, indicatorValue);
		}
		
		_previousIndicatorValue = indicatorValue;
		_previousEmaValue = emaValue;
	}
	
	private void ManageOpenPosition(ICandleMessage candle, decimal previousValue, decimal currentValue)
	{
		if (Position > 0)
		{
			var exitSignal = previousValue > OverboughtExtremeLevel && currentValue < OverboughtActivationLevel;
			if (exitSignal)
			{
				ClosePosition();
				ResetRiskLevels();
				return;
			}
			
			UpdateTrailingStopForLong(candle);
			
			if (ShouldCloseLong(candle))
			{
				ClosePosition();
				ResetRiskLevels();
			}
		}
		else if (Position < 0)
		{
			var exitSignal = previousValue < OversoldExtremeLevel && currentValue > OversoldActivationLevel;
			if (exitSignal)
			{
				ClosePosition();
				ResetRiskLevels();
				return;
			}
			
			UpdateTrailingStopForShort(candle);
			
			if (ShouldCloseShort(candle))
			{
				ClosePosition();
				ResetRiskLevels();
			}
		}
	}
	
	private void EvaluateEntries(ICandleMessage candle, decimal previousValue, decimal currentValue)
	{
		var price = candle.ClosePrice;
		if (price <= 0m)
			return;
		
		if (previousValue < OversoldExtremeLevel && currentValue > OversoldActivationLevel && Position <= 0)
		{
			_entryPrice = price;
			InitializeRiskLevelsForLong(price);
			BuyMarket(GetOrderVolume(price));
		}
		else if (previousValue > OverboughtExtremeLevel && currentValue < OverboughtActivationLevel && Position >= 0)
		{
			_entryPrice = price;
			InitializeRiskLevelsForShort(price);
			SellMarket(GetOrderVolume(price));
		}
	}
	
	private void InitializeRiskLevelsForLong(decimal price)
	{
		var pipDistance = GetPipSize();
		
		if (UseStopLoss && StopLossPips > 0m)
			_stopLossPrice = price - pipDistance * StopLossPips;
		else
			_stopLossPrice = null;
		
		if (UseTakeProfit && TakeProfitPips > 0m)
			_takeProfitPrice = price + pipDistance * TakeProfitPips;
		else
			_takeProfitPrice = null;
	}
	
	private void InitializeRiskLevelsForShort(decimal price)
	{
		var pipDistance = GetPipSize();
		
		if (UseStopLoss && StopLossPips > 0m)
			_stopLossPrice = price + pipDistance * StopLossPips;
		else
			_stopLossPrice = null;
		
		if (UseTakeProfit && TakeProfitPips > 0m)
			_takeProfitPrice = price - pipDistance * TakeProfitPips;
		else
			_takeProfitPrice = null;
	}
	
	private void UpdateTrailingStopForLong(ICandleMessage candle)
	{
		if (!UseTrailingStop || TrailingStopPips <= 0m)
			return;
		
		var pipDistance = GetPipSize() * TrailingStopPips;
		if (pipDistance <= 0m)
			return;
		
		var profit = candle.ClosePrice - _entryPrice;
		if (profit <= pipDistance)
			return;
		
		var newStop = candle.ClosePrice - pipDistance;
		if (_stopLossPrice is null || newStop > _stopLossPrice)
			_stopLossPrice = newStop;
	}
	
	private void UpdateTrailingStopForShort(ICandleMessage candle)
	{
		if (!UseTrailingStop || TrailingStopPips <= 0m)
			return;
		
		var pipDistance = GetPipSize() * TrailingStopPips;
		if (pipDistance <= 0m)
			return;
		
		var profit = _entryPrice - candle.ClosePrice;
		if (profit <= pipDistance)
			return;
		
		var newStop = candle.ClosePrice + pipDistance;
		if (_stopLossPrice is null || newStop < _stopLossPrice)
			_stopLossPrice = newStop;
	}
	
	private bool ShouldCloseLong(ICandleMessage candle)
	{
		var stopHit = UseStopLoss && _stopLossPrice is decimal stop && candle.LowPrice <= stop;
		var takeProfitHit = UseTakeProfit && _takeProfitPrice is decimal takeProfit && candle.HighPrice >= takeProfit;
		return stopHit || takeProfitHit;
	}
	
	private bool ShouldCloseShort(ICandleMessage candle)
	{
		var stopHit = UseStopLoss && _stopLossPrice is decimal stop && candle.HighPrice >= stop;
		var takeProfitHit = UseTakeProfit && _takeProfitPrice is decimal takeProfit && candle.LowPrice <= takeProfit;
		return stopHit || takeProfitHit;
	}
	
	private void ResetRiskLevels()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
	}
	
	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep is null || priceStep == 0m)
			return 0.0001m;
		
		return priceStep.Value;
	}
	
	private decimal GetOrderVolume(decimal price)
	{
		var volume = TradeVolume;
		
		if (!UseMoneyManagement || Portfolio is null || price <= 0m)
			return volume;
		
		var portfolioValue = Portfolio.CurrentValue;
		if (portfolioValue <= 0m)
			return volume;
		
		var riskAmount = portfolioValue * RiskPercent / 100m;
		if (riskAmount <= 0m)
			return volume;
		
		var estimatedVolume = riskAmount / price;
		
		var volumeStep = Security?.VolumeStep;
		if (volumeStep is not null && volumeStep > 0m)
		{
			estimatedVolume = Math.Floor(estimatedVolume / volumeStep.Value) * volumeStep.Value;
		}
		
		if (estimatedVolume <= 0m)
			estimatedVolume = volume;
		
		return estimatedVolume;
	}
}
