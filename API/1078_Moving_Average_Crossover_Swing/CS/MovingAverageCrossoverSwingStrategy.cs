using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover swing strategy with ATR-based stops.
/// </summary>
public class MovingAverageCrossoverSwingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _mediumPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopMult;
	private readonly StrategyParam<decimal> _atrTakeMult;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _minSpreadPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMedium;
	private decimal _entryPrice;
	private decimal _entryAtr;
	private bool _hasPrev;
	private int _barIndex;
	private int _lastSignalBar = -1000000;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrStopMult { get => _atrStopMult.Value; set => _atrStopMult.Value = value; }
	public decimal AtrTakeMult { get => _atrTakeMult.Value; set => _atrTakeMult.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal MinSpreadPercent { get => _minSpreadPercent.Value; set => _minSpreadPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageCrossoverSwingStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10);
		_mediumPeriod = Param(nameof(MediumPeriod), 30);
		_atrPeriod = Param(nameof(AtrPeriod), 20);
		_atrStopMult = Param(nameof(AtrStopMult), 5.0m);
		_atrTakeMult = Param(nameof(AtrTakeMult), 10.0m);
		_cooldownBars = Param(nameof(CooldownBars), 30).SetGreaterThanZero();
		_minSpreadPercent = Param(nameof(MinSpreadPercent), 0.01m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_barIndex = 0;
		_lastSignalBar = -1000000;
		_entryAtr = 0;

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var mediumEma = new ExponentialMovingAverage { Length = MediumPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, mediumEma, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal medium, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevMedium = medium;
			_hasPrev = true;
			return;
		}

		var spreadPercent = candle.ClosePrice != 0m
			? Math.Abs(fast - medium) / candle.ClosePrice * 100m
			: 0m;
		var canSignal = _barIndex - _lastSignalBar >= CooldownBars;
		var longCross = _prevFast <= _prevMedium && fast > medium;
		var shortCross = _prevFast >= _prevMedium && fast < medium;

		if (canSignal && longCross && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_entryAtr = atr;
			_lastSignalBar = _barIndex;
		}
		else if (canSignal && shortCross && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_entryAtr = atr;
			_lastSignalBar = _barIndex;
		}

		if (canSignal && Position > 0 && _entryAtr > 0)
		{
			var stop = _entryPrice - _entryAtr * AtrStopMult;
			var take = _entryPrice + _entryAtr * AtrTakeMult;
			if (candle.LowPrice <= stop || candle.HighPrice >= take)
			{
				SellMarket();
				_lastSignalBar = _barIndex;
			}
		}
		else if (canSignal && Position < 0 && _entryAtr > 0)
		{
			var stop = _entryPrice + _entryAtr * AtrStopMult;
			var take = _entryPrice - _entryAtr * AtrTakeMult;
			if (candle.HighPrice >= stop || candle.LowPrice <= take)
			{
				BuyMarket();
				_lastSignalBar = _barIndex;
			}
		}

		_prevFast = fast;
		_prevMedium = medium;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0m;
		_prevMedium = 0m;
		_entryPrice = 0m;
		_entryAtr = 0m;
		_hasPrev = false;
		_barIndex = 0;
		_lastSignalBar = -1000000;
	}
}
