using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI with EMA smoothing for sentiment signals.
/// </summary>
public class MustangAlgoChannelStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaLen;
	private readonly StrategyParam<decimal> _upperBound;
	private readonly StrategyParam<decimal> _lowerBound;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevEma;
	private bool _isReady;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int EmaLen { get => _emaLen.Value; set => _emaLen.Value = value; }
	public decimal UpperBound { get => _upperBound.Value; set => _upperBound.Value = value; }
	public decimal LowerBound { get => _lowerBound.Value; set => _lowerBound.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MustangAlgoChannelStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Parameters");

		_emaLen = Param(nameof(EmaLen), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA smoothing period", "Parameters");

		_upperBound = Param(nameof(UpperBound), 60m)
			.SetDisplay("Upper Bound", "Overbought threshold", "Signals");

		_lowerBound = Param(nameof(LowerBound), 40m)
			.SetDisplay("Lower Bound", "Oversold threshold", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_prevEma = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaLen };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevRsi = rsi;
			_prevEma = ema;
			_isReady = true;
			return;
		}

		// Buy when RSI crosses up from oversold, sell when it crosses down from overbought
		var wasBelow = _prevRsi < LowerBound;
		var wasAbove = _prevRsi > UpperBound;

		if (wasBelow && rsi >= LowerBound && Position <= 0)
			BuyMarket();
		else if (wasAbove && rsi <= UpperBound && Position >= 0)
			SellMarket();

		_prevRsi = rsi;
		_prevEma = ema;
	}
}
