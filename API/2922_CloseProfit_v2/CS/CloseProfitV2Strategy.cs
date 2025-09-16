using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk management helper that closes all positions when floating profit or loss limits are reached.
/// </summary>
public class CloseProfitV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<decimal> _lossClose;
	private readonly StrategyParam<bool> _allSymbols;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastClosePrice;
	private decimal _flatEquity;
	private bool _closeAllRequested;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseProfitV2Strategy()
	{
		_profitClose = Param(nameof(ProfitClose), 10m)
			.SetDisplay("Profit Close", "Floating profit target that triggers liquidation", "Risk")
			.SetCanOptimize(true);

		_lossClose = Param(nameof(LossClose), 1000m)
			.SetDisplay("Loss Close", "Floating loss threshold that triggers liquidation", "Risk")
			.SetCanOptimize(true);

		_allSymbols = Param(nameof(AllSymbols), false)
			.SetDisplay("All Symbols", "Track every strategy security instead of only the primary one", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used to evaluate floating profit", "Data");
	}

	/// <summary>
	/// Floating profit target for closing positions.
	/// </summary>
	public decimal ProfitClose
	{
		get => _profitClose.Value;
		set => _profitClose.Value = value;
	}

	/// <summary>
	/// Floating loss threshold for closing positions.
	/// </summary>
	public decimal LossClose
	{
		get => _lossClose.Value;
		set => _lossClose.Value = value;
	}

	/// <summary>
	/// Enables tracking of every security traded by the strategy.
	/// </summary>
	public bool AllSymbols
	{
		get => _allSymbols.Value;
		set => _allSymbols.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor floating profit updates.
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
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastClosePrice = null;
		_flatEquity = 0m;
		_closeAllRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		UpdateFlatEquity();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for floating PnL calculations.
		_lastClosePrice = candle.ClosePrice;

		if (!HasOpenPositions())
		{
			_closeAllRequested = false;
			UpdateFlatEquity();
			return;
		}

		if (!_closeAllRequested)
		{
			var profit = CalculateFloatingProfit();

			if (ProfitClose > 0m && profit >= ProfitClose)
			{
				LogInfo($"Profit target reached at {profit:0.##}.");
				_closeAllRequested = true;
			}
			else if (LossClose > 0m && profit <= -LossClose)
			{
				LogInfo($"Loss limit reached at {profit:0.##}.");
				_closeAllRequested = true;
			}
		}

		if (_closeAllRequested)
			ExecuteCloseAll();
	}

	private decimal CalculateFloatingProfit()
	{
		if (AllSymbols)
		{
			var currentValue = Portfolio?.CurrentValue;
			if (!currentValue.HasValue)
				return 0m;

			// Portfolio value is compared with the last flat equity snapshot.
			return currentValue.Value - _flatEquity;
		}

		if (_lastClosePrice is null || Position == 0m)
			return 0m;

		var averagePrice = Position.AveragePrice;
		// Floating profit is calculated from the average fill price.
		return Position * (_lastClosePrice.Value - averagePrice);
	}

	private void ExecuteCloseAll()
	{
		// Cancel active orders before liquidating positions.
		CancelActiveOrders();

		foreach (var security in GetActiveSecurities())
		{
			var volume = GetPositionValue(security, Portfolio) ?? 0m;

			if (volume > 0m)
			{
				// Exit long exposure on the specified security.
				SellMarket(volume, security);
			}
			else if (volume < 0m)
			{
				// Exit short exposure on the specified security.
				BuyMarket(Math.Abs(volume), security);
			}
		}

		if (!HasOpenPositions())
		{
			_closeAllRequested = false;
			UpdateFlatEquity();
		}
	}

	private IEnumerable<Security> GetActiveSecurities()
	{
		if (!AllSymbols)
		{
			if (Security is not null)
				yield return Security;
			yield break;
		}

		var seen = new HashSet<Security>();

		foreach (var position in Positions)
		{
			var security = position.Security;
			if (security is null || !seen.Add(security))
				continue;

			var volume = GetPositionValue(security, Portfolio) ?? 0m;
			if (volume != 0m)
				yield return security;
		}
	}

	private bool HasOpenPositions()
	{
		if (!AllSymbols)
			return Position != 0m;

		foreach (var position in Positions)
		{
			var security = position.Security;
			if (security is null)
				continue;

			var volume = GetPositionValue(security, Portfolio) ?? 0m;
			if (volume != 0m)
				return true;
		}

		return false;
	}

	private void UpdateFlatEquity()
	{
		var currentValue = Portfolio?.CurrentValue;
		if (currentValue.HasValue)
			_flatEquity = currentValue.Value;
	}
}
