using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Suite with selectable moving average type.
/// </summary>
public class HullSuiteByMRSStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<HullMode> _mode;

	private IIndicator _finalMa;
	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private ExponentialMovingAverage _emaFinal;
	private WeightedMovingAverage _wma1;
	private WeightedMovingAverage _wma2;
	private WeightedMovingAverage _wma3;
	private WeightedMovingAverage _wmaFinal;

	private decimal _prev1;
	private decimal _prev2;
	private bool _hasPrev1;
	private bool _hasPrev2;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Hull variation.
	/// </summary>
	public HullMode Mode { get => _mode.Value; set => _mode.Value = value; }

	/// <summary>
	/// Hull variation options.
	/// </summary>
	public enum HullMode
	{
		Hma,
		Ehma,
		Thma
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HullSuiteByMRSStrategy"/> class.
	/// </summary>
	public HullSuiteByMRSStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 55)
			.SetGreaterThanZero()
			.SetDisplay("Length", "MA length", "Parameters");

		_mode = Param(nameof(Mode), HullMode.Hma)
			.SetDisplay("Mode", "Hull variation", "Parameters");
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
		_prev1 = 0m;
		_prev2 = 0m;
		_hasPrev1 = false;
		_hasPrev2 = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		switch (Mode)
		{
			case HullMode.Hma:
			{
				_finalMa = new HullMovingAverage { Length = Length };
				subscription.Bind(_finalMa, ProcessHma).Start();
				break;
			}
			case HullMode.Ehma:
			{
				_emaFast = new ExponentialMovingAverage { Length = Math.Max(1, Length / 2) };
				_emaSlow = new ExponentialMovingAverage { Length = Length };
				_emaFinal = new ExponentialMovingAverage { Length = (int)Math.Round(Math.Sqrt(Length)) };
				_finalMa = _emaFinal;
				subscription.Bind(_emaFast, _emaSlow, ProcessEhma).Start();
				break;
			}
			case HullMode.Thma:
			{
				_wma1 = new WeightedMovingAverage { Length = Math.Max(1, Length / 3) };
				_wma2 = new WeightedMovingAverage { Length = Math.Max(1, Length / 2) };
				_wma3 = new WeightedMovingAverage { Length = Length };
				_wmaFinal = new WeightedMovingAverage { Length = Length };
				_finalMa = _wmaFinal;
				subscription.Bind(_wma1, _wma2, _wma3, ProcessThma).Start();
				break;
			}
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _finalMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHma(ICandleMessage candle, decimal hma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_finalMa.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		HandleSignal(hma);
	}

	private void ProcessEhma(ICandleMessage candle, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var raw = 2m * emaFast - emaSlow;
		var value = _emaFinal.Process(new DecimalIndicatorValue(_emaFinal, raw, candle.ServerTime)).ToDecimal();

		if (!_emaFinal.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		HandleSignal(value);
	}

	private void ProcessThma(ICandleMessage candle, decimal w1, decimal w2, decimal w3)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var raw = w1 * 3m - w2 - w3;
		var value = _wmaFinal.Process(new DecimalIndicatorValue(_wmaFinal, raw, candle.ServerTime)).ToDecimal();

		if (!_wmaFinal.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		HandleSignal(value);
	}

	private void HandleSignal(decimal value)
	{
		if (_hasPrev2)
		{
			if (value > _prev2 && Position <= 0)
				BuyMarket();

			if (value < _prev2 && Position >= 0)
				SellMarket();
		}

		_prev2 = _prev1;
		_prev1 = value;
		_hasPrev2 = _hasPrev1;
		_hasPrev1 = true;
	}
}
