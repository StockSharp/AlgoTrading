using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive RSI strategy with ATR or VWAP driven levels.
/// The overbought line sits at 50 + 20 * BaseK and the oversold line at 50 - 20 * BaseK, each pushed further out by its
/// multiplier times a volatility measure in percent of price: ATR / close for the "ATR" source, or the distance of the
/// close from the daily VWAP for the "VWAP" source. RSI crossing above the oversold line goes long and crossing below the
/// overbought line goes short, reversing an opposite position. A long closes when RSI crosses above 50 and a short when it
/// crosses below 50. A StopLossPercent stop and a StopLossPercent * RiskReward take-profit protect every position.
/// </summary>
public class ArsiVwapAtrStrategy : Strategy
{
	private const decimal _baseBand = 20m;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _baseK;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<string> _sourceOb;
	private readonly StrategyParam<string> _sourceOs;
	private readonly StrategyParam<int> _atrLengthOb;
	private readonly StrategyParam<int> _atrLengthOs;
	private readonly StrategyParam<decimal> _obMultiplier;
	private readonly StrategyParam<decimal> _osMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _vwapDay;
	private decimal _cumPriceVolume;
	private decimal _cumVolume;
	private decimal? _prevRsi;
	private decimal _prevOb;
	private decimal _prevOs;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Scale of the base distance of the levels from 50.
	/// </summary>
	public decimal BaseK
	{
		get => _baseK.Value;
		set => _baseK.Value = value;
	}

	/// <summary>
	/// Percent of equity risked per trade (informational).
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit to stop-loss ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Volatility source of the overbought line: "ATR" or "VWAP".
	/// </summary>
	public string SourceOb
	{
		get => _sourceOb.Value;
		set => _sourceOb.Value = value;
	}

	/// <summary>
	/// Volatility source of the oversold line: "ATR" or "VWAP".
	/// </summary>
	public string SourceOs
	{
		get => _sourceOs.Value;
		set => _sourceOs.Value = value;
	}

	/// <summary>
	/// ATR period of the overbought line.
	/// </summary>
	public int AtrLengthOb
	{
		get => _atrLengthOb.Value;
		set => _atrLengthOb.Value = value;
	}

	/// <summary>
	/// ATR period of the oversold line.
	/// </summary>
	public int AtrLengthOs
	{
		get => _atrLengthOs.Value;
		set => _atrLengthOs.Value = value;
	}

	/// <summary>
	/// Multiplier of the overbought adjustment.
	/// </summary>
	public decimal ObMultiplier
	{
		get => _obMultiplier.Value;
		set => _obMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier of the oversold adjustment.
	/// </summary>
	public decimal OsMultiplier
	{
		get => _osMultiplier.Value;
		set => _osMultiplier.Value = value;
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
	public ArsiVwapAtrStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_baseK = Param(nameof(BaseK), 1m)
			.SetNotNegative()
			.SetDisplay("Base K", "Scale of the base distance of the levels from 50", "Levels");

		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Risk %", "Percent of equity risked per trade", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetNotNegative()
			.SetDisplay("Risk Reward", "Take-profit to stop-loss ratio", "Risk");

		_sourceOb = Param(nameof(SourceOb), "ATR")
			.SetDisplay("OB Source", "Volatility source of the overbought line: ATR or VWAP", "Levels");

		_sourceOs = Param(nameof(SourceOs), "ATR")
			.SetDisplay("OS Source", "Volatility source of the oversold line: ATR or VWAP", "Levels");

		_atrLengthOb = Param(nameof(AtrLengthOb), 14)
			.SetGreaterThanZero()
			.SetDisplay("OB ATR Length", "ATR period of the overbought line", "Levels");

		_atrLengthOs = Param(nameof(AtrLengthOs), 14)
			.SetGreaterThanZero()
			.SetDisplay("OS ATR Length", "ATR period of the oversold line", "Levels");

		_obMultiplier = Param(nameof(ObMultiplier), 10m)
			.SetNotNegative()
			.SetDisplay("OB Multiplier", "Multiplier of the overbought adjustment", "Levels");

		_osMultiplier = Param(nameof(OsMultiplier), 10m)
			.SetNotNegative()
			.SetDisplay("OS Multiplier", "Multiplier of the oversold adjustment", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_vwapDay = default;
		_cumPriceVolume = 0m;
		_cumVolume = 0m;
		_prevRsi = null;
		_prevOb = 0m;
		_prevOs = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atrOb = new AverageTrueRange { Length = AtrLengthOb };
		var atrOs = new AverageTrueRange { Length = AtrLengthOs };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, atrOb, atrOs, ProcessCandle)
			.Start();

		var stop = StopLossPercent;
		var take = StopLossPercent * RiskReward;
		StartProtection(
			take > 0 ? new Unit(take, UnitTypes.Percent) : new Unit(),
			stop > 0 ? new Unit(stop, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal atrOb, decimal atrOs)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Daily VWAP of the typical price, restarted at each UTC day.
		var day = candle.OpenTime.Date;
		if (day != _vwapDay)
		{
			_vwapDay = day;
			_cumPriceVolume = 0m;
			_cumVolume = 0m;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_cumPriceVolume += typical * candle.TotalVolume;
		_cumVolume += candle.TotalVolume;

		var close = candle.ClosePrice;
		var vwap = _cumVolume > 0m ? _cumPriceVolume / _cumVolume : close;

		var ob = Math.Min(100m, 50m + _baseBand * BaseK + ObMultiplier * Deviation(SourceOb, atrOb, close, vwap));
		var os = Math.Max(0m, 50m - _baseBand * BaseK - OsMultiplier * Deviation(SourceOs, atrOs, close, vwap));

		var prevRsi = _prevRsi;
		var prevOb = _prevOb;
		var prevOs = _prevOs;
		_prevRsi = rsi;
		_prevOb = ob;
		_prevOs = os;

		if (prevRsi is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longSignal = prev <= prevOs && rsi > os;
		var shortSignal = prev >= prevOb && rsi < ob;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && prev <= 50m && rsi > 50m)
			SellMarket(Position);
		else if (Position < 0 && prev >= 50m && rsi < 50m)
			BuyMarket(-Position);
	}

	private static decimal Deviation(string source, decimal atr, decimal close, decimal vwap)
	{
		if (close <= 0m)
			return 0m;

		return source.EqualsIgnoreCase("VWAP")
			? Math.Abs(close - vwap) / close * 100m
			: atr / close * 100m;
	}
}
