using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XAUUSD 10-minute strategy using MACD, RSI and Bollinger Bands.
/// Opens long when bullish conditions are met and short on bearish signals.
/// Uses ATR-based stop-loss and take-profit with spread adjustment.
/// </summary>
public class Xauusd10MinuteStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopLossMul;
	private readonly StrategyParam<decimal> _takeProfitMul;
	private readonly StrategyParam<int> _spreadPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// Bollinger length.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Bollinger width.
	/// </summary>
	public decimal BollingerWidth { get => _bollingerWidth.Value; set => _bollingerWidth.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Stop-loss ATR multiplier.
	/// </summary>
	public decimal StopLossMul { get => _stopLossMul.Value; set => _stopLossMul.Value = value; }

	/// <summary>
	/// Take-profit ATR multiplier.
	/// </summary>
	public decimal TakeProfitMul { get => _takeProfitMul.Value; set => _takeProfitMul.Value = value; }

	/// <summary>
	/// Spread in price steps.
	/// </summary>
	public int SpreadPoints { get => _spreadPoints.Value; set => _spreadPoints.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public Xauusd10MinuteStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12).SetGreaterThanZero();
		_macdSlow = Param(nameof(MacdSlow), 26).SetGreaterThanZero();
		_macdSignal = Param(nameof(MacdSignal), 9).SetGreaterThanZero();
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_rsiOverbought = Param(nameof(RsiOverbought), 65m).SetDisplay("RSI Overbought", "Overbought level", "RSI");
		_rsiOversold = Param(nameof(RsiOversold), 35m).SetDisplay("RSI Oversold", "Oversold level", "RSI");
		_bollingerLength = Param(nameof(BollingerLength), 20).SetGreaterThanZero();
		_bollingerWidth = Param(nameof(BollingerWidth), 2m).SetGreaterThanZero();
		_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero();
		_stopLossMul = Param(nameof(StopLossMul), 3m).SetGreaterThanZero();
		_takeProfitMul = Param(nameof(TakeProfitMul), 5m).SetGreaterThanZero();
		_spreadPoints = Param(nameof(SpreadPoints), 38).SetGreaterThanOrEqualZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame());
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = 0;
		_prevSignal = 0;
		_entryPrice = 0;
		_stopLoss = 0;
		_takeProfit = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerWidth };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, rsi, bollinger, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, macd);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!macdValue.IsFinal || !rsiValue.IsFinal || !bollingerValue.IsFinal || !atrValue.IsFinal)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var rsi = rsiValue.ToDecimal();
		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upperBand || bb.LowBand is not decimal lowerBand)
			return;

		var atr = atrValue.ToDecimal();
		var close = candle.ClosePrice;

		var macdBuy = _prevMacd <= _prevSignal && macdTyped.Macd > macdTyped.Signal;
		var macdSell = _prevMacd >= _prevSignal && macdTyped.Macd < macdTyped.Signal;

		var buyCondition = macdBuy || rsi < RsiOversold || close < lowerBand;
		var sellCondition = macdSell || rsi > RsiOverbought || close > upperBand;

		var step = Security?.PriceStep ?? 1m;
		var spread = SpreadPoints * step;

		if (Position > 0)
		{
			if (sellCondition || close <= _stopLoss || close >= _takeProfit)
			{
				SellMarket(Volume + Position);
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (buyCondition || close >= _stopLoss || close <= _takeProfit)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (buyCondition)
			{
				_entryPrice = close + spread;
				_stopLoss = _entryPrice - StopLossMul * atr;
				_takeProfit = _entryPrice + TakeProfitMul * atr;
				BuyMarket(Volume);
			}
			else if (sellCondition)
			{
				_entryPrice = close - spread;
				_stopLoss = _entryPrice + StopLossMul * atr;
				_takeProfit = _entryPrice - TakeProfitMul * atr;
				SellMarket(Volume);
			}
		}

		_prevMacd = macdTyped.Macd;
		_prevSignal = macdTyped.Signal;
	}
}
