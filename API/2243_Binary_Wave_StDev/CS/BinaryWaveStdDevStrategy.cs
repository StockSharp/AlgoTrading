using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Binary Wave Standard Deviation strategy.
/// Combines multiple indicators with weights and uses volatility filter based on standard deviation.
/// </summary>
public class BinaryWaveStdDevStrategy : Strategy
{
	private readonly StrategyParam<decimal> _weightMa;
	private readonly StrategyParam<decimal> _weightMacd;
	private readonly StrategyParam<decimal> _weightCci;
	private readonly StrategyParam<decimal> _weightMomentum;
	private readonly StrategyParam<decimal> _weightRsi;
	private readonly StrategyParam<decimal> _weightAdx;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _fastMacd;
	private readonly StrategyParam<int> _slowMacd;
	private readonly StrategyParam<int> _signalMacd;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _stdDevPeriod;
	private readonly StrategyParam<decimal> _entryVolatility;
	private readonly StrategyParam<decimal> _exitVolatility;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Weight for moving average signal.
	/// </summary>
	public decimal WeightMa { get => _weightMa.Value; set => _weightMa.Value = value; }

	/// <summary>
	/// Weight for MACD histogram signal.
	/// </summary>
	public decimal WeightMacd { get => _weightMacd.Value; set => _weightMacd.Value = value; }

	/// <summary>
	/// Weight for CCI signal.
	/// </summary>
	public decimal WeightCci { get => _weightCci.Value; set => _weightCci.Value = value; }

	/// <summary>
	/// Weight for Momentum signal.
	/// </summary>
	public decimal WeightMomentum { get => _weightMomentum.Value; set => _weightMomentum.Value = value; }

	/// <summary>
	/// Weight for RSI signal.
	/// </summary>
	public decimal WeightRsi { get => _weightRsi.Value; set => _weightRsi.Value = value; }

	/// <summary>
	/// Weight for ADX trend direction.
	/// </summary>
	public decimal WeightAdx { get => _weightAdx.Value; set => _weightAdx.Value = value; }

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Fast MACD EMA length.
	/// </summary>
	public int FastMacd { get => _fastMacd.Value; set => _fastMacd.Value = value; }

	/// <summary>
	/// Slow MACD EMA length.
	/// </summary>
	public int SlowMacd { get => _slowMacd.Value; set => _slowMacd.Value = value; }

	/// <summary>
	/// MACD signal line length.
	/// </summary>
	public int SignalMacd { get => _signalMacd.Value; set => _signalMacd.Value = value; }

