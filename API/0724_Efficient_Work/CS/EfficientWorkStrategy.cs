using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe moving average strategy.
/// Buys when the fast MA is above medium and high MAs.
/// Sells when the fast MA is below both longer MAs.
/// </summary>
public class EfficientWorkStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _mediumMultiplier;
	private readonly StrategyParam<int> _highMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Base moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Medium timeframe multiplier.
	/// </summary>
	public int MediumMultiplier
	{
		get => _mediumMultiplier.Value;
		set => _mediumMultiplier.Value = value;
	}

	/// <summary>
	/// High timeframe multiplier.
	/// </summary>
	public int HighMultiplier
	{
		get => _highMultiplier.Value;
		set => _highMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EfficientWorkStrategy"/> class.
	/// </summary>
	public EfficientWorkStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Base moving average period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_mediumMultiplier = Param(nameof(MediumMultiplier), 5)
			.SetGreaterThanZero()
			.SetDisplay("Medium TF Multiplier", "Multiplier for medium timeframe MA", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_highMultiplier = Param(nameof(HighMultiplier), 10)
			.SetGreaterThanZero()
			.SetDisplay("High TF Multiplier", "Multiplier for high timeframe MA", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var fastMa = new SMA { Length = MaPeriod };
		var mediumMa = new SMA { Length = MaPeriod * MediumMultiplier };
		var highMa = new SMA { Length = MaPeriod * HighMultiplier };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastMa, mediumMa, highMa, (candle, fastValue, mediumValue, highValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!fastMa.IsFormed || !mediumMa.IsFormed || !highMa.IsFormed)
					return;

				var fastAbove = fastValue > mediumValue && fastValue > highValue;
				var fastBelow = fastValue < mediumValue && fastValue < highValue;

				if (fastAbove && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (fastBelow && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, mediumMa);
			DrawIndicator(area, highMa);
			DrawOwnTrades(area);
		}
	}
}
