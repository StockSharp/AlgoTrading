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
/// High-frequency strategy using MACD crossovers with RSI filter and ATR-based risk management.
/// </summary>
public class SunilHighFrequencyMacdRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<decimal> _atrMultiplierTrail;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal _entryPrice;
	private decimal _stop;
	private decimal _take;
	private decimal _highest;
	private decimal _lowest;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplierSl { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }
	public decimal AtrMultiplierTp { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }
	public decimal AtrMultiplierTrail { get => _atrMultiplierTrail.Value; set => _atrMultiplierTrail.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SunilHighFrequencyMacdRsiStrategy()
	{
		_fastLength = Param(nameof(FastLength), 6);
		_slowLength = Param(nameof(SlowLength), 12);
		_signalLength = Param(nameof(SignalLength), 9);
		_rsiLength = Param(nameof(RsiLength), 7);
		_rsiOverbought = Param(nameof(RsiOverbought), 70);
		_rsiOversold = Param(nameof(RsiOversold), 30);
		_atrLength = Param(nameof(AtrLength), 14);
		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 0.5m);
		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 1.5m);
		_atrMultiplierTrail = Param(nameof(AtrMultiplierTrail), 0.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = null;
		_prevSignal = null;
		_entryPrice = 0m;
		_stop = 0m;
		_take = 0m;
		_highest = 0m;
		_lowest = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdVal, IIndicatorValue rsiVal, IIndicatorValue atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return;

		var rsi = rsiVal.GetValue<decimal>();
		var atr = atrVal.GetValue<decimal>();

		if (atr == 0m)
			return;

		if (_prevMacd is decimal pm && _prevSignal is decimal ps)
		{
			var longCondition = pm <= ps && macd > signal && rsi < RsiOverbought;
			var shortCondition = pm >= ps && macd < signal && rsi > RsiOversold;

			if (Position == 0)
			{
				if (longCondition)
				{
					BuyMarket(Volume);
					_entryPrice = candle.ClosePrice;
					_stop = _entryPrice - AtrMultiplierSl * atr;
					_take = _entryPrice + AtrMultiplierTp * atr;
					_highest = candle.HighPrice;
				}
				else if (shortCondition)
				{
					SellMarket(Volume);
					_entryPrice = candle.ClosePrice;
					_stop = _entryPrice + AtrMultiplierSl * atr;
					_take = _entryPrice - AtrMultiplierTp * atr;
					_lowest = candle.LowPrice;
				}
			}
			else if (Position > 0)
			{
				_highest = Math.Max(_highest, candle.HighPrice);
				var trail = _highest - AtrMultiplierTrail * atr;

				if (candle.LowPrice <= _stop || candle.LowPrice <= trail || candle.HighPrice >= _take)
				{
					SellMarket(Math.Abs(Position));
					_stop = 0m;
					_take = 0m;
				}
			}
			else if (Position < 0)
			{
				_lowest = Math.Min(_lowest, candle.LowPrice);
				var trail = _lowest + AtrMultiplierTrail * atr;

				if (candle.HighPrice >= _stop || candle.HighPrice >= trail || candle.LowPrice <= _take)
				{
					BuyMarket(Math.Abs(Position));
					_stop = 0m;
					_take = 0m;
				}
			}
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}
