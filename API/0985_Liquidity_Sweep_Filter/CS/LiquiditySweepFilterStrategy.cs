using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Sweep Filter strategy based on Bollinger bands and volume.
/// </summary>
public class LiquiditySweepFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _majorSweepThreshold;
	private readonly StrategyParam<string> _tradeMode;

	private BollingerBands _bands;
	private Highest _highestVolume;
	private Lowest _lowestVolume;

	private int _trend;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Calculation length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Deviation multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Threshold for major sweeps based on normalized volume.
	/// </summary>
	public decimal MajorSweepThreshold
	{
		get => _majorSweepThreshold.Value;
		set => _majorSweepThreshold.Value = value;
	}

	/// <summary>
	/// Trade direction mode.
	/// </summary>
	public string TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LiquiditySweepFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Base period", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Band width multiplier", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_majorSweepThreshold = Param(nameof(MajorSweepThreshold), 50m)
			.SetRange(0m, 100m)
			.SetDisplay("Major Sweep Threshold", "Normalized volume threshold", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(25m, 75m, 5m);

		_tradeMode = Param(nameof(TradeMode), "Long Only")
			.SetDisplay("Trade Mode", "Allowed trade directions", "Trading")
			.SetOptions("Long & Short", "Long Only", "Short Only");
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

		_bands = new BollingerBands { Length = Length, Width = Multiplier };
		_highestVolume = new Highest { Length = (int)(Length * Multiplier) };
		_lowestVolume = new Lowest { Length = (int)(Length * Multiplier) };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bands, _highestVolume, _lowestVolume, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bands);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal highestVol, decimal lowestVol)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevTrend = _trend;

		if (candle.ClosePrice > upper)
			_trend = 1;
		else if (candle.ClosePrice < lower)
			_trend = -1;

		var volRange = highestVol - lowestVol;
		var nvol = volRange > 0m ? (candle.TotalVolume - lowestVol) * 100m / volRange : 0m;

		var bullishSweep = _trend > 0 && candle.LowPrice < lower && nvol > MajorSweepThreshold;
		var bearishSweep = _trend < 0 && candle.HighPrice > upper && nvol > MajorSweepThreshold;
		// Sweeps can be used for alerts or visualization if needed.

		if (prevTrend <= 0 && _trend > 0)
		{
			if (TradeMode == "Long & Short" || TradeMode == "Long Only")
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				if (Position <= 0)
					BuyMarket();
			}
			else if (TradeMode == "Short Only" && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
		else if (prevTrend >= 0 && _trend < 0)
		{
			if (TradeMode == "Long & Short" || TradeMode == "Short Only")
			{
				if (Position > 0)
					SellMarket(Position);

				if (Position >= 0)
					SellMarket();
			}
			else if (TradeMode == "Long Only" && Position > 0)
			{
				SellMarket(Position);
			}
		}
	}
}
