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
/// Hedge strategy simplified from HTH Trader. Trades based on daily close deviation.
/// </summary>
public class HthTraderStrategy : Strategy
{
	private readonly StrategyParam<bool> _tradeEnabled;
	private readonly StrategyParam<bool> _useProfitTarget;
	private readonly StrategyParam<bool> _useLossLimit;
	private readonly StrategyParam<int> _profitTargetPips;
	private readonly StrategyParam<int> _lossLimitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose1;
	private decimal _prevClose2;
	private decimal _entryPrice;
	private decimal _priceStep;

	/// <summary>
	/// Enable automated trading.
	/// </summary>
	public bool TradeEnabled
	{
		get => _tradeEnabled.Value;
		set => _tradeEnabled.Value = value;
	}

	/// <summary>
	/// Enable closing by reaching the profit target.
	/// </summary>
	public bool UseProfitTarget
	{
		get => _useProfitTarget.Value;
		set => _useProfitTarget.Value = value;
	}

	/// <summary>
	/// Enable closing by reaching the loss limit.
	/// </summary>
	public bool UseLossLimit
	{
		get => _useLossLimit.Value;
		set => _useLossLimit.Value = value;
	}

	/// <summary>
	/// Profit target in pips.
	/// </summary>
	public int ProfitTargetPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	/// <summary>
	/// Loss limit in pips.
	/// </summary>
	public int LossLimitPips
	{
		get => _lossLimitPips.Value;
		set => _lossLimitPips.Value = value;
	}

	/// <summary>
	/// Candle type for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public HthTraderStrategy()
	{
		_tradeEnabled = Param(nameof(TradeEnabled), true)
			.SetDisplay("Trade Enabled", "Allow the strategy to submit orders", "General");

		_useProfitTarget = Param(nameof(UseProfitTarget), true)
			.SetDisplay("Use Profit Target", "Close when profit target is reached", "Risk");

		_useLossLimit = Param(nameof(UseLossLimit), true)
			.SetDisplay("Use Loss Limit", "Close when loss limit is reached", "Risk");

		_profitTargetPips = Param(nameof(ProfitTargetPips), 80)
			.SetDisplay("Profit Target (pips)", "Profit target in pips", "Risk");

		_lossLimitPips = Param(nameof(LossLimitPips), 40)
			.SetDisplay("Loss Limit (pips)", "Loss limit in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for monitoring", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose1 = 0m;
		_prevClose2 = 0m;
		_entryPrice = 0m;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_priceStep = Security?.PriceStep ?? 0.0001m;
		if (_priceStep <= 0m)
			_priceStep = 0.0001m;

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed || !TradeEnabled)
			return;

		// Check exit conditions
		if (Position != 0 && _entryPrice > 0m)
		{
			var priceDiff = Position > 0
				? candle.ClosePrice - _entryPrice
				: _entryPrice - candle.ClosePrice;

			var pipsDiff = priceDiff / _priceStep;

			if (UseProfitTarget && pipsDiff >= ProfitTargetPips)
			{
				if (Position > 0) SellMarket(Math.Abs(Position));
				else BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				return;
			}

			if (UseLossLimit && pipsDiff <= -LossLimitPips)
			{
				if (Position > 0) SellMarket(Math.Abs(Position));
				else BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				return;
			}
		}

		// Entry logic based on daily close deviation
		if (Position == 0 && _prevClose1 > 0m && _prevClose2 > 0m)
		{
			var deviation = (100m * _prevClose1 / _prevClose2) - 100m;

			if (deviation > 0.1m)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (deviation < -0.1m)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevClose2 = _prevClose1;
		_prevClose1 = candle.ClosePrice;
	}
}
