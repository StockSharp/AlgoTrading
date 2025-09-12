using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum SpreadBasis
{
	HighYieldSpread,
	Vix
}

/// <summary>
/// Strategy that trades based on high yield spread or VIX with optional SMA filter.
/// Goes long when spread rises above threshold and price passes SMA filter.
/// Goes short when spread drops below threshold.
/// Closes positions after a fixed holding period.
/// </summary>
public class HighYieldSpreadSmaFilterStrategy : Strategy
{
	private readonly StrategyParam<SpreadBasis> _basis;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _isLong;
	private readonly StrategyParam<int> _holdingPeriod;
	private readonly StrategyParam<bool> _useSmaFilter;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private Security _spreadSecurity;
	private SMA _sma;
	private decimal _spreadValue;
	private int _barsInPosition;

	/// <summary>
	/// Spread source selection.
	/// </summary>
	public SpreadBasis Basis
	{
		get => _basis.Value;
		set => _basis.Value = value;
	}

	/// <summary>
	/// Spread threshold.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Trade direction. True for long, false for short.
	/// </summary>
	public bool IsLong
	{
		get => _isLong.Value;
		set => _isLong.Value = value;
	}

	/// <summary>
	/// Number of candles to hold position.
	/// </summary>
	public int HoldingPeriod
	{
		get => _holdingPeriod.Value;
		set => _holdingPeriod.Value = value;
	}

	/// <summary>
	/// Enable price filter using SMA.
	/// </summary>
	public bool UseSmaFilter
	{
		get => _useSmaFilter.Value;
		set => _useSmaFilter.Value = value;
	}

	/// <summary>
	/// SMA period length.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public HighYieldSpreadSmaFilterStrategy()
	{
		_basis = Param(nameof(Basis), SpreadBasis.HighYieldSpread)
			.SetDisplay("Basis", "Spread source", "General");

		_threshold = Param(nameof(Threshold), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Threshold", "Spread threshold", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_isLong = Param(nameof(IsLong), true)
			.SetDisplay("Long Direction", "Trade long if true, else short", "General");

		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Holding Period", "Number of candles to hold position", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_useSmaFilter = Param(nameof(UseSmaFilter), true)
			.SetDisplay("Use SMA Filter", "Enable SMA price filter", "Filter");

		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period for filter", "Filter")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_spreadSecurity ??= CreateSpreadSecurity();
		yield return (Security, CandleType);
		yield return (_spreadSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_spreadValue = 0m;
		_barsInPosition = 0;
		_spreadSecurity = null;
		_sma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_sma = new SMA { Length = SmaLength };

		var mainSub = SubscribeCandles(CandleType)
			.Bind(_sma, ProcessMain)
			.Start();

		_spreadSecurity ??= CreateSpreadSecurity();
		SubscribeCandles(CandleType, true, _spreadSecurity)
			.Bind(ProcessSpread)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private Security CreateSpreadSecurity()
	{
		var id = Basis == SpreadBasis.HighYieldSpread ? "BAMLH0A0HYM2@FRED" : "VIX@CBOE";
		return new Security { Id = id };
	}

	private void ProcessSpread(ICandleMessage candle)
	{
		if (candle.State == CandleStates.Finished)
			_spreadValue = candle.ClosePrice;
	}

	private void ProcessMain(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldingPeriod)
			{
				ClosePosition();
				_barsInPosition = 0;
			}

			return;
		}

		if (_spreadValue == 0m)
			return;

		if (UseSmaFilter && !_sma.IsFormed)
			return;

		var passSma = !UseSmaFilter || (IsLong ? candle.ClosePrice > smaValue : candle.ClosePrice < smaValue);
		if (!passSma)
			return;

		if (IsLong)
		{
			if (_spreadValue > Threshold && Position <= 0m)
			{
				BuyMarket();
				_barsInPosition = 0;
			}
		}
		else
		{
			if (_spreadValue < Threshold && Position >= 0m)
			{
				SellMarket();
				_barsInPosition = 0;
			}
		}
	}

	private void ClosePosition()
	{
		if (Position > 0m)
			SellMarket();
		else if (Position < 0m)
			BuyMarket();
	}
}
