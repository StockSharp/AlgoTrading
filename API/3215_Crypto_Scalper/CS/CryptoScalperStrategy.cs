namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class CryptoScalperStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _higherFastPeriod;
	private readonly StrategyParam<int> _higherSlowPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _momentumReference;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;

	private ICandleMessage? _previousCandle;
	private decimal? _previousMa;
	private decimal? _higherFastValue;
	private decimal? _higherSlowValue;
	private decimal? _momentumValue;
	private decimal? _macdHistogram;
	private decimal _entryPrice;
	private decimal _pipSize;
	private int _positionDirection;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int HigherFastPeriod
	{
		get => _higherFastPeriod.Value;
		set => _higherFastPeriod.Value = value;
	}

	public int HigherSlowPeriod
	{
		get => _higherSlowPeriod.Value;
		set => _higherSlowPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	public decimal MomentumReference
	{
		get => _momentumReference.Value;
		set => _momentumReference.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public CryptoScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Primary Candle", "Candle type used for trade detection", "Data");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Higher Candle", "Confirmation candle type", "Data");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 8)
			.SetDisplay("Fast LWMA", "Length of the primary linear weighted moving average", "Trend")
			.SetCanOptimize(true);

		_higherFastPeriod = Param(nameof(HigherFastPeriod), 6)
			.SetDisplay("Higher Fast MA", "Fast moving average length on the higher timeframe", "Trend")
			.SetCanOptimize(true);

		_higherSlowPeriod = Param(nameof(HigherSlowPeriod), 85)
			.SetDisplay("Higher Slow MA", "Slow moving average length on the higher timeframe", "Trend")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Momentum length applied on the higher timeframe", "Momentum")
			.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Minimum deviation from the reference momentum", "Momentum")
			.SetCanOptimize(true);

		_momentumReference = Param(nameof(MomentumReference), 100m)
			.SetDisplay("Momentum Reference", "Baseline momentum value used as 100 level in MetaTrader", "Momentum");

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Protective take profit distance expressed in pips", "Risk")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetDisplay("Volume", "Order volume in lots", "Trading")
			.SetGreaterThanZero();

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast", "Short EMA period inside the MACD", "Trend");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow", "Long EMA period inside the MACD", "Trend");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA period inside the MACD", "Trend");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherCandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = default;
		_previousMa = default;
		_higherFastValue = default;
		_higherSlowValue = default;
		_momentumValue = default;
		_macdHistogram = default;
		_entryPrice = default;
		_pipSize = default;
		_positionDirection = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		StartProtection();

		_pipSize = GetPipSize();

		var primaryMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
			.Bind(primaryMa, ProcessPrimaryCandle)
			.Start();

		var higherFast = new LinearWeightedMovingAverage { Length = HigherFastPeriod };
		var higherSlow = new LinearWeightedMovingAverage { Length = HigherSlowPeriod };
		var higherMomentum = new Momentum { Length = MomentumPeriod };
		var higherMacd = new Macd
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
			.Bind(higherFast, higherSlow, higherMomentum, higherMacd, ProcessHigherCandle)
			.Start();

		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			DrawCandles(chartArea, primarySubscription);
			DrawIndicator(chartArea, primaryMa, "Primary LWMA");
		}
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var positionSign = Math.Sign(Position);
		if (positionSign == 0)
		{
			_entryPrice = 0m;
			_positionDirection = 0;
			return;
		}

		if (_positionDirection != positionSign)
		{
			// Store entry price only when a fresh position is established.
			_entryPrice = trade.Trade?.Price ?? 0m;
			_positionDirection = positionSign;
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal higherFast, decimal higherSlow, decimal momentum, decimal macd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store last known confirmation values.
		_higherFastValue = higherFast;
		_higherSlowValue = higherSlow;
		_momentumValue = momentum;
		_macdHistogram = macd;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal primaryMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateProtectiveLevels(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			_previousMa = primaryMa;
			return;
		}

		if (_previousCandle == null || _previousMa == null)
		{
			_previousCandle = candle;
			_previousMa = primaryMa;
			return;
		}

		if (!IsConfirmationReady())
		{
			_previousCandle = candle;
			_previousMa = primaryMa;
			return;
		}

		if (Orders.Any(o => o.State == OrderStates.Active))
		{
			_previousCandle = candle;
			_previousMa = primaryMa;
			return;
		}

		var bullishCross = _previousCandle.LowPrice < _previousMa && _previousCandle.ClosePrice > _previousMa;
		var bearishCross = _previousCandle.HighPrice > _previousMa && _previousCandle.ClosePrice < _previousMa;
		var momentumDistance = Math.Abs((_momentumValue ?? 0m) - MomentumReference);
		var strongMomentum = momentumDistance >= MomentumThreshold;
		var bullishTrend = _higherFastValue > _higherSlowValue && (_macdHistogram ?? 0m) > 0m;
		var bearishTrend = _higherFastValue < _higherSlowValue && (_macdHistogram ?? 0m) < 0m;

		if (bullishCross && strongMomentum && bullishTrend && Position <= 0m)
		{
			BuyMarket(TradeVolume);
		}
		else if (bearishCross && strongMomentum && bearishTrend && Position >= 0m)
		{
			SellMarket(TradeVolume);
		}

		_previousCandle = candle;
		_previousMa = primaryMa;
	}

	private void UpdateProtectiveLevels(ICandleMessage candle)
	{
		if (_entryPrice == 0m || _pipSize <= 0m)
			return;

		if (Position > 0m)
		{
			var stopLoss = StopLossPips > 0 ? _entryPrice - StopLossPips * _pipSize : (decimal?)null;
			var takeProfit = TakeProfitPips > 0 ? _entryPrice + TakeProfitPips * _pipSize : (decimal?)null;

			if (stopLoss.HasValue && candle.LowPrice <= stopLoss.Value)
			{
				ClosePosition();
				return;
			}

			if (takeProfit.HasValue && candle.HighPrice >= takeProfit.Value)
			{
				ClosePosition();
			}
		}
		else if (Position < 0m)
		{
			var stopLoss = StopLossPips > 0 ? _entryPrice + StopLossPips * _pipSize : (decimal?)null;
			var takeProfit = TakeProfitPips > 0 ? _entryPrice - TakeProfitPips * _pipSize : (decimal?)null;

			if (stopLoss.HasValue && candle.HighPrice >= stopLoss.Value)
			{
				ClosePosition();
				return;
			}

			if (takeProfit.HasValue && candle.LowPrice <= takeProfit.Value)
			{
				ClosePosition();
			}
		}
	}

	private bool IsConfirmationReady()
	{
		return _higherFastValue.HasValue && _higherSlowValue.HasValue && _momentumValue.HasValue && _macdHistogram.HasValue;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep;
		if (step != null && step.Value > 0m)
			return step.Value;

		// Fallback for instruments without configured price step.
		return 0.0001m;
	}
}
