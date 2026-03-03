using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA crossover strategy with percent-based exits.
/// </summary>
public class MaWithLogisticStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private decimal _entryPrice;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private int _cooldown;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaWithLogisticStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "Indicators");
		_slowLength = Param(nameof(SlowLength), 25).SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "Indicators");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 8m).SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 5m).SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(20).TimeFrame())
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

		_entryPrice = default;
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

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
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

		var close = candle.ClosePrice;

		// MA crossover entry
		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = close;
			_cooldown = 5;
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = close;
			_cooldown = 5;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
