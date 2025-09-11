namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy combining Ichimoku Cloud with RSI and MACD crossover.
/// </summary>
public class IchimokuRsiMacdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;

	private Ichimoku _ichimoku;
	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergenceSignal _macd;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod { get => _tenkanPeriod.Value; set => _tenkanPeriod.Value = value; }

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod { get => _kijunPeriod.Value; set => _kijunPeriod.Value = value; }

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod { get => _senkouSpanBPeriod.Value; set => _senkouSpanBPeriod.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="IchimokuRsiMacdStrategy"/> class.
	/// </summary>
	public IchimokuRsiMacdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
		.SetRange(5, 30)
		.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
		.SetCanOptimize(true);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
		.SetRange(10, 50)
		.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
		.SetCanOptimize(true);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
		.SetRange(30, 70)
		.SetDisplay("Senkou Span B Period", "Senkou Span B period", "Ichimoku")
		.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetRange(5, 30)
		.SetDisplay("RSI Length", "RSI period", "RSI")
		.SetCanOptimize(true);

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
		.SetRange(60, 80)
		.SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
		.SetCanOptimize(true);

		_rsiOversold = Param(nameof(RsiOversold), 30)
		.SetRange(20, 40)
		.SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
		.SetCanOptimize(true);

		_macdFast = Param(nameof(MacdFast), 12)
		.SetRange(5, 20)
		.SetDisplay("MACD Fast", "MACD fast period", "MACD")
		.SetCanOptimize(true);

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetRange(20, 40)
		.SetDisplay("MACD Slow", "MACD slow period", "MACD")
		.SetCanOptimize(true);

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetRange(5, 15)
		.SetDisplay("MACD Signal", "MACD signal period", "MACD")
		.SetCanOptimize(true);
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
		_ichimoku?.Reset();
		_rsi?.Reset();
		_macd?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_ichimoku, _rsi, _macd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);

			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, _rsi);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, _macd);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.SenkouA is not decimal senkouA)
		return;

		if (ichimokuTyped.SenkouB is not decimal senkouB)
		return;

		var rsi = rsiValue.ToDecimal();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine)
		return;

		if (macdTyped.Signal is not decimal signalLine)
		return;

		var prevMacdValue = _macd.GetValue<MovingAverageConvergenceDivergenceSignalValue>(1);

		if (prevMacdValue.Macd is not decimal prevMacdLine)
		return;

		if (prevMacdValue.Signal is not decimal prevSignalLine)
		return;

		var priceAboveCloud = candle.ClosePrice > senkouA && candle.ClosePrice > senkouB;
		var priceBelowCloud = candle.ClosePrice < senkouA && candle.ClosePrice < senkouB;

		var crossUp = macdLine > signalLine && prevMacdLine <= prevSignalLine;
		var crossDown = macdLine < signalLine && prevMacdLine >= prevSignalLine;

		var longCondition = priceAboveCloud && rsi > RsiOversold && crossUp;
		var shortCondition = priceBelowCloud && rsi < RsiOverbought && crossDown;

		if (longCondition && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position > 0)
		{
			SellMarket(Position);
		}
		else if (crossUp && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}