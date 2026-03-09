namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Five Minute RSI CCI strategy: RSI momentum with CCI trend confirmation.
/// Buys when RSI above level and CCI positive, sells when RSI below level and CCI negative.
/// </summary>
public class FiveMinuteRsiCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _bullishLevel;
	private readonly StrategyParam<decimal> _bearishLevel;
	private bool _wasBullish;
	private bool _hasPrevSignal;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal BullishLevel { get => _bullishLevel.Value; set => _bullishLevel.Value = value; }
	public decimal BearishLevel { get => _bearishLevel.Value; set => _bearishLevel.Value = value; }

	public FiveMinuteRsiCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
		_bullishLevel = Param(nameof(BullishLevel), 55m)
			.SetDisplay("Bullish RSI Level", "RSI above this for buy", "Signals");
		_bearishLevel = Param(nameof(BearishLevel), 45m)
			.SetDisplay("Bearish RSI Level", "RSI below this for sell", "Signals");
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
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var isBullish = rsiValue > BullishLevel && cciValue > 0;

		if (_hasPrevSignal && isBullish != _wasBullish)
		{
			if (isBullish && Position <= 0)
				BuyMarket();
			else if (!isBullish && rsiValue < BearishLevel && cciValue < 0 && Position >= 0)
				SellMarket();
		}

		_wasBullish = isBullish;
		_hasPrevSignal = true;
	}
}
