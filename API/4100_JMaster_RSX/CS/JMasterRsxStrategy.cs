using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader 4 expert advisor jMasterRSXv1 that aligns two Jurik RSX readings across M5 and M30 timeframes.
/// </summary>
public class JMasterRsxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _fastCandleType;
	private readonly StrategyParam<DataType> _slowCandleType;
	private readonly StrategyParam<int> _rsxLength;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _midlineLevel;
	private readonly StrategyParam<decimal> _epsilon;

	private RsxIndicator _fastRsx = null!;
	private RsxIndicator _slowRsx = null!;

	private decimal? _fastPrevious;
	private decimal? _slowPrevious;
	private decimal? _slowCurrent;

	/// <summary>
	/// Initializes a new instance of the <see cref="JMasterRsxStrategy"/> class.
	/// </summary>
	public JMasterRsxStrategy()
	{
		_fastCandleType = Param(nameof(FastCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Fast Candle", "Primary trading timeframe", "General");

		_slowCandleType = Param(nameof(SlowCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Slow Candle", "Higher timeframe for trend filter", "General");

		_rsxLength = Param(nameof(RsxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSX Length", "Lookback period used by both RSX indicators", "Indicators")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 75m)
			.SetRange(0m, 100m)
			.SetDisplay("Overbought", "RSX threshold triggering short entries", "Signals")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 25m)
			.SetRange(0m, 100m)
			.SetDisplay("Oversold", "RSX threshold triggering long entries", "Signals")
			.SetCanOptimize(true);

		_midlineLevel = Param(nameof(MidlineLevel), 50m)
			.SetRange(0m, 100m)
			.SetDisplay("Midline", "RSX midline separating bullish and bearish regimes", "Signals");

		_epsilon = Param(nameof(Epsilon), 1e-10m)
			.SetGreaterThanZero()
			.SetDisplay("RSX Epsilon", "Numerical stability epsilon used inside the RSX filter", "Indicators");

	}

	/// <summary>
	/// Fast timeframe candle type (defaults to 5-minute candles).
	/// </summary>
	public DataType FastCandleType
	{
		get => _fastCandleType.Value;
		set => _fastCandleType.Value = value;
	}

	/// <summary>
	/// Slow timeframe candle type (defaults to 30-minute candles).
	/// </summary>
	public DataType SlowCandleType
	{
		get => _slowCandleType.Value;
		set => _slowCandleType.Value = value;
	}

	/// <summary>
	/// Lookback period for both RSX indicators.
	/// </summary>
	public int RsxLength
	{
		get => _rsxLength.Value;
		set => _rsxLength.Value = value;
	}

	/// <summary>
	/// RSX level that signals an overbought condition on the fast timeframe.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// RSX level that signals an oversold condition on the fast timeframe.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Neutral RSX reference used for the higher timeframe trend filter.
	/// </summary>
	public decimal MidlineLevel
	{
		get => _midlineLevel.Value;
		set => _midlineLevel.Value = value;
	}

	/// <summary>
	/// Numerical stability epsilon passed to the Jurik RSX calculation.
	/// </summary>
	public decimal Epsilon
	{
		get => _epsilon.Value;
		set => _epsilon.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastRsx = new RsxIndicator { Length = RsxLength, Epsilon = Epsilon };
		_slowRsx = new RsxIndicator { Length = RsxLength, Epsilon = Epsilon };

		var fastSubscription = SubscribeCandles(FastCandleType);
		fastSubscription
			.Bind(_fastRsx, ProcessFastCandle)
			.Start();

		var slowSubscription = SubscribeCandles(SlowCandleType);
		slowSubscription
			.Bind(_slowRsx, ProcessSlowCandle)
			.Start();
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_fastPrevious = null;
		_slowPrevious = null;
		_slowCurrent = null;

		_fastRsx?.Reset();
		_slowRsx?.Reset();
	}

	private void ProcessFastCandle(ICandleMessage candle, decimal rsxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_slowPrevious.HasValue && _fastPrevious.HasValue)
		{
			// Evaluate exit and entry conditions using fully completed bars.
			var shouldSell = _slowPrevious.Value < MidlineLevel && _fastPrevious.Value > OverboughtLevel;
			var shouldBuy = _slowPrevious.Value > MidlineLevel && _fastPrevious.Value < OversoldLevel;

			if (Position > 0 && shouldSell)
			{
				ClosePosition();
			}
			else if (Position < 0 && shouldBuy)
			{
				ClosePosition();
			}
			else if (Position == 0)
			{
				var orderVolume = Volume;
				if (orderVolume <= 0m)
					return;

				if (shouldBuy)
				{
					BuyMarket(orderVolume);
				}
				else if (shouldSell)
				{
					SellMarket(orderVolume);
				}
			}
		}

		// Store the RSX value for the next bar to mimic the MT4 shift=1 behaviour.
		_fastPrevious = rsxValue;
	}

	private void ProcessSlowCandle(ICandleMessage candle, decimal rsxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Preserve the previous slow RSX reading so the next fast bar can reference it.
		if (_slowCurrent.HasValue)
			_slowPrevious = _slowCurrent;

		_slowCurrent = rsxValue;
	}

	private sealed class RsxIndicator : Indicator<ICandleMessage>
	{
		public decimal Epsilon { get; set; } = 1e-10m;

		private decimal _f88;
		private decimal _f90;
		private decimal _f0;
		private decimal _f8;
		private decimal _f10;
		private decimal _f18;
		private decimal _f20;
		private decimal _f28;
		private decimal _f30;
		private decimal _f38;
		private decimal _f40;
		private decimal _f48;
		private decimal _f50;
		private decimal _f58;
		private decimal _f60;
		private decimal _f68;
		private decimal _f70;
		private decimal _f78;
		private decimal _f80;
		private decimal _v14;
		private decimal _v18;
		private decimal _v20;

		public int Length { get; set; } = 14;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (candle.State != CandleStates.Finished)
				return new DecimalIndicatorValue(this, 50m, input.Time);

			var highLowSum = (candle.HighPrice + candle.LowPrice) * 100m;

			if (_f90 == 0m)
			{
				// Initialize the recursive filter using the very first finished candle.
				_f90 = 1m;
				_f0 = 0m;
				_f88 = Math.Max(5m, Length - 1m);
				_f8 = highLowSum;
				_f18 = 3m / (Length + 2m);
				_f20 = 1m - _f18;
				IsFormed = false;
				return new DecimalIndicatorValue(this, 50m, input.Time);
			}

			_f90 = _f88 <= _f90 ? _f88 + 1m : _f90 + 1m;

			_f10 = _f8;
			_f8 = highLowSum;
			var v8 = _f8 - _f10;
			_f28 = _f20 * _f28 + _f18 * v8;
			_f30 = _f18 * _f28 + _f20 * _f30;
			var vC = _f28 * 1.5m - _f30 * 0.5m;
			_f38 = _f20 * _f38 + _f18 * vC;
			_f40 = _f18 * _f38 + _f20 * _f40;
			var v10 = _f38 * 1.5m - _f40 * 0.5m;
			_f48 = _f20 * _f48 + _f18 * v10;
			_f50 = _f18 * _f48 + _f20 * _f50;
			_v14 = _f48 * 1.5m - _f50 * 0.5m;
			_f58 = _f20 * _f58 + _f18 * Math.Abs(v8);
			_f60 = _f18 * _f58 + _f20 * _f60;
			_v18 = _f58 * 1.5m - _f60 * 0.5m;
			_f68 = _f20 * _f68 + _f18 * _v18;
			_f70 = _f18 * _f68 + _f20 * _f70;
			var v1C = _f68 * 1.5m - _f70 * 0.5m;
			_f78 = _f20 * _f78 + _f18 * v1C;
			_f80 = _f18 * _f78 + _f20 * _f80;
			_v20 = _f78 * 1.5m - _f80 * 0.5m;

			if (_f88 >= _f90 && _f8 != _f10)
				_f0 = 1m;

			if (_f88 == _f90 && _f0 == 0m)
				_f90 = 0m;

			decimal result;
			if (_f88 < _f90 && Math.Abs(_v20) > Epsilon)
			{
				// Normalize the RSX output to the 0-100 oscillator range.
				result = (_v14 / _v20 + 1m) * 50m;
				if (result > 100m)
					result = 100m;
				else if (result < 0m)
					result = 0m;
			}
			else
			{
				result = 50m;
			}

			IsFormed = _f88 < _f90;
			return new DecimalIndicatorValue(this, result, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_f88 = 0m;
			_f90 = 0m;
			_f0 = 0m;
			_f8 = 0m;
			_f10 = 0m;
			_f18 = 0m;
			_f20 = 0m;
			_f28 = 0m;
			_f30 = 0m;
			_f38 = 0m;
			_f40 = 0m;
			_f48 = 0m;
			_f50 = 0m;
			_f58 = 0m;
			_f60 = 0m;
			_f68 = 0m;
			_f70 = 0m;
			_f78 = 0m;
			_f80 = 0m;
			_v14 = 0m;
			_v18 = 0m;
			_v20 = 0m;
		}
	}
}
