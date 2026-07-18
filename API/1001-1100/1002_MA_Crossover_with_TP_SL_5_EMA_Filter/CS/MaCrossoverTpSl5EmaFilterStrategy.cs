using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA crossover strategy with take profit, stop loss, and EMA filter.
/// </summary>
public class MaCrossoverTpSl5EmaFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _targetPercent;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private ExponentialMovingAverage _ema;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private int _cooldown;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal TargetPercent { get => _targetPercent.Value; set => _targetPercent.Value = value; }
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaCrossoverTpSl5EmaFilterStrategy()
	{
		_fastLength = Param(nameof(FastLength), 8).SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "Indicators");
		_slowLength = Param(nameof(SlowLength), 21).SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "Indicators");
		_emaLength = Param(nameof(EmaLength), 5).SetGreaterThanZero()
			.SetDisplay("EMA Filter", "EMA filter length", "Indicators");
		_targetPercent = Param(nameof(TargetPercent), 10m).SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");
		_stopPercent = Param(nameof(StopPercent), 8m).SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
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

		_prevFast = default;
		_prevSlow = default;
		_initialized = false;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_ema.IsFormed)
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		// Long entry on cross up with EMA filter
		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = 12;
		}
		// Short entry on cross down with EMA filter
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = 12;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
