using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive RSI strategy with dynamic OB/OS levels based on ATR or VWAP deviation.
/// </summary>
public class ArsiVwapAtrStrategy : Strategy
{
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

	private decimal? _prevRsi;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Base K coefficient.
	/// </summary>
	public decimal BaseK { get => _baseK.Value; set => _baseK.Value = value; }

	/// <summary>
	/// Risk percent of equity.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Risk reward multiplier.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// Source for overbought level (ATR or VWAP).
	/// </summary>
	public string SourceOb { get => _sourceOb.Value; set => _sourceOb.Value = value; }

	/// <summary>
	/// Source for oversold level (ATR or VWAP).
	/// </summary>
	public string SourceOs { get => _sourceOs.Value; set => _sourceOs.Value = value; }

	/// <summary>
	/// ATR length for OB calculation.
	/// </summary>
	public int AtrLengthOb { get => _atrLengthOb.Value; set => _atrLengthOb.Value = value; }

	/// <summary>
	/// ATR length for OS calculation.
	/// </summary>
	public int AtrLengthOs { get => _atrLengthOs.Value; set => _atrLengthOs.Value = value; }

	/// <summary>
	/// Multiplier for OB line.
	/// </summary>
	public decimal ObMultiplier { get => _obMultiplier.Value; set => _obMultiplier.Value = value; }

	/// <summary>
	/// Multiplier for OS line.
	/// </summary>
	public decimal OsMultiplier { get => _osMultiplier.Value; set => _osMultiplier.Value = value; }

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="ArsiVwapAtrStrategy"/>.
	/// </summary>
	public ArsiVwapAtrStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators");

		_baseK = Param(nameof(BaseK), 1m)
			.SetDisplay("Base K", "Base coefficient", "Indicators");

		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetDisplay("Risk %", "Risk percent of equity", "Risk")
			.SetGreaterThanZero();

		_stopLossPercent = Param(nameof(StopLossPercent), 2.5m)
			.SetDisplay("SL %", "Stop loss percent", "Risk")
			.SetGreaterThanZero();

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("RR", "Risk reward multiplier", "Risk")
			.SetGreaterThanZero();

		_sourceOb = Param(nameof(SourceOb), "ATR")
			.SetDisplay("OB Source", "Source for OB line (ATR/VWAP)", "Indicators");

		_sourceOs = Param(nameof(SourceOs), "ATR")
			.SetDisplay("OS Source", "Source for OS line (ATR/VWAP)", "Indicators");

		_atrLengthOb = Param(nameof(AtrLengthOb), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR OB", "ATR length for OB", "Indicators");

		_atrLengthOs = Param(nameof(AtrLengthOs), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR OS", "ATR length for OS", "Indicators");

		_obMultiplier = Param(nameof(ObMultiplier), 10m)
			.SetDisplay("OB Mult", "OB line multiplier", "Indicators");

		_osMultiplier = Param(nameof(OsMultiplier), 10m)
			.SetDisplay("OS Mult", "OS line multiplier", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var vwap = new VolumeWeightedMovingAverage();
		var atrOb = new AverageTrueRange { Length = AtrLengthOb };
		var atrOs = new AverageTrueRange { Length = AtrLengthOs };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwap, rsi, atrOb, atrOs, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			takeProfit: new Unit(StopLossPercent * RiskReward, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal rsiValue, decimal atrObValue, decimal atrOsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var volOb = SourceOb == "ATR" ? atrObValue / close * 100m : Math.Abs(close - vwapValue) / close * 100m;
		var volOs = SourceOs == "ATR" ? atrOsValue / close * 100m : Math.Abs(close - vwapValue) / close * 100m;

		var kOb = BaseK * Math.Max(volOb / 5m, 0.5m);
		var kOs = BaseK * Math.Max(volOs / 5m, 0.5m);

		var ob = 50m + kOb * ObMultiplier;
		var os = 50m - kOs * OsMultiplier;

		if (_prevRsi != null)
		{
			var longIn = _prevRsi < os && rsiValue >= os;
			var shortIn = _prevRsi > ob && rsiValue <= ob;
			var exitLong = (_prevRsi > 50m && rsiValue <= 50m) || (_prevRsi < ob && rsiValue >= ob);
			var exitShort = (_prevRsi < 50m && rsiValue >= 50m) || (_prevRsi > os && rsiValue <= os);

			if (longIn && Position <= 0)
			{
				var volume = CalculateQty(close);
				BuyMarket(volume);
			}
			else if (shortIn && Position >= 0)
			{
				var volume = CalculateQty(close);
				SellMarket(volume);
			}
			else if (exitLong && Position > 0)
			{
				SellMarket(Position);
			}
			else if (exitShort && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		_prevRsi = rsiValue;
	}

	private decimal CalculateQty(decimal price)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskValue = equity * RiskPercent / 100m;
		var stopDist = price * StopLossPercent / 100m;
		return stopDist > 0m ? riskValue / stopDist : 0m;
	}
}

