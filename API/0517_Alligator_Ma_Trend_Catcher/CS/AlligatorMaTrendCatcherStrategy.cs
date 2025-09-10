using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alligator + MA Trend Catcher strategy.
/// Buy when price is above the EMA trendline and Alligator lines are aligned up.
/// Sell short when price is below the trendline and Alligator lines are aligned down.
/// </summary>
public class AlligatorMaTrendCatcherStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _trendlineLength;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Jaw length.
	/// </summary>
	public int JawLength { get => _jawLength.Value; set => _jawLength.Value = value; }

	/// <summary>
	/// Teeth length.
	/// </summary>
	public int TeethLength { get => _teethLength.Value; set => _teethLength.Value = value; }

	/// <summary>
	/// Lips length.
	/// </summary>
	public int LipsLength { get => _lipsLength.Value; set => _lipsLength.Value = value; }

	/// <summary>
	/// EMA trendline length.
	/// </summary>
	public int TrendlineLength { get => _trendlineLength.Value; set => _trendlineLength.Value = value; }

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public AlligatorMaTrendCatcherStrategy()
	{
		_jawLength = Param(nameof(JawLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Length", "Length of the jaw smoothed moving average", "Alligator");

		_teethLength = Param(nameof(TeethLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Length", "Length of the teeth smoothed moving average", "Alligator");

		_lipsLength = Param(nameof(LipsLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lips Length", "Length of the lips smoothed moving average", "Alligator");

		_trendlineLength = Param(nameof(TrendlineLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of the EMA trendline", "Trendline");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

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

		var jaw = new SmoothedMovingAverage { Length = JawLength };
		var teeth = new SmoothedMovingAverage { Length = TeethLength };
		var lips = new SmoothedMovingAverage { Length = LipsLength };
		var trend = new ExponentialMovingAverage { Length = TrendlineLength };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ProcessCandle);

		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trend);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var median = (candle.HighPrice + candle.LowPrice) / 2m;

			var jawValue = jaw.Process(median);
			var teethValue = teeth.Process(median);
			var lipsValue = lips.Process(median);
			var trendValue = trend.Process(candle.ClosePrice);

			if (!jawValue.IsFinal || !teethValue.IsFinal || !lipsValue.IsFinal || !trendValue.IsFinal)
				return;

			var jawMa = jawValue.GetValue<decimal>();
			var teethMa = teethValue.GetValue<decimal>();
			var lipsMa = lipsValue.GetValue<decimal>();
			var trendline = trendValue.GetValue<decimal>();

			if (Position > 0 && candle.ClosePrice < trendline && lipsMa < teethMa && teethMa < jawMa)
			{
				SellMarket(Position);
			}
			else if (Position < 0 && candle.ClosePrice > trendline && lipsMa > teethMa && teethMa > jawMa)
			{
				BuyMarket(Math.Abs(Position));
			}
			else if (EnableLong && Position == 0 && candle.ClosePrice > trendline && lipsMa > teethMa && teethMa > jawMa)
			{
				BuyMarket(Volume);
			}
			else if (EnableShort && Position == 0 && candle.ClosePrice < trendline && lipsMa < teethMa && teethMa < jawMa)
			{
				SellMarket(Volume);
			}
		}
	}
}
