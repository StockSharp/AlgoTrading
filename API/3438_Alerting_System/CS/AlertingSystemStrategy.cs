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
/// Alerting System Strategy - triggers notifications when bid or ask reaches configured levels.
/// </summary>
public class AlertingSystemStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperPrice;
	private readonly StrategyParam<decimal> _lowerPrice;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private bool _upperAlertActive;
	private bool _lowerAlertActive;

	/// <summary>
	/// Price level that triggers an alert when the best bid reaches or exceeds it.
	/// </summary>
	public decimal UpperPrice
	{
		get => _upperPrice.Value;
		set => _upperPrice.Value = value;
	}

	/// <summary>
	/// Price level that triggers an alert when the best ask reaches or falls below it.
	/// </summary>
	public decimal LowerPrice
	{
		get => _lowerPrice.Value;
		set => _lowerPrice.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AlertingSystemStrategy()
	{
		_upperPrice = Param(nameof(UpperPrice), 0m)
			.SetDisplay("Upper Price", "Bid alert activation price", "Alerts");

		_lowerPrice = Param(nameof(LowerPrice), 0m)
			.SetDisplay("Lower Price", "Ask alert activation price", "Alerts");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;

			if (bid > 0m)
				_bestBid = bid;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;

			if (ask > 0m)
				_bestAsk = ask;
		}

		CheckUpperAlert();
		CheckLowerAlert();
	}

	private void CheckUpperAlert()
	{
		var level = UpperPrice;
		if (level <= 0m)
		{
			_upperAlertActive = false;
			return;
		}

		if (_bestBid is not decimal bid)
			return;

		if (bid >= level)
		{
			if (!_upperAlertActive)
			{
				LogInfo("Upper alert triggered. Best bid={0:F5}, level={1:F5}", bid, level);
				_upperAlertActive = true;
			}
		}
		else
		{
			_upperAlertActive = false;
		}
	}

	private void CheckLowerAlert()
	{
		var level = LowerPrice;
		if (level <= 0m)
		{
			_lowerAlertActive = false;
			return;
		}

		if (_bestAsk is not decimal ask)
			return;

		if (ask <= level)
		{
			if (!_lowerAlertActive)
			{
				LogInfo("Lower alert triggered. Best ask={0:F5}, level={1:F5}", ask, level);
				_lowerAlertActive = true;
			}
		}
		else
		{
			_lowerAlertActive = false;
		}
	}
}

