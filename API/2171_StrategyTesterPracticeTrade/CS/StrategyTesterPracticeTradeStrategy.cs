using System;
using System.Collections.Generic;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that executes market orders based on external command files.
/// </summary>
public class StrategyTesterPracticeTradeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<string> _commandDir;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Volume for each market order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Directory where command files are placed.
	/// </summary>
	public string CommandDir
	{
		get => _commandDir.Value;
		set => _commandDir.Value = value;
	}

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StrategyTesterPracticeTradeStrategy"/>.
	/// </summary>
	public StrategyTesterPracticeTradeStrategy()
	{
		_lotSize = Param(nameof(LotSize), 1m)
			.SetDisplay("Lot Size", "Volume for market orders", "General")
			.SetCanOptimize(true);

		_commandDir = Param(nameof(CommandDir), Path.GetTempPath())
			.SetDisplay("Command Directory", "Folder containing command files", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		CheckCommands();
	}

	private void CheckCommands()
	{
		var buyFile = Path.Combine(CommandDir, "buy.txt");
		var sellFile = Path.Combine(CommandDir, "sell.txt");
		var closeFile = Path.Combine(CommandDir, "close.txt");

		if (File.Exists(buyFile))
		{
			DeleteFiles(buyFile, sellFile, closeFile);
			ExecuteBuy();
		}
		else if (File.Exists(sellFile))
		{
			DeleteFiles(buyFile, sellFile, closeFile);
			ExecuteSell();
		}
		else if (File.Exists(closeFile))
		{
			DeleteFiles(buyFile, sellFile, closeFile);
			ClosePosition();
		}
	}

	private void ExecuteBuy()
	{
		var volume = LotSize;

		if (Position < 0)
			volume += Math.Abs(Position);

		BuyMarket(volume);
	}

	private void ExecuteSell()
	{
		var volume = LotSize;

		if (Position > 0)
			volume += Position;

		SellMarket(volume);
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	private static void DeleteFiles(params string[] files)
	{
		foreach (var file in files)
		{
			if (File.Exists(file))
				File.Delete(file);
		}
	}
}