	/// <summary>
	/// CCI lookback period.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	/// <summary>
	/// Momentum length.
	/// </summary>
	public int MomentumPeriod { get => _momentumPeriod.Value; set => _momentumPeriod.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// ADX length.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// Standard deviation length.
	/// </summary>
	public int StdDevPeriod { get => _stdDevPeriod.Value; set => _stdDevPeriod.Value = value; }

	/// <summary>
	/// Entry volatility threshold.
	/// </summary>
	public decimal EntryVolatility { get => _entryVolatility.Value; set => _entryVolatility.Value = value; }

	/// <summary>
	/// Exit volatility threshold.
	/// </summary>
	public decimal ExitVolatility { get => _exitVolatility.Value; set => _exitVolatility.Value = value; }

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BinaryWaveStdDevStrategy()
	{
		_weightMa = Param(nameof(WeightMa), 1m)
			.SetDisplay("MA Weight", "Weight for moving average direction", "Weights");
		_weightMacd = Param(nameof(WeightMacd), 1m)
			.SetDisplay("MACD Weight", "Weight for MACD histogram", "Weights");
		_weightCci = Param(nameof(WeightCci), 1m)
			.SetDisplay("CCI Weight", "Weight for CCI direction", "Weights");
		_weightMomentum = Param(nameof(WeightMomentum), 1m)
			.SetDisplay("Momentum Weight", "Weight for momentum", "Weights");
		_weightRsi = Param(nameof(WeightRsi), 1m)
			.SetDisplay("RSI Weight", "Weight for RSI", "Weights");
		_weightAdx = Param(nameof(WeightAdx), 1m)
			.SetDisplay("ADX Weight", "Weight for ADX trend", "Weights");

		_maPeriod = Param(nameof(MaPeriod), 13)
			.SetDisplay("MA Period", "Moving average period", "Indicators")
			.SetCanOptimize(true);
		_fastMacd = Param(nameof(FastMacd), 12)
			.SetDisplay("Fast MACD", "Fast EMA length", "Indicators")
			.SetCanOptimize(true);
		_slowMacd = Param(nameof(SlowMacd), 26)
			.SetDisplay("Slow MACD", "Slow EMA length", "Indicators")
			.SetCanOptimize(true);
		_signalMacd = Param(nameof(SignalMacd), 9)
			.SetDisplay("MACD Signal", "Signal line length", "Indicators")
			.SetCanOptimize(true);
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "Lookback period for CCI", "Indicators")
			.SetCanOptimize(true);
		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Lookback for momentum", "Indicators")
			.SetCanOptimize(true);
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Lookback for RSI", "Indicators")
			.SetCanOptimize(true);
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Lookback for ADX", "Indicators")
			.SetCanOptimize(true);
		_stdDevPeriod = Param(nameof(StdDevPeriod), 9)
			.SetDisplay("StdDev Period", "Length of standard deviation", "Indicators")
			.SetCanOptimize(true);

		_entryVolatility = Param(nameof(EntryVolatility), 1.5m)
			.SetDisplay("Entry Volatility", "Minimum standard deviation to enter", "Risk Management")
			.SetCanOptimize(true);
		_exitVolatility = Param(nameof(ExitVolatility), 1m)
			.SetDisplay("Exit Volatility", "Standard deviation threshold to exit", "Risk Management")
			.SetCanOptimize(true);
		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
			.SetCanOptimize(true);
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management")
			.SetCanOptimize(true);
		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk Management");
		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		StartProtection(
			useMarketOrders: true,
			takeProfit: UseTakeProfit ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null,
			stopLoss: UseStopLoss ? new Unit(StopLossPoints, UnitTypes.Absolute) : null);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = FastMacd,
			LongLength = SlowMacd,
			SignalLength = SignalMacd
		};
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var stdDev = new StandardDeviation { Length = StdDevPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, macd, cci, momentum, rsi, adx, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, cci);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);

			var volArea = CreateChartArea();
			if (volArea != null)
			{
				DrawIndicator(volArea, stdDev);
			}
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue emaValue,
		IIndicatorValue macdValue,
		IIndicatorValue cciValue,
		IIndicatorValue momentumValue,
		IIndicatorValue rsiValue,
		IIndicatorValue adxValue,
		IIndicatorValue stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ema = emaValue.ToDecimal();
		var macd = (MovingAverageConvergenceDivergenceValue)macdValue;
		var cci = cciValue.ToDecimal();
		var momentum = momentumValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var adx = (AverageDirectionalIndexValue)adxValue;
		var std = stdDevValue.ToDecimal();

		var score = 0m;
		score += candle.ClosePrice > ema ? WeightMa : -WeightMa;
		score += macd.Histogram > 0 ? WeightMacd : -WeightMacd;
		score += cci > 0 ? WeightCci : -WeightCci;
		score += momentum > 0 ? WeightMomentum : -WeightMomentum;
		score += rsi > 50 ? WeightRsi : -WeightRsi;
		score += adx.Dx.Plus > adx.Dx.Minus ? WeightAdx : -WeightAdx;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (std >= EntryVolatility)
		{
			if (score > 0 && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (score < 0 && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		if (std <= ExitVolatility && Position != 0)
		{
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(-Position);
		}
	}
}

