using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MarketTrendLevelsNonRepaintingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private decimal? _prevDiff;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MarketTrendLevelsNonRepaintingStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12);
		_slowLength = Param(nameof(SlowLength), 25);
		_rsiLength = Param(nameof(RsiLength), 14);
		_cooldownBars = Param(nameof(CooldownBars), 3);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_emaFast = null;
		_emaSlow = null;
		_rsi = null;
		_prevDiff = null;
		_barsFromSignal = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaFast = new ExponentialMovingAverage { Length = FastLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_prevDiff = null;
		_barsFromSignal = CooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _rsi, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, decimal fast, decimal slow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed)
		{
			_prevDiff = fast - slow;
			return;
		}

		var diff = fast - slow;
		var crossUp = _prevDiff.HasValue && _prevDiff <= 0 && diff > 0;
		var crossDown = _prevDiff.HasValue && _prevDiff >= 0 && diff < 0;
		_prevDiff = diff;
		var filterLong = rsiValue > 52;
		var filterShort = rsiValue < 48;

		if (crossUp && Position <= 0 && filterLong)
			BuyMarket();
		if (crossDown && Position >= 0 && filterShort)
			SellMarket();
	}
}
