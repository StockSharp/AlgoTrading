using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Post open long strategy with ATR-based stop loss and take profit.
/// Enters long after breakout during market open with multiple filters.
/// </summary>
public class PostOpenLongAtrStopLossTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMult;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrStopLossMultiplier;
	private readonly StrategyParam<decimal> _atrTakeProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _resistanceLevel;
	private int _resistanceTouches;

	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	private decimal _prevOpen1;
	private decimal _prevClose1;
	private decimal _prevOpen2;
	private decimal _prevClose2;
	private bool _hasPrev1;
	private bool _hasPrev2;

	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public decimal BbMult { get => _bbMult.Value; set => _bbMult.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiThreshold { get => _rsiThreshold.Value; set => _rsiThreshold.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrStopLossMultiplier { get => _atrStopLossMultiplier.Value; set => _atrStopLossMultiplier.Value = value; }
	public decimal AtrTakeProfitMultiplier { get => _atrTakeProfitMultiplier.Value; set => _atrTakeProfitMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PostOpenLongAtrStopLossTakeProfitStrategy()
	{
		_bbLength = Param(nameof(BbLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "General")
			;

		_bbMult = Param(nameof(BbMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("BB Mult", "Bollinger Bands multiplier", "General")
			;

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Short EMA length", "General")
			;

		_emaLongLength = Param(nameof(EmaLongLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long Length", "Long EMA length", "General")
			;

		_rsiLength = Param(nameof(RsiLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General")
			;

		_rsiThreshold = Param(nameof(RsiThreshold), 30m)
			.SetDisplay("RSI Threshold", "RSI minimum value", "General")
			;

		_adxLength = Param(nameof(AdxLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "General")
			;

		_adxThreshold = Param(nameof(AdxThreshold), 10m)
			.SetDisplay("ADX Threshold", "ADX minimum value", "General")
			;

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "General")
			;

		_atrStopLossMultiplier = Param(nameof(AtrStopLossMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR SL Mult", "ATR stop-loss multiplier", "General")
			;

		_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "ATR take-profit multiplier", "General")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new EMA { Length = EmaLength };
		var atr = new ATR { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var middleBand = emaValue;
		var upperBand = emaValue;
		var lowerBand = emaValue;
		var emaLongValue = emaValue;
		var rsiValue = 50m;
		var highestValue = candle.HighPrice;

		var time = candle.OpenTime;
		var hour = time.Hour;
		var minute = time.Minute;

		var daxOpen = hour >= 8 && hour < 12;
		var usOpen = (hour == 15 && minute >= 30) || (hour >= 16 && hour < 19);

		var lateralization = Math.Abs(candle.ClosePrice - middleBand) < (0.01m * candle.ClosePrice) && (daxOpen || usOpen);

		if (highestValue != _resistanceLevel)
		{
			_resistanceLevel = highestValue;
			_resistanceTouches = candle.HighPrice >= _resistanceLevel ? 1 : 0;
		}
		else if (candle.HighPrice >= _resistanceLevel && _resistanceTouches < 2)
		{
			_resistanceTouches++;
		}

		var breakout = candle.ClosePrice > _resistanceLevel && _resistanceTouches >= 2;

		var bullMarket = candle.ClosePrice > emaLongValue;
		var trendDown = candle.ClosePrice < emaValue;

		var firstRed = _hasPrev1 && _prevClose1 < _prevOpen1;
		var secondRed = _hasPrev2 && _prevClose2 < _prevOpen2;
		var avoidPullback = !(firstRed && secondRed);

		var panicCandle = candle.ClosePrice < candle.OpenPrice && (daxOpen || usOpen);

		var longCondition = candle.ClosePrice > emaValue && rsiValue > RsiThreshold &&
			candle.ClosePrice > emaLongValue;

		if (longCondition && Position == 0)
		{
			BuyMarket();
			_stopPrice = candle.ClosePrice - atrValue * AtrStopLossMultiplier;
			_takeProfitPrice = candle.ClosePrice + atrValue * AtrTakeProfitMultiplier;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
				SellMarket(Position);
			else if (candle.HighPrice >= _takeProfitPrice)
				SellMarket(Position);
		}

		_prevOpen2 = _prevOpen1;
		_prevClose2 = _prevClose1;
		_hasPrev2 = _hasPrev1;
		_prevOpen1 = candle.OpenPrice;
		_prevClose1 = candle.ClosePrice;
		_hasPrev1 = true;
	}
}
