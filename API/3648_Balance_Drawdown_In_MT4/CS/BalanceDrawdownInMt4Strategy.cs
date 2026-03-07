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
/// Replicates the BalanceDrawdownInMT4 expert advisor: opens a single long position and tracks drawdown from the peak balance.
/// </summary>
public class BalanceDrawdownInMt4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _startBalance;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _entryCooldownDays;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _maxBalance;
	private decimal _lastDrawdown;
	private decimal _lastPrice;
	private DateTime _lastEntryDate;

	/// <summary>
	/// Balance used as the baseline for drawdown calculations.
	/// </summary>
	public decimal StartBalance
	{
		get => _startBalance.Value;
		set => _startBalance.Value = value;
	}


	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimum number of days between new entries.
	/// </summary>
	public int EntryCooldownDays
	{
		get => _entryCooldownDays.Value;
		set => _entryCooldownDays.Value = value;
	}

	/// <summary>
	/// Timeframe used to trigger periodic drawdown updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Current drawdown percentage relative to the peak balance.
	/// </summary>
	public decimal DrawdownPercent => _lastDrawdown;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BalanceDrawdownInMt4Strategy()
	{
		_startBalance = Param(nameof(StartBalance), 1000m)
			.SetDisplay("Start Balance", "Initial balance for drawdown measurement.", "Risk")
			;


		_stopLossPoints = Param(nameof(StopLossPoints), 300m)
			.SetDisplay("Stop-Loss (points)", "Distance from entry price to the protective stop.", "Risk")
			;

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetDisplay("Take-Profit (points)", "Distance from entry price to the profit target.", "Risk")
			;

		_entryCooldownDays = Param(nameof(EntryCooldownDays), 5)
			.SetGreaterThanZero()
			.SetDisplay("Entry Cooldown Days", "Minimum number of days between new long entries.", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe that drives drawdown monitoring.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maxBalance = 0m;
		_lastDrawdown = 0m;
		_lastPrice = 0m;
		_lastEntryDate = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(
			stopLoss: new Unit(StopLossPoints * GetPriceStep(), UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfitPoints * GetPriceStep(), UnitTypes.Absolute));

		_maxBalance = StartBalance;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrice = candle.ClosePrice;

		EnsurePosition(candle.CloseTime);
		UpdateDrawdown();
	}

	private void EnsurePosition(DateTime candleDate)
	{
		if (Position != 0m)
			return;

		if (_lastEntryDate != default && (candleDate.Date - _lastEntryDate.Date).TotalDays < EntryCooldownDays)
			return;

		if (Volume <= 0m)
		{
			LogWarning("Volume parameter must be positive to open the initial trade.");
			return;
		}

		BuyMarket(Volume);
		_lastEntryDate = candleDate.Date;
	}

	private void UpdateDrawdown()
	{
		var balanceWithoutFloating = StartBalance + PnL;
		if (balanceWithoutFloating > _maxBalance)
			_maxBalance = balanceWithoutFloating;

		if (_maxBalance <= 0m)
		{
			_lastDrawdown = 0m;
			return;
		}

		var unrealized = CalculateUnrealizedPnL(_lastPrice);
		var currentBalance = balanceWithoutFloating + unrealized;

		var drawdown = (_maxBalance - currentBalance) / _maxBalance * 100m;
		_lastDrawdown = drawdown > 0m ? drawdown : 0m;

		LogInfo($"Current drawdown: {_lastDrawdown:F2}%.");
	}

	private decimal CalculateUnrealizedPnL(decimal price)
	{
		if (Position == 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		var stepPrice = GetSecurityValue<decimal?>(Level1Fields.StepPrice) ?? 0m;
		if (step <= 0m || stepPrice <= 0m)
			return 0m;

		var priceDiff = price - _lastPrice;
		var points = priceDiff / step;

		return points * stepPrice * Position;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}
}
