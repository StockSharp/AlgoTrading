using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Direction Index re-entry strategy.
/// Trades based on crossings between the TDI momentum line and the TDI index line.
/// </summary>
public class Tdi2ReOpenStrategy : Strategy
{
	private readonly StrategyParam<int> _tdiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastClose;
	private decimal? _directional;
	private decimal? _index;
	private decimal? _prevDirectional;
	private decimal? _prevIndex;

	public int TdiPeriod { get => _tdiPeriod.Value; set => _tdiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Tdi2ReOpenStrategy()
	{
		_tdiPeriod = Param(nameof(TdiPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("TDI Period", "Momentum lookback period", "Indicator")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Data series", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastClose = null;
		_directional = null;
		_index = null;
		_prevDirectional = null;
		_prevIndex = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastClose = null;
		_directional = null;
		_index = null;
		_prevDirectional = null;
		_prevIndex = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var close = candle.ClosePrice;
			if (_lastClose is not decimal lastClose)
			{
				_lastClose = close;
				return;
			}

			var momentum = close - lastClose;
			_lastClose = close;
			var alpha = 2m / (TdiPeriod + 1m);

			if (_directional is not decimal prevDirectionalLine || _index is not decimal prevIndexLine)
			{
				_directional = momentum;
				_index = momentum;
				return;
			}

			var directional = prevDirectionalLine + alpha * (momentum - prevDirectionalLine);
			var index = prevIndexLine + alpha * (directional - prevIndexLine);

			if (_prevDirectional is not decimal prevDir || _prevIndex is not decimal prevIdx)
			{
				_directional = directional;
				_index = index;
				_prevDirectional = prevDirectionalLine;
				_prevIndex = prevIndexLine;
				return;
			}

			var crossUp = prevDir <= prevIdx && directional > index;
			var crossDown = prevDir >= prevIdx && directional < index;

			if (crossUp && Position <= 0)
				BuyMarket();
			else if (crossDown && Position >= 0)
				SellMarket();

			_directional = directional;
			_index = index;
			_prevDirectional = directional;
			_prevIndex = index;
		}
	}
}
