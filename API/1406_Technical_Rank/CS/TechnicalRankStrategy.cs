using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on composite technical rank across multiple indicators.
/// Opens long when rank exceeds upper threshold and short when below lower threshold.
/// </summary>
public class TechnicalRankStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SMA _sma200 = new() { Length = 200 };
	private readonly SMA _sma50 = new() { Length = 50 };
	private readonly RateOfChange _roc125 = new() { Length = 125 };
	private readonly RateOfChange _roc20 = new() { Length = 20 };
	private readonly EMA _ema12 = new() { Length = 12 };
	private readonly EMA _ema26 = new() { Length = 26 };
	private readonly EMA _ppoSignal = new() { Length = 9 };
	private readonly RSI _rsi = new() { Length = 14 };

	private readonly Queue<decimal> _ppoHist = new();

	/// <summary>
	/// Upper rank threshold.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Lower rank threshold.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
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
	/// Initializes a new instance of <see cref="TechnicalRankStrategy"/>.
	/// </summary>
	public TechnicalRankStrategy()
	{
		_upperThreshold = Param(nameof(UpperThreshold), 70m)
			.SetDisplay("Upper Threshold", "Technical rank above this opens long", "Parameters");

		_lowerThreshold = Param(nameof(LowerThreshold), 30m)
			.SetDisplay("Lower Threshold", "Technical rank below this opens short", "Parameters");

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

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma50);
			DrawIndicator(area, _sma200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var ma200 = _sma200.Process(candle).ToDecimal();
		var ma50 = _sma50.Process(candle).ToDecimal();
		var roc125 = _roc125.Process(candle).ToDecimal();
		var roc20 = _roc20.Process(candle).ToDecimal();
		var ema12 = _ema12.Process(candle).ToDecimal();
		var ema26 = _ema26.Process(candle).ToDecimal();
		var rsiValue = _rsi.Process(candle).ToDecimal();

		if (!_sma200.IsFormed || !_sma50.IsFormed || !_roc125.IsFormed || !_roc20.IsFormed || !_ema12.IsFormed || !_ema26.IsFormed || !_rsi.IsFormed)
			return;

		var ppo = ema26 == 0 ? 0m : 100m * (ema12 - ema26) / ema26;
		var ppoSig = _ppoSignal.Process(ppo, candle.OpenTime, true).ToDecimal();
		var ppoHist = ppo - ppoSig;

		_ppoHist.Enqueue(ppoHist);
		if (_ppoHist.Count < 9)
			return;
		var prev8 = _ppoHist.Dequeue();
		var slope = (ppoHist - prev8) / 3m;
		var stPpo = 0.05m * 100m * slope;
		var stRsi = 0.05m * rsiValue;

		var longTermMa = ma200 == 0 ? 0m : 0.30m * 100m * (close - ma200) / ma200;
		var longTermRoc = 0.30m * roc125;
		var midTermMa = ma50 == 0 ? 0m : 0.15m * 100m * (close - ma50) / ma50;
		var midTermRoc = 0.15m * roc20;

		var rank = longTermMa + longTermRoc + midTermMa + midTermRoc + stPpo + stRsi;
		rank = Math.Min(100m, Math.Max(0m, rank));

		if (rank > UpperThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (rank < LowerThreshold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
