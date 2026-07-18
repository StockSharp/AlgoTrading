using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double RSI Strategy.
/// Uses a short RSI and a long RSI for confirmation.
/// Buys when both RSIs are oversold and sells when both are overbought.
/// </summary>
public class DoubleRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _rsiShortLength;
	private readonly StrategyParam<int> _rsiLongLength;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsiShort;
	private RelativeStrengthIndex _rsiLong;
	private int _cooldownRemaining;

	public DoubleRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_rsiShortLength = Param(nameof(RSIShortLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI", "Short RSI period", "RSI");

		_rsiLongLength = Param(nameof(RSILongLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI", "Long RSI period", "RSI");

		_oversold = Param(nameof(Oversold), 35m)
			.SetDisplay("Oversold", "RSI oversold level", "RSI");

		_overbought = Param(nameof(Overbought), 65m)
			.SetDisplay("Overbought", "RSI overbought level", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int RSIShortLength
	{
		get => _rsiShortLength.Value;
		set => _rsiShortLength.Value = value;
	}

	public int RSILongLength
	{
		get => _rsiLongLength.Value;
		set => _rsiLongLength.Value = value;
	}

	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsiShort = null;
		_rsiLong = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsiShort = new RelativeStrengthIndex { Length = RSIShortLength };
		_rsiLong = new RelativeStrengthIndex { Length = RSILongLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_rsiShort, _rsiLong, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal rsiShort, decimal rsiLong)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsiShort.IsFormed || !_rsiLong.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		// Buy: both RSIs oversold
		if (rsiShort < Oversold && rsiLong < Oversold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: both RSIs overbought
		else if (rsiShort > Overbought && rsiLong > Overbought && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: short RSI overbought
		else if (Position > 0 && rsiShort > Overbought)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: short RSI oversold
		else if (Position < 0 && rsiShort < Oversold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
