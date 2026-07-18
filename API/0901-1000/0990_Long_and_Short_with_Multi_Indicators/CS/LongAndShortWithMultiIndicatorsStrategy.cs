using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long and Short strategy with RSI, ROC and EMA trend filter.
/// </summary>
public class LongAndShortWithMultiIndicatorsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private RateOfChange _roc;
	private ExponentialMovingAverage _ma;
	private int _barsSinceSignal;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int RocLength { get => _rocLength.Value; set => _rocLength.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public LongAndShortWithMultiIndicatorsStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "Length of RSI", "Indicators")
			.SetGreaterThanZero();

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_rocLength = Param(nameof(RocLength), 10)
			.SetDisplay("ROC Length", "Length of ROC", "Indicators")
			.SetGreaterThanZero();

		_maLength = Param(nameof(MaLength), 20)
			.SetDisplay("MA Length", "Length of moving average", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 60)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_roc = null;
		_ma = null;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsSinceSignal = 0;
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_roc = new RateOfChange { Length = RocLength };
		_ma = new ExponentialMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _roc, _ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal rocVal, decimal maVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_rsi.IsFormed || !_roc.IsFormed || !_ma.IsFormed)
			return;

		if (_barsSinceSignal < CooldownBars)
			return;

		var close = candle.ClosePrice;

		// Long: price above MA trend + RSI not overbought + positive momentum
		var longSignal = close > maVal && rsiVal < RsiOverbought && rocVal > 0;
		// Short: price below MA trend + RSI not oversold + negative momentum
		var shortSignal = close < maVal && rsiVal > RsiOversold && rocVal < 0;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceSignal = 0;
		}
	}
}
