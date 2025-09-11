using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with RSI and volume confirmation. Exits via ATR-based stop and take profit.
/// </summary>
public class MultiIndicatorTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private decimal _prevFast;
	private decimal _prevSlow;

	/// <summary>
	/// Length of the fast EMA.
	/// </summary>
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }

	/// <summary>
	/// Length of the slow EMA.
	/// </summary>
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Volume SMA period.
	/// </summary>
	public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }

	/// <summary>
	/// Volume confirmation multiplier.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Stop loss multiplier based on ATR.
	/// </summary>
	public decimal StopLossAtrMultiplier { get => _stopLossAtrMultiplier.Value; set => _stopLossAtrMultiplier.Value = value; }

	/// <summary>
	/// Take profit multiplier based on ATR.
	/// </summary>
	public decimal TakeProfitAtrMultiplier { get => _takeProfitAtrMultiplier.Value; set => _takeProfitAtrMultiplier.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiIndicatorTrendFollowingStrategy"/> class.
	/// </summary>
	public MultiIndicatorTrendFollowingStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of the fast EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowMaLength = Param(nameof(SlowMaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of the slow EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 10);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Lookback period for RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Volume SMA period", "Volume")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Volume confirmation multiplier", "Volume")
			.SetCanOptimize(true)
			.SetOptimize(1m, 2m, 0.25m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for exits", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss ATR Multiplier", "ATR multiple for stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit ATR Multiplier", "ATR multiple for take profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
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

		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
		_prevFast = 0;
		_prevSlow = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastMaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal rsi, decimal atr)
	{
		var volumeMa = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

		if (candle.State != CandleStates.Finished)
			return;

		if (!_volumeSma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = _prevFast <= _prevSlow && fastMa > slowMa;
		var crossDown = _prevFast >= _prevSlow && fastMa < slowMa;

		_prevFast = fastMa;
		_prevSlow = slowMa;

		var highVolume = candle.TotalVolume > volumeMa * VolumeMultiplier;

		if (crossUp && rsi > 50 && highVolume && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossDown && rsi < 50 && highVolume && Position >= 0)
		{
			SellMarket();
		}

		if (Position > 0)
		{
			var stop = PositionPrice - atr * StopLossAtrMultiplier;
			var take = PositionPrice + atr * TakeProfitAtrMultiplier;

			if (candle.LowPrice <= stop || candle.ClosePrice <= stop)
				SellMarket(Position);
			else if (candle.HighPrice >= take || candle.ClosePrice >= take)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var stop = PositionPrice + atr * StopLossAtrMultiplier;
			var take = PositionPrice - atr * TakeProfitAtrMultiplier;

			if (candle.HighPrice >= stop || candle.ClosePrice >= stop)
				BuyMarket(-Position);
			else if (candle.LowPrice <= take || candle.ClosePrice <= take)
				BuyMarket(-Position);
		}
	}
}
