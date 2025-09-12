
using System;
using System.Collections.Generic;

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

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _initialized;
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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = 0m;
		_prevSignal = 0m;
		_initialized = false;
		_entryPrice = 0m;
		_stop = 0m;
		_take = 0m;
		_highest = 0m;
		_lowest = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		var atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, rsi, atr, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal _, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_initialized = true;
			return;
		}

		var longCondition = _prevMacd <= _prevSignal && macd > signal && rsi < RsiOverbought;
		var shortCondition = _prevMacd >= _prevSignal && macd < signal && rsi > RsiOversold;

		if (Position == 0)
		{
			if (longCondition)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stop = _entryPrice - AtrMultiplierSl * atr;
				_take = _entryPrice + AtrMultiplierTp * atr;
				_highest = candle.HighPrice;
			}
			else if (shortCondition)
			{
				SellMarket();
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
				SellMarket();
				_stop = 0m;
				_take = 0m;
			}
		}
		else
		{
			_lowest = Math.Min(_lowest, candle.LowPrice);
			var trail = _lowest + AtrMultiplierTrail * atr;

			if (candle.HighPrice >= _stop || candle.HighPrice >= trail || candle.LowPrice <= _take)
			{
				BuyMarket();
				_stop = 0m;
				_take = 0m;
			}
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}
