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
/// Strategy that replicates the Volume Calculator expert advisor and prints risk metrics.
/// </summary>
public class VolumeCalculatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPrice;
	private readonly StrategyParam<decimal> _takeProfitPrice;
	private readonly StrategyParam<decimal> _maxLossPercent;
	private readonly StrategyParam<bool> _isLongPosition;

	private decimal? _maxLossValue;
	private decimal? _takeProfitValue;
	private decimal? _riskRewardRatio;
	private decimal? _suggestedVolume;
	private decimal? _stopLossSteps;
	private decimal? _takeProfitSteps;

	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	public decimal TakeProfitPrice
	{
		get => _takeProfitPrice.Value;
		set => _takeProfitPrice.Value = value;
	}

	public decimal MaxLossPercent
	{
		get => _maxLossPercent.Value;
		set => _maxLossPercent.Value = value;
	}

	public bool IsLongPosition
	{
		get => _isLongPosition.Value;
		set => _isLongPosition.Value = value;
	}

	public decimal? MaxLossValue => _maxLossValue;

	public decimal? TakeProfitValue => _takeProfitValue;

	public decimal? RiskRewardRatio => _riskRewardRatio;

	public decimal? SuggestedVolume => _suggestedVolume;

	public decimal? StopLossSteps => _stopLossSteps;

	public decimal? TakeProfitSteps => _takeProfitSteps;

	public VolumeCalculatorStrategy()
	{
		_stopLossPrice = Param(nameof(StopLossPrice), 0m)
			.SetDisplay("Stop Loss Price", "Price level of the stop loss.", "Risk")
			.SetCanOptimize(false);

		_takeProfitPrice = Param(nameof(TakeProfitPrice), 0m)
			.SetDisplay("Take Profit Price", "Price level of the take profit.", "Risk")
			.SetCanOptimize(false);

		_maxLossPercent = Param(nameof(MaxLossPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss %", "Maximum percentage of the portfolio to risk per trade.", "Risk");

		_isLongPosition = Param(nameof(IsLongPosition), true)
			.SetDisplay("Is Long Position", "Defines whether the planned trade is long (true) or short (false).", "General")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maxLossValue = null;
		_takeProfitValue = null;
		_riskRewardRatio = null;
		_suggestedVolume = null;
		_stopLossSteps = null;
		_takeProfitSteps = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		CalculateRiskMetrics();
	}

	private void CalculateRiskMetrics()
	{
		var portfolioValue = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;

		if (portfolioValue <= 0m)
		{
			LogWarning("Unable to calculate volume: portfolio value is not available.");
			return;
		}

		var priceStep = Security.PriceStep;
		var stepPrice = Security.StepPrice;

		if (priceStep == null || priceStep <= 0m)
		{
			LogWarning("Unable to calculate volume: security price step is not defined.");
			return;
		}

		if (stepPrice == null || stepPrice <= 0m)
		{
			LogWarning("Unable to calculate volume: security step price is not defined.");
			return;
		}

		var currentPrice = GetReferencePrice();

		if (currentPrice == null)
		{
			LogWarning("Unable to calculate volume: current price is not available.");
			return;
		}

		var stopLossDistance = IsLongPosition
			? currentPrice.Value - StopLossPrice
			: StopLossPrice - currentPrice.Value;

		if (stopLossDistance <= 0m)
		{
			LogWarning("Invalid stop loss configuration: price distance must be positive.");
			return;
		}

		var takeProfitDistance = IsLongPosition
			? TakeProfitPrice - currentPrice.Value
			: currentPrice.Value - TakeProfitPrice;

		if (takeProfitDistance <= 0m)
		{
			LogWarning("Invalid take profit configuration: price distance must be positive.");
			return;
		}

		_stopLossSteps = decimal.Ceiling(stopLossDistance / priceStep.Value);
		_takeProfitSteps = decimal.Ceiling(takeProfitDistance / priceStep.Value);

		if (_stopLossSteps <= 0m)
		{
			LogWarning("Invalid stop loss configuration: calculated steps are zero.");
			return;
		}

		_maxLossValue = portfolioValue * (MaxLossPercent / 100m);

		var lossPerStep = _maxLossValue / _stopLossSteps;

		_suggestedVolume = lossPerStep / stepPrice.Value;

		_takeProfitValue = _takeProfitSteps * stepPrice.Value * _suggestedVolume;

		_riskRewardRatio = _stopLossSteps == 0m ? null : _takeProfitSteps / _stopLossSteps;

		LogInfo("-------------- New Calculations for New Position --------------");
		LogInfo("Stop Loss steps = {0}", _stopLossSteps);
		LogInfo("Take Profit steps = {0}", _takeProfitSteps);
		LogInfo("Max Possible Loss Value = {0:F2}", _maxLossValue);
		LogInfo("Take Profit Value = {0:F2}", _takeProfitValue);
		LogInfo("Risk to Reward Ratio = {0:F2}", _riskRewardRatio);
		LogInfo("Allowed Volume = {0:F4}", _suggestedVolume);

		if (_riskRewardRatio >= 3m)
			LogInfo("You can trade.");
		else
			LogWarning("High risk position, do not trade.");
	}

	private decimal? GetReferencePrice()
	{
		var lastTradePrice = Security.LastTick?.Price;

		if (lastTradePrice != null)
			return lastTradePrice;

		var bestBid = Security.BestBid?.Price;
		var bestAsk = Security.BestAsk?.Price;

		if (bestBid != null && bestAsk != null)
			return (bestBid.Value + bestAsk.Value) / 2m;

		return bestAsk ?? bestBid ?? Security.LastPrice;
	}
}

