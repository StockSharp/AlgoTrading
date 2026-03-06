using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MeanReversionVFStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _deviation1;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _ma;
	private decimal _entryPrice;
	private decimal _prevClose;
	private bool _hasPrevClose;
	private int _barsFromSignal;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal Deviation1 { get => _deviation1.Value; set => _deviation1.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MeanReversionVFStrategy()
	{
		_maLength = Param(nameof(MaLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "WMA length", "General");
		_deviation1 = Param(nameof(Deviation1), 3.2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation %", "Lower deviation from WMA", "General");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Target profit percent", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ma = null;
		_entryPrice = 0m;
		_prevClose = 0m;
		_hasPrevClose = false;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_ma = new WeightedMovingAverage { Length = MaLength };
		_entryPrice = 0;
		_prevClose = 0m;
		_hasPrevClose = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_ma.IsFormed)
			return;

		var l1 = maValue * (1 - Deviation1 / 100m);
		var close = candle.ClosePrice;
		_barsFromSignal++;

		if (Position > 0 && _entryPrice > 0)
		{
			var tpPrice = _entryPrice * (1 + TakeProfitPercent / 100m);
			if (candle.HighPrice >= tpPrice)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}

		var crossedBelow = _hasPrevClose && _prevClose >= l1 && close < l1;

		if (_barsFromSignal >= SignalCooldownBars && crossedBelow && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_barsFromSignal = 0;
		}
		else if (close > maValue && Position > 0)
		{
			SellMarket();
			_entryPrice = 0;
		}

		_prevClose = close;
		_hasPrevClose = true;
	}
}
