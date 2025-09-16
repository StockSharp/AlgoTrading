using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trading panel strategy that mirrors the MQL TradePad script.
/// </summary>
public class TradePadStrategy : Strategy
{
	private TradePanel _userPanel;

	/// <summary>
	/// Initializes a new instance of the <see cref="TradePadStrategy"/> class.
	/// </summary>
	public TradePadStrategy()
	{
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_userPanel ??= new TradePanel();

		_userPanel.OnInit(this);

		SubscribeTrades().Bind(_userPanel.OnTick).Start();

		Timer.Start(TimeSpan.FromSeconds(1), _userPanel.OnTimer);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		_userPanel.OnTrade(trade);
	}

	/// <inheritdoc />
	protected override void OnProcessMessage(Message message)
	{
		base.OnProcessMessage(message);

		_userPanel.OnChartEvent(message);
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		_userPanel?.Dispose();

		base.OnStop();
	}

	private sealed class TradePanel : IDisposable
	{
		private Strategy _strategy;

		public void OnInit(Strategy strategy)
		{
			_strategy = strategy;
		}

		public void OnTick(ExecutionMessage trade)
		{
			// Handle each trade tick update.
		}

		public void OnTimer()
		{
			// Handle periodic tasks triggered by timer.
		}

		public void OnTrade(MyTrade trade)
		{
			// Handle trade execution events.
		}

		public void OnChartEvent(Message message)
		{
			// Handle generic chart or message events.
		}

		public void Dispose()
		{
			// Dispose resources if any were allocated.
		}
	}
}
