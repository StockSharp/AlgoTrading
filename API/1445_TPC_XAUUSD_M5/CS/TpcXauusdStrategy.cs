using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XAUUSD strategy with EMA, RSI and MACD filters and fixed pip targets.
/// </summary>
public class TpcXauusdStrategy : Strategy
{
	private readonly StrategyParam<int> _ema200Len;
	private readonly StrategyParam<int> _ema21Len;
	private readonly StrategyParam<int> _rsiLen;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _slPips;
	private readonly StrategyParam<decimal> _tpPips;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _ema200;
	private EMA _ema21;
	private RSI _rsi;
	private MACD _macd;

	private decimal _prevMacd;
	private decimal _prevSignal;

	private decimal? _stop;
	private decimal? _take;

	public int Ema200Length { get => _ema200Len.Value; set => _ema200Len.Value = value; }
	public int Ema21Length { get => _ema21Len.Value; set => _ema21Len.Value = value; }
	public int RsiLength { get => _rsiLen.Value; set => _rsiLen.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public decimal SlPips { get => _slPips.Value; set => _slPips.Value = value; }
	public decimal TpPips { get => _tpPips.Value; set => _tpPips.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TpcXauusdStrategy()
	{
		_ema200Len = Param(nameof(Ema200Length), 200);
		_ema21Len = Param(nameof(Ema21Length), 21);
		_rsiLen = Param(nameof(RsiLength), 14);
		_macdFast = Param(nameof(MacdFast), 12);
		_macdSlow = Param(nameof(MacdSlow), 26);
		_macdSignal = Param(nameof(MacdSignal), 9);
		_slPips = Param(nameof(SlPips), 15m);
		_tpPips = Param(nameof(TpPips), 22.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
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

		_ema200 = new EMA { Length = Ema200Length };
		_ema21 = new EMA { Length = Ema21Length };
		_rsi = new RSI { Length = RsiLength };
		_macd = new MACD { ShortPeriod = MacdFast, LongPeriod = MacdSlow, SignalPeriod = MacdSignal };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema200, _ema21, _rsi, _macd, Process).Start();
	}

	private void Process(ICandleMessage candle, decimal ema200, decimal ema21, decimal rsi, decimal macdLine, decimal signalLine)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema200.IsFormed || !_ema21.IsFormed || !_rsi.IsFormed || !_macd.IsFormed)
			return;

		var sl = SlPips * Security.PriceStep * 10m;
		var tp = TpPips * Security.PriceStep * 10m;

		var longCond = candle.ClosePrice > ema200 && candle.ClosePrice > ema21 && rsi > 50m &&
			macdLine > signalLine && _prevMacd <= _prevSignal && macdLine > _prevMacd;
		var shortCond = candle.ClosePrice < ema200 && candle.ClosePrice < ema21 && rsi < 50m &&
			macdLine < signalLine && _prevMacd >= _prevSignal && macdLine < _prevMacd;

		if (longCond && Position <= 0)
		{
			BuyMarket(Volume);
			_stop = candle.ClosePrice - sl;
			_take = candle.ClosePrice + tp;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(Volume);
			_stop = candle.ClosePrice + sl;
			_take = candle.ClosePrice - tp;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stop || candle.HighPrice >= _take)
			{
				SellMarket(Math.Abs(Position));
				_stop = null;
				_take = null;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stop || candle.LowPrice <= _take)
			{
				BuyMarket(Math.Abs(Position));
				_stop = null;
				_take = null;
			}
		}

		_prevMacd = macdLine;
		_prevSignal = signalLine;
	}
}

