using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on divergences using RSI and MACD histogram.
/// </summary>
public class DivergenceForManyIndicatorsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _minDivergence;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;

	private RelativeStrengthIndex _rsi = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _prevClose;
	private decimal _prevRsi;
	private decimal _prevMacdHist;
	private bool _hasPrev;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Fast period for MACD.
	/// </summary>
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }

	/// <summary>
	/// Slow period for MACD.
	/// </summary>
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }

	/// <summary>
	/// Minimal number of indicators confirming divergence.
	/// </summary>
	public int MinDivergence { get => _minDivergence.Value; set => _minDivergence.Value = value; }

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Initialize <see cref="DivergenceForManyIndicatorsStrategy"/>.
	/// </summary>
	public DivergenceForManyIndicatorsStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 1);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast period for MACD", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow period for MACD", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 34, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal period for MACD", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_minDivergence = Param(nameof(MinDivergence), 1)
			.SetGreaterThanZero()
			.SetDisplay("Min Divergence", "Minimum indicators confirming divergence", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 2, 1);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
			.SetDisplay("Candle Type", "Candle type for strategy", "General");

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _rsi, ProcessIndicators)
			.Start();

		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal || !rsiValue.IsFinal)
		return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal macdSignal)
		return;

		var macdHist = macdLine - macdSignal;
		var rsi = rsiValue.GetValue<decimal>();

		if (!_hasPrev)
		{
		_prevClose = candle.ClosePrice;
		_prevRsi = rsi;
		_prevMacdHist = macdHist;
		_hasPrev = true;
		return;
		}

		var negCount = 0;
		var posCount = 0;

		if (candle.ClosePrice > _prevClose && rsi < _prevRsi)
		negCount++;

		if (candle.ClosePrice > _prevClose && macdHist < _prevMacdHist)
		negCount++;

		if (candle.ClosePrice < _prevClose && rsi > _prevRsi)
		posCount++;

		if (candle.ClosePrice < _prevClose && macdHist > _prevMacdHist)
		posCount++;

		if (negCount >= MinDivergence && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		{
		CancelActiveOrders();
		SellMarket(Volume + Math.Abs(Position));
		}
		else if (posCount >= MinDivergence && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
		CancelActiveOrders();
		BuyMarket(Volume + Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
		_prevRsi = rsi;
		_prevMacdHist = macdHist;
	}
}
