using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flawless Victory strategy.
/// Goes long when the close is below the lower Bollinger band with RSI under 30 and short when it is above the upper band
/// with RSI over 70; the opposite signal reverses the position. Version 2 adds percent take-profit and stop-loss exits,
/// and Version 3 additionally requires MFI under 20 for longs and over 80 for shorts.
/// </summary>
public class FlawlessVictoryStrategy : Strategy
{
	private const decimal _rsiOversold = 30m;
	private const decimal _rsiOverbought = 70m;
	private const decimal _mfiOversold = 20m;
	private const decimal _mfiOverbought = 80m;

	private readonly StrategyParam<int> _version;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _mfiLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Strategy version: 1, 2 or 3.
	/// </summary>
	public int Version
	{
		get => _version.Value;
		set => _version.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RSI_length
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// MFI period.
	/// </summary>
	public int MFI_length
	{
		get => _mfiLength.Value;
		set => _mfiLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit percentage used by version 2.
	/// </summary>
	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage used by version 2.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FlawlessVictoryStrategy()
	{
		_version = Param(nameof(Version), 1)
			.SetRange(1, 3)
			.SetDisplay("Version", "1: RSI signals, 2: adds take-profit/stop-loss, 3: adds MFI confirmation", "General");

		_rsiLength = Param(nameof(RSI_length), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_mfiLength = Param(nameof(MFI_length), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Length", "MFI period", "Indicators");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Indicators");

		_takeProfitPct = Param(nameof(TakeProfitPct), 1.5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage for version 2", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 1.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage for version 2", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};
		var rsi = new RelativeStrengthIndex { Length = RSI_length };
		var mfi = new MoneyFlowIndex { Length = MFI_length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, rsi, mfi, ProcessCandle)
			.Start();

		if (Version == 2)
		{
			StartProtection(
				TakeProfitPct > 0 ? new Unit(TakeProfitPct, UnitTypes.Percent) : new Unit(),
				StopLossPct > 0 ? new Unit(StopLossPct, UnitTypes.Percent) : new Unit(),
				useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, mfi);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !rsiValue.IsFormed || !mfiValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.ToDecimal();
		var mfi = mfiValue.ToDecimal();
		var close = candle.ClosePrice;
		var useMfi = Version == 3;

		var longSignal = close < lower && rsi < _rsiOversold && (!useMfi || mfi < _mfiOversold);
		var shortSignal = close > upper && rsi > _rsiOverbought && (!useMfi || mfi > _mfiOverbought);

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
