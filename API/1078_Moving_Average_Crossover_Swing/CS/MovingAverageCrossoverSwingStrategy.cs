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
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMedium;
	private decimal _entryAtr;
	private bool _hasPrev;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrStopMult { get => _atrStopMult.Value; set => _atrStopMult.Value = value; }
	public decimal AtrTakeMult { get => _atrTakeMult.Value; set => _atrTakeMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MovingAverageCrossoverSwingStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5);
		_mediumPeriod = Param(nameof(MediumPeriod), 10);
		_atrPeriod = Param(nameof(AtrPeriod), 14);
		_atrStopMult = Param(nameof(AtrStopMult), 1.4m);
		_atrTakeMult = Param(nameof(AtrTakeMult), 3.2m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_entryAtr = 0;

		var fastEma = new EMA { Length = FastPeriod };
		var mediumEma = new EMA { Length = MediumPeriod };
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevMedium = medium;
			_hasPrev = true;
			return;
		}

		var longCross = _prevFast <= _prevMedium && fast > medium;
		var shortCross = _prevFast >= _prevMedium && fast < medium;

		if (longCross && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket();
			_entryAtr = atr;
		}
		else if (shortCross && Position >= 0)
		{
			if (Position > 0) SellMarket(Position);
			SellMarket();
			_entryAtr = atr;
		}

		if (Position > 0 && _entryAtr > 0)
		{
			var stop = PositionPrice - _entryAtr * AtrStopMult;
			var take = PositionPrice + _entryAtr * AtrTakeMult;
			if (candle.LowPrice <= stop || candle.HighPrice >= take)
				SellMarket();
		}
		else if (Position < 0 && _entryAtr > 0)
		{
			var stop = PositionPrice + _entryAtr * AtrStopMult;
			var take = PositionPrice - _entryAtr * AtrTakeMult;
			if (candle.HighPrice >= stop || candle.LowPrice <= take)
				BuyMarket();
		}

		_prevFast = fast;
		_prevMedium = medium;
	}
}
