using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MNQ strategy based on multiple EMA levels and dynamic exits.
/// </summary>
public class MNQEMAStrategy : Strategy
{
	private readonly StrategyParam<int> _ema5Length;
	private readonly StrategyParam<int> _ema13Length;
	private readonly StrategyParam<int> _ema30Length;
	private readonly StrategyParam<int> _ema200Length;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma5;
	private decimal _prevEma13;
	private bool _hasPrev;
	private int _barsFromSignal;

	public int Ema5Length { get => _ema5Length.Value; set => _ema5Length.Value = value; }
	public int Ema13Length { get => _ema13Length.Value; set => _ema13Length.Value = value; }
	public int Ema30Length { get => _ema30Length.Value; set => _ema30Length.Value = value; }
	public int Ema200Length { get => _ema200Length.Value; set => _ema200Length.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MNQEMAStrategy()
	{
		_ema5Length = Param(nameof(Ema5Length), 5).SetGreaterThanZero();
		_ema13Length = Param(nameof(Ema13Length), 13).SetGreaterThanZero();
		_ema30Length = Param(nameof(Ema30Length), 30).SetGreaterThanZero();
		_ema200Length = Param(nameof(Ema200Length), 50).SetGreaterThanZero();
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma5 = 0m;
		_prevEma13 = 0m;
		_hasPrev = false;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_prevEma5 = 0m;
		_prevEma13 = 0m;
		_hasPrev = false;
		_barsFromSignal = SignalCooldownBars;

		var ema5 = new EMA { Length = Ema5Length };
		var ema13 = new EMA { Length = Ema13Length };
		var ema30 = new EMA { Length = Ema30Length };
		var ema200 = new EMA { Length = Ema200Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema5, ema13, ema30, ema200, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema5, decimal ema13, decimal ema30, decimal ema200)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		_barsFromSignal++;

		if (!_hasPrev)
		{
			_prevEma5 = ema5;
			_prevEma13 = ema13;
			_hasPrev = true;
			return;
		}

		var crossUp = _prevEma5 <= _prevEma13 && ema5 > ema13;
		var crossDown = _prevEma5 >= _prevEma13 && ema5 < ema13;

		var longTrend = close > ema200 && ema30 > ema200;
		var shortTrend = close < ema200 && ema30 < ema200;

		if (_barsFromSignal >= SignalCooldownBars && crossUp && longTrend && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && crossDown && shortTrend && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_barsFromSignal = 0;
		}
		else if (Position > 0 && crossDown)
		{
			SellMarket();
		}
		else if (Position < 0 && crossUp)
		{
			BuyMarket();
		}

		_prevEma5 = ema5;
		_prevEma13 = ema13;
	}
}
