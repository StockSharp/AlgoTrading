using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
/// <summary>
/// Escort Trend strategy combining WMA crossover with MACD and CCI confirmation.
/// </summary>
public class EscortTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastWmaPeriod;
	private readonly StrategyParam<int> _slowWmaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<DataType> _candleType;
	/// <summary>
	/// Fast weighted moving average period.
	/// </summary>
	public int FastWmaPeriod { get => _fastWmaPeriod.Value; set => _fastWmaPeriod.Value = value; }
	/// <summary>
	/// Slow weighted moving average period.
	/// </summary>
	public int SlowWmaPeriod { get => _slowWmaPeriod.Value; set => _slowWmaPeriod.Value = value; }
	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	/// <summary>
	/// Threshold value for CCI signals.
	/// </summary>
	public decimal CciThreshold { get => _cciThreshold.Value; set => _cciThreshold.Value = value; }
	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	/// <summary>
	/// Step for trailing stop adjustment.
	/// </summary>
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	/// <summary>
	/// Initializes a new instance of the strategy with default parameters.
	/// </summary>
	public EscortTrendStrategy()
	{
		_fastWmaPeriod = Param(nameof(FastWmaPeriod), 8)
			.SetDisplay("Fast WMA", "Length of fast weighted MA", "General")
			.SetCanOptimize(true);
		_slowWmaPeriod = Param(nameof(SlowWmaPeriod), 18)
			.SetDisplay("Slow WMA", "Length of slow weighted MA", "General")
			.SetCanOptimize(true);
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "CCI calculation period", "General")
			.SetCanOptimize(true);
		_cciThreshold = Param(nameof(CciThreshold), 100m)
			.SetDisplay("CCI Threshold", "Threshold for CCI signal", "General")
			.SetCanOptimize(true);
		_macdFast = Param(nameof(MacdFast), 8)
			.SetDisplay("MACD Fast EMA", "Fast EMA period for MACD", "MACD")
			.SetCanOptimize(true);
		_macdSlow = Param(nameof(MacdSlow), 18)
			.SetDisplay("MACD Slow EMA", "Slow EMA period for MACD", "MACD")
			.SetCanOptimize(true);
		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");
		_stopLoss = Param(nameof(StopLoss), 55m)
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 35m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 3m)
			.SetDisplay("Trailing Step", "Step for trailing stop", "Risk");
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
		var fastWma = new WeightedMovingAverage { Length = FastWmaPeriod };
		var slowWma = new WeightedMovingAverage { Length = SlowWmaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = 9 }
		};
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastWma, slowWma, cci)
			.BindEx(macd, Process)
			.Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastWma);
			DrawIndicator(area, slowWma);
			DrawIndicator(area, cci);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : null,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : null,
			trailingStop: TrailingStop > 0 ? new Unit(TrailingStop, UnitTypes.Absolute) : null,
			trailingStep: TrailingStep > 0 ? new Unit(TrailingStep, UnitTypes.Absolute) : null
		);
	}
	private void Process(ICandleMessage candle, decimal fast, decimal slow, decimal cciValue, IIndicatorValue macdValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;
		// Ensure trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;
		// Determine signals
		var buy = fast > slow && macdLine > signalLine && cciValue > CciThreshold;
		var sell = fast < slow && macdLine < signalLine && cciValue < -CciThreshold;
		if (buy && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (sell && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
