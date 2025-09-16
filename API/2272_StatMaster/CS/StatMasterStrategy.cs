using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum SavePeriod
{
	EveryMinute,
	EveryHour,
	EveryDay
}

/// <summary>
/// Strategy that records best bid/ask prices and spread into a CSV file.
/// </summary>
public class StatMasterStrategy : Strategy
{
	private readonly StrategyParam<SavePeriod> _savePeriod;

	private decimal _lastBid;
	private int _lastMinute;
	private int _lastHour;
	private int _lastDay;
	private readonly StringBuilder _log = new("DATE;TIME;ASK;BID;SPREAD" + Environment.NewLine);
	private int _ticksSaved;

	/// <summary>
	/// Period for saving log data.
	/// </summary>
	public SavePeriod SavePeriod
	{
		get => _savePeriod.Value;
		set => _savePeriod.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="StatMasterStrategy"/>.
	/// </summary>
	public StatMasterStrategy()
	{
		_savePeriod = Param(nameof(SavePeriod), StockSharp.Samples.Strategies.SavePeriod.EveryMinute)
			.SetDisplay("Save Period", "Frequency to write log to CSV", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.MarketDepth)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastBid = 0;
		_lastMinute = 0;
		_lastHour = 0;
		_lastDay = 0;
		_ticksSaved = 0;
		_log.Clear();
		_log.Append("DATE;TIME;ASK;BID;SPREAD" + Environment.NewLine);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
	}

	private void ProcessOrderBook(IOrderBookMessage depth)
	{
		var bestBid = depth.Bids != null && depth.Bids.Length > 0 ? depth.Bids[0].Price : (decimal?)null;
		var bestAsk = depth.Asks != null && depth.Asks.Length > 0 ? depth.Asks[0].Price : (decimal?)null;

		if (bestBid == null || bestAsk == null)
			return;

		var time = depth.ServerTime;
		var bid = bestBid.Value;
		var ask = bestAsk.Value;

		if (_lastBid != bid)
		{
			var spread = ask - bid;
			_log.AppendLine($"{time:yyyy-MM-dd};{time:HH:mm:ss};{ask:0.#####};{bid:0.#####};{spread:0.#####}");
			_ticksSaved++;
			_lastBid = bid;
		}

		var needFlush = SavePeriod switch
		{
			SavePeriod.EveryMinute => time.Minute != _lastMinute,
			SavePeriod.EveryHour => time.Hour != _lastHour,
			SavePeriod.EveryDay => time.Day != _lastDay,
			_ => false
		};

		if (needFlush)
			FlushLog();

		_lastMinute = time.Minute;
		_lastHour = time.Hour;
		_lastDay = time.Day;
	}

	private void FlushLog()
	{
		var fileName = $"StatMaster_{Security.Id}.csv";
		File.WriteAllText(fileName, _log.ToString());
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		FlushLog();
		base.OnStopped();
	}
}
