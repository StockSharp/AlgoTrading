namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Closes profitable positions on Friday after the configured cut-off time.
/// Replicates the MetaTrader utility that secures profits before the weekend.
/// </summary>
public class CloseProfitEndOfWeekStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _startTradeTime;
	private readonly StrategyParam<TimeSpan> _endTradeTime;
	private readonly StrategyParam<bool> _closeTradesAtEndTime;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isCryptoAsset;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseProfitEndOfWeekStrategy()
	{
		_startTradeTime = Param(nameof(StartTradeTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Time of day when the monitoring window begins", "Schedule");

		_endTradeTime = Param(nameof(EndTradeTime), new TimeSpan(20, 0, 0))
			.SetDisplay("Cut-off Time", "All profitable positions are closed after this time on Fridays", "Schedule");

		_closeTradesAtEndTime = Param(nameof(CloseTradesAtEndTime), true)
			.SetDisplay("Close Trades", "Enable automatic closing of profitable positions at the weekly cut-off", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Data series used to evaluate the schedule", "Data");
	}

	/// <summary>
	/// Start of the monitoring window (unused but exposed for completeness).
	/// </summary>
	public TimeSpan StartTradeTime
	{
		get => _startTradeTime.Value;
		set => _startTradeTime.Value = value;
	}

	/// <summary>
	/// Cut-off time on Fridays when profitable positions must be closed.
	/// </summary>
	public TimeSpan EndTradeTime
	{
		get => _endTradeTime.Value;
		set => _endTradeTime.Value = value;
	}

	/// <summary>
	/// Enables or disables the automatic weekend protection routine.
	/// </summary>
	public bool CloseTradesAtEndTime
	{
		get => _closeTradesAtEndTime.Value;
		set => _closeTradesAtEndTime.Value = value;
	}

	/// <summary>
	/// Candle type used for time tracking.
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_isCryptoAsset = Security?.Type == SecurityTypes.CryptoCurrency;

		if (_isCryptoAsset)
		{
			LogInfo($"{Security?.Id} classified as crypto asset. Weekend closing routine disabled.");
			return;
		}

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!CloseTradesAtEndTime)
			return;

		if (_isCryptoAsset)
			return;

		var closeTime = candle.CloseTime;

		if (closeTime.DayOfWeek != DayOfWeek.Friday)
			return;

		if (closeTime.TimeOfDay < EndTradeTime)
			return;

		TryCloseProfitablePositions(closeTime);
	}

	private void TryCloseProfitablePositions(DateTimeOffset time)
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		foreach (var position in portfolio.Positions)
		{
			var security = position.Security ?? Security;
			if (security == null)
				continue;

			if (security != Security)
				continue;

			if (position.CurrentValue is not decimal volume || volume == 0m)
				continue;

			if (position.PnL is not decimal profit || profit <= 0m)
				continue;

			var side = volume > 0m ? Sides.Sell : Sides.Buy;
			if (HasActiveExitOrder(security, side))
				continue;

			if (volume > 0m)
				SellMarket(volume, security);
			else
				BuyMarket(-volume, security);

			LogInfo($"Closed profitable {security.Id} position {volume:0.####} at {time:yyyy-MM-dd HH:mm} with profit {profit:0.##}.");
		}
	}

	private bool HasActiveExitOrder(Security security, Sides side)
	{
		foreach (var order in Orders)
		{
			if (order.Security != security)
				continue;

			if (!order.State.IsActive())
				continue;

			if (order.Side == side)
				return true;
		}

		return false;
	}
}
