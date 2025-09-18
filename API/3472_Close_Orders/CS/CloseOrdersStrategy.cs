namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class CloseOrdersStrategy : Strategy
{
	private static readonly PropertyInfo? StrategyIdProperty = typeof(Position).GetProperty("StrategyId");

	private readonly StrategyParam<decimal> _targetProfitMoney;
	private readonly StrategyParam<decimal> _cutLossMoney;
	private readonly StrategyParam<string> _magicNumber;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _normalizedCutLoss;
	private bool _liquidationRequested;

	public CloseOrdersStrategy()
	{
		_targetProfitMoney = Param(nameof(TargetProfitMoney), 10m)
			.SetDisplay("Target Profit", "Floating profit (money) required to close matching orders", "General")
			.SetCanOptimize(true);

		_cutLossMoney = Param(nameof(CutLossMoney), 0m)
			.SetDisplay("Cut Loss", "Floating loss (money) that triggers forced liquidation", "General")
			.SetCanOptimize(true);

		_magicNumber = Param(nameof(MagicNumber), string.Empty)
			.SetDisplay("Magic Number", "Strategy identifier to match (empty = all)", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for periodic profit checks", "Data");
	}

	public decimal TargetProfitMoney
	{
		get => _targetProfitMoney.Value;
		set => _targetProfitMoney.Value = value;
	}

	public decimal CutLossMoney
	{
		get => _cutLossMoney.Value;
		set => _cutLossMoney.Value = value;
	}

	public string MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

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
	protected override void OnReseted()
	{
		base.OnReseted();

		_normalizedCutLoss = 0m;
		_liquidationRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TargetProfitMoney <= 0m)
			throw new InvalidOperationException("Target profit must be greater than zero.");

		_normalizedCutLoss = CutLossMoney == 0m ? 0m : -Math.Abs(CutLossMoney);
		_liquidationRequested = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to match the tick-based trigger from the MQL version.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EvaluateFloatingProfit();
	}

	private void EvaluateFloatingProfit()
	{
		var profit = CalculateFloatingProfit();

		if (profit >= TargetProfitMoney)
		{
			LogInfo($"Floating profit {profit:0.##} reached target {TargetProfitMoney:0.##}. Closing matching orders.");
			_liquidationRequested = true;
		}
		else if (_normalizedCutLoss < 0m && profit <= _normalizedCutLoss)
		{
			LogInfo($"Floating loss {profit:0.##} breached limit {_normalizedCutLoss:0.##}. Closing matching orders.");
			_liquidationRequested = true;
		}

		if (_liquidationRequested)
			ExecuteLiquidation();
	}

	private decimal CalculateFloatingProfit()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		var magic = MagicNumber;
		var filterEnabled = !string.IsNullOrEmpty(magic);

		if (!filterEnabled && portfolio.CurrentProfit is decimal currentProfit)
			return currentProfit;

		decimal total = 0m;

		foreach (var position in portfolio.Positions)
		{
			if (filterEnabled && !MatchesMagicNumber(position, magic))
				continue;

			total += position.PnL ?? 0m;
		}

		return total;
	}

	private void ExecuteLiquidation()
	{
		CancelMatchingOrders();
		CloseMatchingPositions();

		if (!HasMatchingPositions())
			_liquidationRequested = false;
	}

	private void CancelMatchingOrders()
	{
		var magic = MagicNumber;
		var filterEnabled = !string.IsNullOrEmpty(magic);

		var snapshot = new List<Order>(ActiveOrders);
		foreach (var order in snapshot)
		{
			if (filterEnabled && !string.Equals(order.UserOrderId, magic, StringComparison.Ordinal))
				continue;

			CancelOrder(order);
		}
	}

	private void CloseMatchingPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var magic = MagicNumber;
		var filterEnabled = !string.IsNullOrEmpty(magic);

		foreach (var position in portfolio.Positions)
		{
			if (filterEnabled && !MatchesMagicNumber(position, magic))
				continue;

			var security = position.Security;
			if (security == null)
				continue;

			var volume = position.CurrentValue ?? 0m;
			if (volume > 0m)
			{
				// Send a sell market order to exit long exposure.
				SellMarket(volume, security);
			}
			else if (volume < 0m)
			{
				// Send a buy market order to exit short exposure.
				BuyMarket(-volume, security);
			}
		}
	}

	private bool HasMatchingPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return false;

		var magic = MagicNumber;
		var filterEnabled = !string.IsNullOrEmpty(magic);

		foreach (var position in portfolio.Positions)
		{
			if (filterEnabled && !MatchesMagicNumber(position, magic))
				continue;

			var volume = position.CurrentValue ?? 0m;
			if (volume != 0m)
				return true;
		}

		return false;
	}

	private static bool MatchesMagicNumber(Position position, string magicNumber)
	{
		if (string.IsNullOrEmpty(magicNumber))
			return true;

		if (StrategyIdProperty == null)
			return true;

		var value = StrategyIdProperty.GetValue(position);
		return value != null && string.Equals(value.ToString(), magicNumber, StringComparison.Ordinal);
	}
}