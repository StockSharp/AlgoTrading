using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic crossover strategy with trailing stop and take profit.
/// Buys when %K crosses above %D in oversold zone, sells when crosses below in overbought zone.
/// </summary>
public class StochTpTsV3103Strategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _startOffset;
	private readonly StrategyParam<decimal> _trailStopOffset;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingStop;
	private decimal? _prevK;
	private decimal? _prevD;

	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal StartOffset { get => _startOffset.Value; set => _startOffset.Value = value; }
	public decimal TrailStopOffset { get => _trailStopOffset.Value; set => _trailStopOffset.Value = value; }
	public decimal StopLossOffset { get => _stopLossOffset.Value; set => _stopLossOffset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StochTpTsV3103Strategy()
	{
		_stochLength = Param(nameof(StochLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Stochastic K length", "Indicators");
		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing period for %D", "Indicators");
		_startOffset = Param(nameof(StartOffset), 400m)
			.SetDisplay("Start Offset", "Profit to activate trailing stop", "Risk");
		_trailStopOffset = Param(nameof(TrailStopOffset), 200m)
			.SetDisplay("Trail Stop Offset", "Trailing stop distance", "Risk");
		_stopLossOffset = Param(nameof(StopLossOffset), 500m)
			.SetDisplay("Stop Loss Offset", "Initial stop loss distance", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailingStop = 0;
		_prevK = null;
		_prevD = null;

		var stoch = new StochasticOscillator();
		stoch.K.Length = StochLength;
		stoch.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (StochasticOscillatorValue)stochValue;
		var k = typed.K;
		var d = typed.D;

		if (k is not decimal kVal || d is not decimal dVal)
			return;

		var price = candle.ClosePrice;

		// Trailing stop / SL / TP management
		if (Position > 0)
		{
			// Activate trailing once we reach the start offset
			if (price - _entryPrice >= StartOffset)
			{
				var newStop = price - TrailStopOffset;
				if (newStop > _trailingStop)
					_trailingStop = newStop;
			}

			// Check stop loss or trailing stop hit
			if ((_trailingStop > 0 && price <= _trailingStop) || (_entryPrice - price >= StopLossOffset))
			{
				SellMarket();
				_entryPrice = 0;
				_trailingStop = 0;
				_prevK = kVal;
				_prevD = dVal;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= StartOffset)
			{
				var newStop = price + TrailStopOffset;
				if (_trailingStop == 0 || newStop < _trailingStop)
					_trailingStop = newStop;
			}

			if ((_trailingStop > 0 && price >= _trailingStop) || (price - _entryPrice >= StopLossOffset))
			{
				BuyMarket();
				_entryPrice = 0;
				_trailingStop = 0;
				_prevK = kVal;
				_prevD = dVal;
				return;
			}
		}

		// Entry on stochastic crossover
		if (Position == 0 && _prevK.HasValue && _prevD.HasValue)
		{
			var prevKv = _prevK.Value;
			var prevDv = _prevD.Value;

			// Buy: %K crosses above %D (previous K <= D, current K > D)
			if (prevKv <= prevDv && kVal > dVal)
			{
				BuyMarket();
				_entryPrice = price;
				_trailingStop = 0;
			}
			// Sell: %K crosses below %D (previous K >= D, current K < D)
			else if (prevKv >= prevDv && kVal < dVal)
			{
				SellMarket();
				_entryPrice = price;
				_trailingStop = 0;
			}
		}

		_prevK = kVal;
		_prevD = dVal;
	}
}
