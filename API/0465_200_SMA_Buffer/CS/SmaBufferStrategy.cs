namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 200 SMA Buffer Strategy.
/// Buys when price is above SMA by entry percent.
/// Sells when price falls below SMA by exit percent.
/// Also supports short positions.
/// </summary>
public class SmaBufferStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _entryPercent;
	private readonly StrategyParam<decimal> _exitPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _sma;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public decimal EntryPercent
	{
		get => _entryPercent.Value;
		set => _entryPercent.Value = value;
	}

	public decimal ExitPercent
	{
		get => _exitPercent.Value;
		set => _exitPercent.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public SmaBufferStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_smaLength = Param(nameof(SmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of the moving average", "Parameters");

		_entryPercent = Param(nameof(EntryPercent), 2m)
			.SetDisplay("Entry %", "Percent above/below SMA to enter", "Parameters");

		_exitPercent = Param(nameof(ExitPercent), 1m)
			.SetDisplay("Exit %", "Percent toward SMA to exit", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var price = candle.ClosePrice;
		var upperEntry = smaValue * (1m + EntryPercent / 100m);
		var lowerEntry = smaValue * (1m - EntryPercent / 100m);
		var upperExit = smaValue * (1m + ExitPercent / 100m);
		var lowerExit = smaValue * (1m - ExitPercent / 100m);

		// Buy: price above upper entry threshold
		if (price > upperEntry && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price below lower entry threshold
		else if (price < lowerEntry && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price drops below lower exit
		else if (Position > 0 && price < lowerExit)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price rises above upper exit
		else if (Position < 0 && price > upperExit)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
