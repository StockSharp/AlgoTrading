using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR with Bulls/Bears Power filter.
/// Sells when fast EMA is below slow EMA, SAR is above high and Bears Power rises while negative.
/// Buys when fast EMA is above slow EMA, SAR is below low and Bulls Power falls while positive.
/// Applies a dynamic free margin threshold before trading and uses fixed stop-loss and take-profit.
/// </summary>
public class EmaSarPowerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _powerLength;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _minMargin;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBears;
	private decimal _prevBulls;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Parabolic SAR step.
	/// </summary>
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }

	/// <summary>
	/// Parabolic SAR maximum step.
	/// </summary>
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }

	/// <summary>
	/// Bulls/Bears Power period.
	/// </summary>
	public int PowerLength { get => _powerLength.Value; set => _powerLength.Value = value; }

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Minimum free margin required to trade.
	/// </summary>
	public decimal MinMargin { get => _minMargin.Value; set => _minMargin.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes <see cref="EmaSarPowerStrategy"/>.
	/// </summary>
	public EmaSarPowerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero();

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero();

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero();

		_powerLength = Param(nameof(PowerLength), 13)
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 2000m)
			.SetGreaterThanZero();

		_minMargin = Param(nameof(MinMargin), 600m)
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		Volume = 30;
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

		_prevBears = 0m;
		_prevBulls = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage
		{
			Length = FastLength,
			CandlePrice = CandlePrice.Median,
		};

		var slowEma = new ExponentialMovingAverage
		{
			Length = SlowLength,
			CandlePrice = CandlePrice.Median,
		};

		var sar = new ParabolicStopAndReverse
		{
			Step = SarStep,
			MaxStep = SarMax,
		};

		var bears = new BearsPower { Length = PowerLength };
		var bulls = new BullsPower { Length = PowerLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, sar, bears, bulls, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, sar);
			DrawIndicator(area, bears);
			DrawIndicator(area, bulls);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossPoints * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal sar, decimal bears, decimal bulls)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.Hour;
		if (hour <= 8 || hour >= 17)
			return;

		var margin = Portfolio?.CurrentValue ?? 0m;
		var threshold = MinMargin;

		if (margin > 1000m && margin < 1300m)
			threshold = 1000m;
		else if (margin >= 1300m && margin < 1600m)
			threshold = 1300m;
		else if (margin >= 1600m && margin < 1900m)
			threshold = 1500m;
		else if (margin >= 1900m && margin < 2100m)
			threshold = 1800m;
		else if (margin >= 2100m && margin < 2500m)
			threshold = 2000m;
		else if (margin >= 2500m && margin < 3000m)
			threshold = 2500m;

		if (margin < threshold)
			return;

		var prevBears = _prevBears;
		var prevBulls = _prevBulls;
		_prevBears = bears;
		_prevBulls = bulls;

		if (fast < slow && sar > candle.HighPrice && bears < 0m && bears > prevBears)
		{
			if (Position <= 0)
				SellMarket(Volume);
		}
		else if (fast > slow && sar < candle.LowPrice && bulls > 0m && bulls < prevBulls)
		{
			if (Position >= 0)
				BuyMarket(Volume);
		}
	}
}
