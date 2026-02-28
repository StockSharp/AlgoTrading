namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// RSI with EMA crossover strategy.
/// Enters when RSI crosses its own EMA.
/// </summary>
public class IdEmarsiOnChartStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevEma;
	private bool _isInitialized;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IdEmarsiOnChartStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 16)
			.SetDisplay("RSI Length", "RSI length", "General");

		_emaLength = Param(nameof(EmaLength), 42)
			.SetDisplay("EMA Length", "EMA of RSI length", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_prevEma = 0;
		_isInitialized = false;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate EMA of RSI manually (exponential smoothing)
		var alpha = 2m / (EmaLength + 1m);
		var emaValue = !_isInitialized ? rsiValue : _prevEma * (1 - alpha) + rsiValue * alpha;

		if (!_isInitialized)
		{
			_prevRsi = rsiValue;
			_prevEma = emaValue;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevRsi <= _prevEma && rsiValue > emaValue;
		var crossDown = _prevRsi >= _prevEma && rsiValue < emaValue;

		if (crossUp && Position <= 0)
			BuyMarket();
		else if (crossDown && Position >= 0)
			SellMarket();

		_prevRsi = rsiValue;
		_prevEma = emaValue;
	}
}
