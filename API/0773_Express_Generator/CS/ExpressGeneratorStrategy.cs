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
/// Strategy based on MA crossover with RSI and MACD confirmation and ATR-based position sizing.
/// </summary>
public class ExpressGeneratorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMa;
	private readonly StrategyParam<int> _slowMa;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _overbought;
	private readonly StrategyParam<int> _oversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<int> _trailingPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _trailingLong;
	private decimal _trailingShort;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Fast MA length.
	/// </summary>
	public int FastMa { get => _fastMa.Value; set => _fastMa.Value = value; }

	/// <summary>
	/// Slow MA length.
	/// </summary>
	public int SlowMa { get => _slowMa.Value; set => _slowMa.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int Oversold { get => _oversold.Value; set => _oversold.Value = value; }

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
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Risk percent of equity.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Stop loss size in pips used for position sizing.
	/// </summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingPips { get => _trailingPips.Value; set => _trailingPips.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ExpressGeneratorStrategy"/>.
	/// </summary>
	public ExpressGeneratorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastMa = Param(nameof(FastMa), 9)
			.SetDisplay("Fast MA", "Fast moving average length", "Indicators")
			.SetCanOptimize(true);

		_slowMa = Param(nameof(SlowMa), 21)
			.SetDisplay("Slow MA", "Slow moving average length", "Indicators")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators")
			.SetCanOptimize(true);

		_overbought = Param(nameof(Overbought), 70)
			.SetDisplay("Overbought", "RSI overbought level", "Signals")
			.SetCanOptimize(true);

		_oversold = Param(nameof(Oversold), 30)
			.SetDisplay("Oversold", "RSI oversold level", "Signals")
			.SetCanOptimize(true);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetDisplay("MACD Fast", "MACD fast period", "Indicators")
			.SetCanOptimize(true);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetDisplay("MACD Slow", "MACD slow period", "Indicators")
			.SetCanOptimize(true);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetDisplay("MACD Signal", "MACD signal period", "Indicators")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR calculation period", "Risk")
			.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetDisplay("Risk %", "Risk percent of equity", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetDisplay("Stop Loss Pips", "Stop loss size in pips", "Risk");

		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailingPips = Param(nameof(TrailingPips), 50)
			.SetDisplay("Trailing Pips", "Trailing stop distance in pips", "Risk");
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevMacd = 0m;
		_prevSignal = 0m;
		_trailingLong = 0m;
		_trailingShort = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fast = new SimpleMovingAverage { Length = FastMa };
		var slow = new SimpleMovingAverage { Length = SlowMa };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, fast, slow, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);

			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, rsi);
			DrawIndicator(rsiArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;

		var longMaCross = fast > slow && _prevFast <= _prevSlow;
		var shortMaCross = fast < slow && _prevFast >= _prevSlow;
		var longMacdCross = macd > signal && _prevMacd <= _prevSignal;
		var shortMacdCross = macd < signal && _prevMacd >= _prevSignal;

		var longCondition = longMaCross && rsi < Overbought && longMacdCross;
		var shortCondition = shortMaCross && rsi > Oversold && shortMacdCross;

		var volume = CalculateQty(atr);

		if (longCondition && Position <= 0 && volume > 0m)
		{
			BuyMarket(volume + Math.Abs(Position));
			if (UseTrailing)
				_trailingLong = candle.ClosePrice - TrailingPips * Security.PriceStep;
		}
		else if (shortCondition && Position >= 0 && volume > 0m)
		{
			SellMarket(volume + Math.Abs(Position));
			if (UseTrailing)
				_trailingShort = candle.ClosePrice + TrailingPips * Security.PriceStep;
		}

		if (UseTrailing)
		{
			if (Position > 0)
			{
				_trailingLong = Math.Max(_trailingLong, candle.ClosePrice - TrailingPips * Security.PriceStep);
				if (candle.ClosePrice <= _trailingLong)
				{
					SellMarket(Math.Abs(Position));
					_trailingLong = 0m;
				}
			}
			else if (Position < 0)
			{
				_trailingShort = Math.Min(_trailingShort, candle.ClosePrice + TrailingPips * Security.PriceStep);
				if (candle.ClosePrice >= _trailingShort)
				{
					BuyMarket(Math.Abs(Position));
					_trailingShort = 0m;
				}
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevMacd = macd;
		_prevSignal = signal;
	}

	private decimal CalculateQty(decimal atr)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * RiskPercent / 100m;
		var tradeRisk = StopLossPips * Security.PriceStep;
		var volFactor = atr / (Security.PriceStep * 100m);
		return tradeRisk > 0m && volFactor > 0m ? riskAmount / tradeRisk / volFactor : 0m;
	}
}
