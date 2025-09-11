using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class PortfolioTrackerV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useCash;
	private readonly StrategyParam<decimal> _cash;

	private sealed class PositionEntry
	{
		public StrategyParam<bool> Enabled { get; init; }
		public StrategyParam<string> Symbol { get; init; }
		public StrategyParam<decimal> Quantity { get; init; }
		public StrategyParam<decimal> Cost { get; init; }
		public Security? Security { get; set; }
		public decimal LastPrice;
	}

	private readonly List<PositionEntry> _positions = new();

	private decimal _totalPortfolio;
	private decimal _totalPnL;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public bool UseCash
	{
		get => _useCash.Value;
		set => _useCash.Value = value;
	}

	public decimal Cash
	{
		get => _cash.Value;
		set => _cash.Value = value;
	}

	public PortfolioTrackerV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_useCash = Param(nameof(UseCash), true)
			.SetDisplay("Use Cash", "Include cash in portfolio", "Cash");

		_cash = Param(nameof(Cash), 10000m)
			.SetDisplay("Cash Amount", "Initial cash balance", "Cash");

		_positions.Add(CreatePosition(1, true, "MSFT", 1000m, 100m));
		_positions.Add(CreatePosition(2, true, "AAPL", 1000m, 100m));
		_positions.Add(CreatePosition(3, true, "INTC", 1000m, 40m));
		_positions.Add(CreatePosition(4, false, "TWTR", 100m, 50m));
		_positions.Add(CreatePosition(5, false, "FB", 100m, 100m));
		_positions.Add(CreatePosition(6, false, "MSFT", 100m, 100m));
		_positions.Add(CreatePosition(7, false, "MSFT", 100m, 100m));
		_positions.Add(CreatePosition(8, false, "MSFT", 100m, 100m));
		_positions.Add(CreatePosition(9, false, "MSFT", 100m, 100m));
		_positions.Add(CreatePosition(10, false, "MSFT", 100m, 100m));
	}

	private PositionEntry CreatePosition(int index, bool enabled, string symbol, decimal qty, decimal cost)
	{
		var en = Param($"Enable{index}", enabled)
			.SetDisplay($"Pos #{index}", $"Enable position #{index}", $"Position {index}");

		var sym = Param($"Symbol{index}", symbol)
			.SetDisplay($"Symbol #{index}", $"Ticker for position #{index}", $"Position {index}");

		var q = Param($"Quantity{index}", qty)
			.SetDisplay($"Qty #{index}", $"Quantity for position #{index}", $"Position {index}");

		var c = Param($"Cost{index}", cost)
			.SetDisplay($"Cost #{index}", $"Cost per share for position #{index}", $"Position {index}");

		return new PositionEntry
		{
				Enabled = en,
				Symbol = sym,
				Quantity = q,
				Cost = c
		};
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var p in _positions)
		{
				if (!p.Enabled.Value || p.Security == null)
						continue;
				yield return (p.Security, CandleType);
		}
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		foreach (var p in _positions)
				p.LastPrice = 0m;
		_totalPortfolio = 0m;
		_totalPnL = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var p in _positions)
		{
				if (!p.Enabled.Value)
						continue;

				p.Security = SecurityProvider.LookupById(p.Symbol.Value);
				if (p.Security == null)
						throw new InvalidOperationException($"Security '{p.Symbol.Value}' not found.");

				SubscribeCandles(CandleType, true, p.Security)
						.Bind(c => ProcessCandle(c, p))
						.Start();
		}

		UpdateTotals();
	}

	private void ProcessCandle(ICandleMessage candle, PositionEntry p)
	{
		if (candle.State != CandleStates.Finished)
				return;

		p.LastPrice = candle.ClosePrice;
		UpdateTotals();
	}

	private void UpdateTotals()
	{
		var totalValue = UseCash ? Cash : 0m;
		var totalCost = 0m;

		foreach (var p in _positions)
		{
				if (!p.Enabled.Value)
						continue;

				var value = p.LastPrice * p.Quantity.Value;
				var cost = p.Cost.Value * p.Quantity.Value;

				totalValue += value;
				totalCost += cost;
		}

		var pnl = totalValue - totalCost;

		_totalPortfolio = totalValue;
		_totalPnL = pnl;

		LogInfo($"Total portfolio {totalValue:F2}; PnL {pnl:F2}");
	}
}
