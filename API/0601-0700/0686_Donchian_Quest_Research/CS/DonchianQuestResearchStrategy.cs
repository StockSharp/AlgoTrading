using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Quest Research strategy.
/// A close above the upper band of the OpenPeriod Donchian channel goes long and a close below its lower band goes short,
/// reversing an opposite position. A long closes when price touches the lower band of the ClosePeriod channel and a short
/// when price touches its upper band. Both channels are measured on the candles before the current one.
/// </summary>
public class DonchianQuestResearchStrategy : Strategy
{
	private readonly StrategyParam<int> _openPeriod;
	private readonly StrategyParam<int> _closePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOpenUpper;
	private decimal? _prevOpenLower;
	private decimal? _prevCloseUpper;
	private decimal? _prevCloseLower;

	/// <summary>
	/// Period of the entry channel.
	/// </summary>
	public int OpenPeriod
	{
		get => _openPeriod.Value;
		set => _openPeriod.Value = value;
	}

	/// <summary>
	/// Period of the exit channel.
	/// </summary>
	public int ClosePeriod
	{
		get => _closePeriod.Value;
		set => _closePeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DonchianQuestResearchStrategy()
	{
		_openPeriod = Param(nameof(OpenPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Open Period", "Period of the entry Donchian channel", "Indicators");

		_closePeriod = Param(nameof(ClosePeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Close Period", "Period of the exit Donchian channel", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevOpenUpper = null;
		_prevOpenLower = null;
		_prevCloseUpper = null;
		_prevCloseLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var openChannel = new DonchianChannels { Length = OpenPeriod };
		var closeChannel = new DonchianChannels { Length = ClosePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(openChannel, closeChannel, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, openChannel);
			DrawIndicator(area, closeChannel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue openValue, IIndicatorValue closeValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openUpper = _prevOpenUpper;
		var openLower = _prevOpenLower;
		var closeUpper = _prevCloseUpper;
		var closeLower = _prevCloseLower;

		if (openValue.IsFormed && openValue is IDonchianChannelsValue { UpperBand: decimal ou, LowerBand: decimal ol })
		{
			_prevOpenUpper = ou;
			_prevOpenLower = ol;
		}

		if (closeValue.IsFormed && closeValue is IDonchianChannelsValue { UpperBand: decimal cu, LowerBand: decimal cl })
		{
			_prevCloseUpper = cu;
			_prevCloseLower = cl;
		}

		if (openUpper is not decimal entryHigh || openLower is not decimal entryLow || closeUpper is not decimal exitHigh || closeLower is not decimal exitLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (close > entryHigh && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < entryLow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && candle.LowPrice <= exitLow)
			SellMarket(Position);
		else if (Position < 0 && candle.HighPrice >= exitHigh)
			BuyMarket(-Position);
	}
}
