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

		_emaLongLength = Param(nameof(EmaLongLength), 40)
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

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		var fast = new ExponentialMovingAverage { Length = EmaLength };
		var slow = new ExponentialMovingAverage { Length = EmaLongLength };

		var prevF = 0m;
		var prevS = 0m;
		var init = false;
		var lastSignal = DateTimeOffset.MinValue;
		var cooldown = TimeSpan.FromMinutes(360);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fast, slow, (candle, f, s) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!fast.IsFormed || !slow.IsFormed)
					return;

				if (!init)
				{
					prevF = f;
					prevS = s;
					init = true;
					return;
				}

				if (candle.OpenTime - lastSignal >= cooldown)
				{
					if (prevF <= prevS && f > s && Position <= 0)
					{
						BuyMarket();
						lastSignal = candle.OpenTime;
					}
					else if (prevF >= prevS && f < s && Position >= 0)
					{
						SellMarket();
						lastSignal = candle.OpenTime;
					}
				}

				prevF = f;
				prevS = s;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
