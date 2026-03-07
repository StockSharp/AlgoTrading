using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Extreme EA strategy using fast/slow EMA crossover with CCI filter.
/// Buys when both EMAs rising and CCI below lower level (oversold bounce).
/// Sells when both EMAs falling and CCI above upper level (overbought reversal).
/// </summary>
public class ExtremeEaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciUpperLevel;
	private readonly StrategyParam<decimal> _cciLowerLevel;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevFast2;
	private decimal _prevSlow2;
	private bool _hasPrev;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// CCI indicator length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI threshold for sell entries.
	/// </summary>
	public decimal CciUpperLevel
	{
		get => _cciUpperLevel.Value;
		set => _cciUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower CCI threshold for buy entries.
	/// </summary>
	public decimal CciLowerLevel
	{
		get => _cciLowerLevel.Value;
		set => _cciLowerLevel.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ExtremeEaStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast EMA period", "Indicator");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow EMA period", "Indicator");

		_cciPeriod = Param(nameof(CciPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI lookback period", "Indicator");

		_cciUpperLevel = Param(nameof(CciUpperLevel), 50m)
			.SetDisplay("CCI Upper", "Upper CCI threshold for sell", "Levels");

		_cciLowerLevel = Param(nameof(CciLowerLevel), -50m)
			.SetDisplay("CCI Lower", "Lower CCI threshold for buy", "Levels");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null;
		_slowMa = null;
		_prevFast = 0;
		_prevSlow = 0;
		_prevFast2 = 0;
		_prevSlow2 = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_fastMa, _slowMa, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
			_prevFast2 = _prevFast;
			_prevSlow2 = _prevSlow;
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevFast2 = _prevFast;
			_prevSlow2 = _prevSlow;
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_hasPrev = true;
			return;
		}

		var slowIsRising = _prevSlow > _prevSlow2;
		var slowIsFalling = _prevSlow < _prevSlow2;
		var fastIsRising = fastValue > _prevFast;
		var fastIsFalling = fastValue < _prevFast;

		// Buy: fast crosses above slow
		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
		}
		// Sell: fast crosses below slow
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
		}

		_prevFast2 = _prevFast;
		_prevSlow2 = _prevSlow;
		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
