using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing liquidity grab and market structure shift logic for Gold and EUR/USD.
/// </summary>
public class GoldEurUsdStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _prevHighIndicator = null!;
	private Lowest _prevLowIndicator = null!;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }

	/// <summary>
	/// SMA period length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int StochLength { get => _stochLength.Value; set => _stochLength.Value = value; }

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochOverbought { get => _stochOverbought.Value; set => _stochOverbought.Value = value; }

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochOversold { get => _stochOversold.Value; set => _stochOversold.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoldEurUsdStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetRange(5, 50)
			.SetDisplay("RSI Length", "RSI period", "RSI")
			.SetCanOptimize(true);

		_overbought = Param(nameof(Overbought), 70m)
			.SetRange(60m, 90m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
			.SetCanOptimize(true);

		_oversold = Param(nameof(Oversold), 30m)
			.SetRange(10m, 40m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
			.SetCanOptimize(true);

		_maLength = Param(nameof(MaLength), 50)
			.SetRange(10, 200)
			.SetDisplay("MA Length", "SMA period", "MA")
			.SetCanOptimize(true);

		_stochLength = Param(nameof(StochLength), 14)
			.SetRange(5, 50)
			.SetDisplay("Stoch Length", "Stochastic %K period", "Stochastic")
			.SetCanOptimize(true);

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetRange(50m, 100m)
			.SetDisplay("Stoch Overbought", "Stochastic overbought level", "Stochastic")
			.SetCanOptimize(true);

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetRange(0m, 50m)
			.SetDisplay("Stoch Oversold", "Stochastic oversold level", "Stochastic")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ma = new SMA { Length = MaLength };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = 3 },
		};
		var atr = new AverageTrueRange { Length = 14 };
		var demand = new Lowest { Length = 20 };
		var supply = new Highest { Length = 20 };

		_prevHighIndicator = new Highest { Length = 5 };
		_prevLowIndicator = new Lowest { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ma, stochastic, atr, demand, supply, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, rsi);
			DrawIndicator(area, stochastic);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue maValue, IIndicatorValue stochValue, IIndicatorValue atrValue, IIndicatorValue demandValue, IIndicatorValue supplyValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose == 0m)
		{
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		var rsi = rsiValue.ToDecimal();
		var ma = maValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var demand = demandValue.ToDecimal();
		var supply = supplyValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;
		var k = stoch.K;
		var d = stoch.D;

		var highestPrevVal = _prevHighIndicator.Process(_prevHigh);
		var lowestPrevVal = _prevLowIndicator.Process(_prevLow);
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (!_prevHighIndicator.IsFormed || !_prevLowIndicator.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var highestPrev = highestPrevVal.ToDecimal();
		var lowestPrev = lowestPrevVal.ToDecimal();

		var bullishShift = candle.HighPrice > highestPrev && candle.LowPrice > lowestPrev;
		var bearishShift = candle.LowPrice < lowestPrev && candle.HighPrice < highestPrev;

		var liquidityGrabLow = candle.LowPrice < demand;
		var liquidityGrabHigh = candle.HighPrice > supply;

		var fairValueGap = Math.Abs(candle.ClosePrice - _prevClose) > atr * 0.5m;

		if (liquidityGrabLow && bullishShift && fairValueGap && rsi < Oversold && candle.ClosePrice > ma && k < StochOversold && d < StochOversold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (liquidityGrabHigh && bearishShift && fairValueGap && rsi > Overbought && candle.ClosePrice < ma && k > StochOverbought && d > StochOverbought && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevClose = candle.ClosePrice;
	}
}

