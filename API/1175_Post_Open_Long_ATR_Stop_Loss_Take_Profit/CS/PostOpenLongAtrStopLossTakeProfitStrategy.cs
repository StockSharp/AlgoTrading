using System;
using System.Collections.Generic;

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

	private Highest _highest = null!;

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
			.SetCanOptimize();

		_bbMult = Param(nameof(BbMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("BB Mult", "Bollinger Bands multiplier", "General")
			.SetCanOptimize();

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Short EMA length", "General")
			.SetCanOptimize();

		_emaLongLength = Param(nameof(EmaLongLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long Length", "Long EMA length", "General")
			.SetCanOptimize();

		_rsiLength = Param(nameof(RsiLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General")
			.SetCanOptimize();

		_rsiThreshold = Param(nameof(RsiThreshold), 30m)
			.SetDisplay("RSI Threshold", "RSI minimum value", "General")
			.SetCanOptimize();

		_adxLength = Param(nameof(AdxLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "General")
			.SetCanOptimize();

		_adxThreshold = Param(nameof(AdxThreshold), 10m)
			.SetDisplay("ADX Threshold", "ADX minimum value", "General")
			.SetCanOptimize();

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "General")
			.SetCanOptimize();

		_atrStopLossMultiplier = Param(nameof(AtrStopLossMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR SL Mult", "ATR stop-loss multiplier", "General")
			.SetCanOptimize();

		_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "ATR take-profit multiplier", "General")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BbLength,
			Width = BbMult
		};

		var ema = new EMA { Length = EmaLength };
		var emaLong = new EMA { Length = EmaLongLength };
		var rsi = new RSI { Length = RsiLength };
		var adx = new ADX { Length = AdxLength };
		var atr = new ATR { Length = AtrLength };
		_highest = new Highest { Length = 20 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(bollinger, ema, emaLong, rsi, adx, atr, _highest, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, ema);
			DrawIndicator(area, emaLong);
			DrawIndicator(area, rsi);
			DrawIndicator(area, adx);
			DrawIndicator(area, _highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal middleBand,
		decimal upperBand,
		decimal lowerBand,
		decimal emaValue,
		decimal emaLongValue,
		decimal rsiValue,
		decimal adxValue,
		decimal atrValue,
		decimal highestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

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

		var longCondition = breakout && lateralization && candle.ClosePrice > emaValue && rsiValue > RsiThreshold &&
			adxValue > AdxThreshold && !trendDown && avoidPullback && bullMarket && panicCandle;

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
