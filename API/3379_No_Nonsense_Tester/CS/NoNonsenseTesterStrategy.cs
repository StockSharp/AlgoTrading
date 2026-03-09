namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// No Nonsense Tester strategy: EMA baseline + RSI confirmation + CCI confirmation.
/// Buys when price above EMA, RSI above 50, CCI above 0.
/// Sells when price below EMA, RSI below 50, CCI below 0.
/// </summary>
public class NoNonsenseTesterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private bool _wasBullish;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public NoNonsenseTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_emaPeriod = Param(nameof(EmaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA baseline period", "Indicators");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI confirmation period", "Indicators");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI confirmation period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_wasBullish = false;
		_hasPrevSignal = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrevSignal = false;
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi, cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal rsi, decimal cci)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		var isBullish = close > ema && rsi > 50 && cci > 0;

		if (_hasPrevSignal && isBullish != _wasBullish)
		{
			if (isBullish && Position <= 0)
				BuyMarket();
			else if (!isBullish && close < ema && rsi < 50 && cci < 0 && Position >= 0)
				SellMarket();
		}

		_wasBullish = isBullish;
		_hasPrevSignal = true;
	}
}
