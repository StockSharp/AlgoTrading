using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using RSI and ROC with logistic-style normalization.
/// Trades on zero crossovers of a composite signal.
/// </summary>
public class LogisticRsiStochRocAoStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private RateOfChange _roc;
	private decimal? _prevSignal;
	private int _barsSinceSignal;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RocLength { get => _rocLength.Value; set => _rocLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public LogisticRsiStochRocAoStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "General");
		_rocLength = Param(nameof(RocLength), 9)
			.SetDisplay("ROC Length", "ROC period", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Min bars between signals", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null;
		_roc = null;
		_prevSignal = null;
		_barsSinceSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSignal = null;
		_barsSinceSignal = 0;
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_roc = new RateOfChange { Length = RocLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _roc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal rocVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_rsi.IsFormed || !_roc.IsFormed)
			return;

		// Composite signal: normalized RSI (-0.5 to 0.5) + sign of ROC
		var rsiNorm = rsiVal / 100m - 0.5m;
		var rocSign = rocVal > 0 ? 0.5m : rocVal < 0 ? -0.5m : 0m;
		var signal = rsiNorm + rocSign;

		if (_prevSignal.HasValue && _barsSinceSignal >= CooldownBars)
		{
			var prev = _prevSignal.Value;
			var crossUp = prev <= 0m && signal > 0m;
			var crossDown = prev >= 0m && signal < 0m;

			if (crossUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceSignal = 0;
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceSignal = 0;
			}
		}

		_prevSignal = signal;
	}
}
