using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic RSI strategy.
/// A Parabolic SAR (SarStart, SarIncrement, SarMax) is run on the RSI line instead of on price. When the SAR flips below the RSI and RSI
/// is at least LongRsiMin the strategy goes long, when it flips above the RSI and RSI is at most ShortRsiMax it goes short. An opposite
/// flip always closes the current position and reverses it when its own RSI condition holds.
/// </summary>
public class ParabolicRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _longRsiMin;
	private readonly StrategyParam<decimal> _shortRsiMax;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;
	private decimal? _prevRsi2;
	private bool _sarReady;
	private bool _isUpTrend;
	private decimal _sar;
	private decimal _extreme;
	private decimal _af;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Initial SAR acceleration factor.
	/// </summary>
	public decimal SarStart
	{
		get => _sarStart.Value;
		set => _sarStart.Value = value;
	}

	/// <summary>
	/// SAR acceleration factor increment.
	/// </summary>
	public decimal SarIncrement
	{
		get => _sarIncrement.Value;
		set => _sarIncrement.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration factor.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Minimum RSI for a long entry.
	/// </summary>
	public decimal LongRsiMin
	{
		get => _longRsiMin.Value;
		set => _longRsiMin.Value = value;
	}

	/// <summary>
	/// Maximum RSI for a short entry.
	/// </summary>
	public decimal ShortRsiMax
	{
		get => _shortRsiMax.Value;
		set => _shortRsiMax.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ParabolicRsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Start", "Initial SAR acceleration factor", "Indicators");

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Increment", "SAR acceleration factor increment", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum SAR acceleration factor", "Indicators");

		_longRsiMin = Param(nameof(LongRsiMin), 50m)
			.SetDisplay("Long RSI Min", "Minimum RSI for a long entry", "Signals");

		_shortRsiMax = Param(nameof(ShortRsiMax), 50m)
			.SetDisplay("Short RSI Max", "Maximum RSI for a short entry", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_prevRsi = null;
		_prevRsi2 = null;
		_sarReady = false;
		_isUpTrend = false;
		_sar = 0m;
		_extreme = 0m;
		_af = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var flip = UpdateSar(rsi);

		if (flip == 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (flip > 0)
		{
			if (rsi >= LongRsiMin && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(-Position);
		}
		else
		{
			if (rsi <= ShortRsiMax && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}

	// Parabolic SAR on a single series; returns 1 on a flip below the series, -1 on a flip above it, 0 otherwise.
	private int UpdateSar(decimal value)
	{
		var prev = _prevRsi;
		var prev2 = _prevRsi2;
		_prevRsi2 = _prevRsi;
		_prevRsi = value;

		if (prev is not decimal p)
			return 0;

		if (!_sarReady)
		{
			_isUpTrend = value >= p;
			_sar = _isUpTrend ? Math.Min(p, value) : Math.Max(p, value);
			_extreme = _isUpTrend ? Math.Max(p, value) : Math.Min(p, value);
			_af = SarStart;
			_sarReady = true;
			return 0;
		}

		var sar = _sar + _af * (_extreme - _sar);

		if (_isUpTrend)
		{
			sar = Math.Min(sar, p);
			if (prev2 is decimal p2)
				sar = Math.Min(sar, p2);

			if (value < sar)
			{
				_isUpTrend = false;
				_sar = _extreme;
				_extreme = value;
				_af = SarStart;
				return -1;
			}

			if (value > _extreme)
			{
				_extreme = value;
				_af = Math.Min(_af + SarIncrement, SarMax);
			}
		}
		else
		{
			sar = Math.Max(sar, p);
			if (prev2 is decimal p2)
				sar = Math.Max(sar, p2);

			if (value > sar)
			{
				_isUpTrend = true;
				_sar = _extreme;
				_extreme = value;
				_af = SarStart;
				return 1;
			}

			if (value < _extreme)
			{
				_extreme = value;
				_af = Math.Min(_af + SarIncrement, SarMax);
			}
		}

		_sar = sar;
		return 0;
	}
}
