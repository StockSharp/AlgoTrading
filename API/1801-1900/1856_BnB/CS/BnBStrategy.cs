using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on bull/bear power comparison using EMA smoothing.
/// </summary>
public class BnBStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _minNetPower;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevBull;
	private decimal _prevBear;
	private bool _initialized;
	private decimal _bullEma;
	private decimal _bearEma;
	private decimal _k;
	private int _count;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal MinNetPower { get => _minNetPower.Value; set => _minNetPower.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public BnBStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "General");

		_length = Param(nameof(Length), 14)
			.SetDisplay("EMA Length", "Length of smoothing for bulls and bears", "Parameters");

		_minNetPower = Param(nameof(MinNetPower), 20m)
			.SetDisplay("Minimum Net Power", "Minimum absolute net bull/bear power for entries", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_prevBull = 0m;
		_prevBear = 0m;
		_initialized = false;
		_bullEma = 0m;
		_bearEma = 0m;
		_k = 0m;
		_count = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_k = 2m / (Length + 1m);
		_count = 0;

		var sma = new SimpleMovingAverage { Length = Length };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bullPower = candle.HighPrice - smaValue;
		var bearPower = candle.LowPrice - smaValue;
		_count++;
		if (_count == 1)
		{
			_bullEma = bullPower;
			_bearEma = bearPower;
		}
		else
		{
			_bullEma = bullPower * _k + _bullEma * (1m - _k);
			_bearEma = bearPower * _k + _bearEma * (1m - _k);
		}

		if (_count < Length)
			return;

		if (!_initialized)
		{
			_prevBull = _bullEma;
			_prevBear = _bearEma;
			_initialized = true;
			return;
		}

		var netPower = _bullEma + _bearEma;
		var prevNet = _prevBull + _prevBear;
		var crossUp = prevNet <= 0m && netPower > 0m && Math.Abs(netPower) >= MinNetPower;
		var crossDown = prevNet >= 0m && netPower < 0m && Math.Abs(netPower) >= MinNetPower;

		if (_cooldownRemaining == 0)
		{
			if (crossUp && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevBull = _bullEma;
		_prevBear = _bearEma;
	}
}

