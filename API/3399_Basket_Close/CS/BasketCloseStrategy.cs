namespace StockSharp.Samples.Strategies;

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
public class BasketCloseStrategy : Strategy
{
	private readonly StrategyParam<BasketCloseLossModes> _lossMode;
	private readonly StrategyParam<decimal> _lossPercentage;
	private readonly StrategyParam<decimal> _lossCurrency;
	private readonly StrategyParam<BasketCloseProfitModes> _profitMode;
	private readonly StrategyParam<decimal> _profitPercentage;
	private readonly StrategyParam<decimal> _profitCurrency;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableTestOrders;
	private readonly StrategyParam<decimal> _testOrderVolume;

	private bool _lossTriggered;
	private bool _profitTriggered;
	private bool _testOrderSubmitted;
	private decimal _initialBalance;

	public BasketCloseStrategy()
	{
		_lossMode = Param(nameof(LossMode), BasketCloseLossModes.Percentage)
			.SetDisplay("Loss Mode", "Determines whether the loss threshold is evaluated in percent or currency.", "General");

		_lossPercentage = Param(nameof(LossPercentage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Loss Percentage", "Close all positions once floating loss reaches this percentage.", "Risk");

		_lossCurrency = Param(nameof(LossCurrency), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Loss Currency", "Close all positions once floating loss reaches this amount.", "Risk");

		_profitMode = Param(nameof(ProfitMode), BasketCloseProfitModes.Percentage)
			.SetDisplay("Profit Mode", "Determines whether the profit target is evaluated in percent or currency.", "General");

		_profitPercentage = Param(nameof(ProfitPercentage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Percentage", "Close all positions once floating profit reaches this percentage.", "Risk");

		_profitCurrency = Param(nameof(ProfitCurrency), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Currency", "Close all positions once floating profit reaches this amount.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for periodic basket checks.", "Data");

		_enableTestOrders = Param(nameof(EnableTestOrders), false)
			.SetDisplay("Enable Test Orders", "Automatically place a market order when no positions are open.", "Testing");

		_testOrderVolume = Param(nameof(TestOrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Test Order Volume", "Default size for the optional test order.", "Testing");
	}

	public BasketCloseLossModes LossMode
	{
		get => _lossMode.Value;
		set => _lossMode.Value = value;
	}

	public decimal LossPercentage
	{
		get => _lossPercentage.Value;
		set => _lossPercentage.Value = value;
	}

	public decimal LossCurrency
	{
		get => _lossCurrency.Value;
		set => _lossCurrency.Value = value;
	}

	public BasketCloseProfitModes ProfitMode
	{
		get => _profitMode.Value;
		set => _profitMode.Value = value;
	}

	public decimal ProfitPercentage
	{
		get => _profitPercentage.Value;
		set => _profitPercentage.Value = value;
	}

	public decimal ProfitCurrency
	{
		get => _profitCurrency.Value;
		set => _profitCurrency.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool EnableTestOrders
	{
		get => _enableTestOrders.Value;
		set => _enableTestOrders.Value = value;
	}

	public decimal TestOrderVolume
	{
		get => _testOrderVolume.Value;
		set => _testOrderVolume.Value = value;
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

		_lossTriggered = false;
		_profitTriggered = false;
		_testOrderSubmitted = false;
		_initialBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TestOrderVolume;
		_initialBalance = GetAccountBalance();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var totalProfit = CalculateTotalProfit();
		var balance = GetAccountBalance();
		var referenceBalance = balance > 0m ? balance : _initialBalance;
		var percentage = referenceBalance > 0m ? totalProfit / referenceBalance * 100m : 0m;

		if (!_lossTriggered && ShouldTriggerLoss(totalProfit, percentage))
		{
			_lossTriggered = true;
			LogInfo($"Loss threshold reached. Floating PnL={totalProfit:0.##}; Percentage={percentage:0.##}%.");
		}

		if (!_profitTriggered && ShouldTriggerProfit(totalProfit, percentage))
		{
			_profitTriggered = true;
			LogInfo($"Profit target reached. Floating PnL={totalProfit:0.##}; Percentage={percentage:0.##}%.");
		}

		if (_lossTriggered)
		{
			CloseAllPositions();
			if (!HasAnyOpenPosition())
			{
				_lossTriggered = false;
				_testOrderSubmitted = false;
			}
		}

		if (_profitTriggered)
		{
			CloseAllPositions();
			if (!HasAnyOpenPosition())
			{
				_profitTriggered = false;
				_testOrderSubmitted = false;
			}
		}

		if (EnableTestOrders)
		{
			if (HasAnyOpenPosition())
			{
				_testOrderSubmitted = false;
			}
			else if (!_testOrderSubmitted)
			{
				SendTestOrder();
				_testOrderSubmitted = true;
			}
		}
		else
		{
			_testOrderSubmitted = false;
		}
	}

	private bool ShouldTriggerLoss(decimal totalProfit, decimal percentage)
	{
		return LossMode switch
		{
			BasketCloseLossModes.Percentage => percentage <= -LossPercentage,
			BasketCloseLossModes.Currency => totalProfit <= -LossCurrency,
			_ => false,
		};
	}

	private bool ShouldTriggerProfit(decimal totalProfit, decimal percentage)
	{
		return ProfitMode switch
		{
			BasketCloseProfitModes.Percentage => percentage >= ProfitPercentage,
			BasketCloseProfitModes.Currency => totalProfit >= ProfitCurrency,
			_ => false,
		};
	}

	private decimal CalculateTotalProfit()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		if (portfolio.CurrentProfit is decimal currentProfit)
			return currentProfit;

		decimal total = 0m;
		foreach (var position in portfolio.Positions)
		{
			total += position.PnL ?? 0m;
		}

		return total;
	}

	private void CloseAllPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var securities = new HashSet<Security>();

		if (Security != null)
			securities.Add(Security);

		foreach (var strategyPosition in Positions)
		{
			if (strategyPosition.Security != null)
				securities.Add(strategyPosition.Security);
		}

		foreach (var position in portfolio.Positions)
		{
			if (position.Security != null)
				securities.Add(position.Security);
		}

		foreach (var security in securities)
		{
			var volume = GetPositionValue(security, portfolio) ?? 0m;
			if (volume > 0m)
			{
				SellMarket(volume, security);
			}
			else if (volume < 0m)
			{
				BuyMarket(-volume, security);
			}
		}
	}

	private bool HasAnyOpenPosition()
	{
		if (Position != 0m)
			return true;

		var portfolio = Portfolio;
		if (portfolio == null)
			return false;

		foreach (var position in portfolio.Positions)
		{
			if ((position.CurrentValue ?? 0m) != 0m)
				return true;
		}

		return false;
	}

	private decimal GetAccountBalance()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return _initialBalance;

		var balance = portfolio.BeginValue ?? portfolio.CurrentValue ?? 0m;
		if (balance <= 0m && portfolio.CurrentValue is decimal currentValue && currentValue > 0m)
			balance = currentValue;

		if (balance > 0m && _initialBalance <= 0m)
			_initialBalance = balance;

		return balance > 0m ? balance : _initialBalance;
	}

	private void SendTestOrder()
	{
		var volume = TestOrderVolume;
		if (volume <= 0m)
			return;

		BuyMarket(volume);
	}

	public enum BasketCloseLossModes
	{
		Percentage,
		Currency,
	}

	public enum BasketCloseProfitModes
	{
		Percentage,
		Currency,
	}
}
