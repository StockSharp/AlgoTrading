using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the MetaTrader alert system by monitoring
/// best bid/ask quotes and raising journal notifications when
/// configurable horizontal levels are crossed.
/// </summary>
public class AlertingSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperPrice;
	private readonly StrategyParam<decimal> _lowerPrice;

	private bool _upperAlertTriggered;
	private bool _lowerAlertTriggered;

	/// <summary>
	/// Upper alert price. Set to zero to disable the check.
	/// </summary>
	public decimal UpperPrice
	{
		get => _upperPrice.Value;
		set => _upperPrice.Value = value;
	}

	/// <summary>
	/// Lower alert price. Set to zero to disable the check.
	/// </summary>
	public decimal LowerPrice
	{
		get => _lowerPrice.Value;
		set => _lowerPrice.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AlertingSystemStrategy"/>.
	/// </summary>
	public AlertingSystemStrategy()
	{
		_upperPrice = Param(nameof(UpperPrice), 0m)
			.SetDisplay("Upper Price", "Upper horizontal level that triggers an alert.", "Alerts");

		_lowerPrice = Param(nameof(LowerPrice), 0m)
			.SetDisplay("Lower Price", "Lower horizontal level that triggers an alert.", "Alerts");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_upperAlertTriggered = false;
		_lowerAlertTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to bid/ask updates to mirror the original OnTick loop.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		LogConfiguredLevels();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;
			if (bid > 0m)
				CheckUpperAlert(bid);
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;
			if (ask > 0m)
				CheckLowerAlert(ask);
		}
	}

	private void CheckUpperAlert(decimal bid)
	{
		var level = UpperPrice;
		if (level <= 0m)
		{
			_upperAlertTriggered = false;
			return;
		}

		if (bid >= level)
		{
			if (!_upperAlertTriggered)
			{
				AddInfoLog($"Bid {bid:0.#####} crossed the upper alert level {level:0.#####}.");
				_upperAlertTriggered = true;
			}
		}
		else if (_upperAlertTriggered)
		{
			// Reset the flag once the market moves back below the level.
			_upperAlertTriggered = false;
		}
	}

	private void CheckLowerAlert(decimal ask)
	{
		var level = LowerPrice;
		if (level <= 0m)
		{
			_lowerAlertTriggered = false;
			return;
		}

		if (ask <= level)
		{
			if (!_lowerAlertTriggered)
			{
				AddInfoLog($"Ask {ask:0.#####} crossed the lower alert level {level:0.#####}.");
				_lowerAlertTriggered = true;
			}
		}
		else if (_lowerAlertTriggered)
		{
			// Reset the flag once the market moves back above the level.
			_lowerAlertTriggered = false;
		}
	}

	private void LogConfiguredLevels()
	{
		var upper = UpperPrice;
		var lower = LowerPrice;

		if (upper > 0m)
			AddInfoLog($"Upper alert level configured at {upper:0.#####}.");
		else
			AddInfoLog("Upper alert level disabled (value is 0).");

		if (lower > 0m)
			AddInfoLog($"Lower alert level configured at {lower:0.#####}.");
		else
			AddInfoLog("Lower alert level disabled (value is 0).");
	}
}
