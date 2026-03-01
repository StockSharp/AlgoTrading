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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mirrors trades by opening an opposite hedge position with trailing stop management.
/// Simplified from the EES Hedger expert advisor.
/// </summary>
public class EesHedgerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _hedgeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;

	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal _pipSize;

	/// <summary>
	/// Hedge position volume.
	/// </summary>
	public decimal HedgeVolume
	{
		get => _hedgeVolume.Value;
		set => _hedgeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum step between trailing stop updates in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EesHedgerStrategy()
	{
		_hedgeVolume = Param(nameof(HedgeVolume), 0.1m)
			.SetDisplay("Hedge Volume", "Volume used for hedge orders", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance per hedge", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Take-profit distance per hedge", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 25)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimum trailing stop increment", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = CalculatePipSize();

		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var price = trade.Price;

		if (price <= 0m)
			return;

		// Entry: if no position, open based on tick direction
		if (Position == 0 && _entryPrice == 0m)
		{
			var volume = HedgeVolume > 0m ? HedgeVolume : Volume;
			if (volume <= 0m)
				return;

			BuyMarket(volume);
			_entryPrice = price;
			_stopPrice = null;
			return;
		}

		if (Position != 0 && _entryPrice == 0m)
			_entryPrice = price;

		// Check stop loss
		if (Position != 0 && StopLossPips > 0 && _pipSize > 0m)
		{
			var stopDistance = StopLossPips * _pipSize;

			if (Position > 0 && price <= _entryPrice - stopDistance)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0m;
				_stopPrice = null;
				return;
			}
			else if (Position < 0 && price >= _entryPrice + stopDistance)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				_stopPrice = null;
				return;
			}
		}

		// Check take profit
		if (Position != 0 && TakeProfitPips > 0 && _pipSize > 0m)
		{
			var takeDistance = TakeProfitPips * _pipSize;

			if (Position > 0 && price >= _entryPrice + takeDistance)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = 0m;
				_stopPrice = null;
				return;
			}
			else if (Position < 0 && price <= _entryPrice - takeDistance)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				_stopPrice = null;
				return;
			}
		}

		// Trailing stop
		if (Position != 0 && TrailingStopPips > 0 && _pipSize > 0m)
		{
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			if (Position > 0)
			{
				var newStop = price - trailingDistance;
				if (newStop > _entryPrice && (!_stopPrice.HasValue || newStop > _stopPrice.Value + trailingStep))
					_stopPrice = newStop;

				if (_stopPrice.HasValue && price <= _stopPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					_entryPrice = 0m;
					_stopPrice = null;
				}
			}
			else if (Position < 0)
			{
				var newStop = price + trailingDistance;
				if (newStop < _entryPrice && (!_stopPrice.HasValue || newStop < _stopPrice.Value - trailingStep))
					_stopPrice = newStop;

				if (_stopPrice.HasValue && price >= _stopPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = 0m;
					_stopPrice = null;
				}
			}
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var text = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
		var index = text.IndexOf('.');
		return index >= 0 ? text.Length - index - 1 : 0;
	}
}
