using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High Yield Spread strategy with SMA filter.
/// Uses either High Yield Spread or VIX to trigger trades.
/// </summary>
public class HighYieldSpreadStrategyWithSmaFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Basis> _basis;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<Sides> _direction;
	private readonly StrategyParam<int> _holdingPeriod;
	private readonly StrategyParam<bool> _useSmaFilter;
	private readonly StrategyParam<int> _smaLength;

	private SimpleMovingAverage _sma;
	private Security _basisSecurity;
	private decimal _spread;
	private int _barsInPosition;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Basis selection.
	/// </summary>
	public Basis BasisSelection
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
	/// Trade direction.
	/// </summary>
	public Sides Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Holding period in bars.
	/// </summary>
	public int HoldingPeriod
	{
		get => _holdingPeriod.Value;
		set => _holdingPeriod.Value = value;
	}

	/// <summary>
	/// Use SMA filter.
	/// </summary>
	public bool UseSmaFilter
	{
		get => _useSmaFilter.Value;
		set => _useSmaFilter.Value = value;
	}

	/// <summary>
	/// SMA length.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HighYieldSpreadStrategyWithSmaFilterStrategy"/> class.
	/// </summary>
	public HighYieldSpreadStrategyWithSmaFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_basis = Param(nameof(BasisSelection), Basis.HighYieldSpread)
			.SetDisplay("Basis", "Spread basis", "General");

		_threshold = Param(nameof(Threshold), 5m)
			.SetDisplay("Threshold", "Spread threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 0.5m);

		_direction = Param(nameof(Direction), Sides.Buy)
			.SetDisplay("Direction", "Trade direction", "Parameters");

		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Holding Period", "Bars to hold position", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_useSmaFilter = Param(nameof(UseSmaFilter), true)
			.SetDisplay("Use SMA Filter", "Enable price SMA filter", "Filters");

		_smaLength = Param(nameof(SmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length of SMA filter", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_basisSecurity = BasisSelection == Basis.HighYieldSpread
			? new Security { Id = "FRED:BAMLH0A0HYM2" }
			: new Security { Id = "CBOE:VIX" };

		return
		[
			(Security, CandleType),
			(_basisSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sma = null;
		_basisSecurity = null;
		_spread = default;
		_barsInPosition = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = SmaLength };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(_sma, ProcessMainCandle).Start();

		var spreadSub = SubscribeCandles(CandleType, security: _basisSecurity);
		spreadSub.Bind(ProcessSpreadCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSpreadCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_spread = candle.ClosePrice;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_spread == 0)
			return;

		var longCond = Direction == Sides.Buy && _spread > Threshold && (!UseSmaFilter || candle.ClosePrice > sma);
		var shortCond = Direction == Sides.Sell && _spread < Threshold && (!UseSmaFilter || candle.ClosePrice < sma);

		if (Position == 0)
		{
			if (longCond)
			{
				BuyMarket();
				_barsInPosition = 0;
			}
			else if (shortCond)
			{
				SellMarket();
				_barsInPosition = 0;
			}
		}
		else
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldingPeriod)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_barsInPosition = 0;
			}
		}
	}

	/// <summary>
	/// Spread basis options.
	/// </summary>
	public enum Basis
	{
		HighYieldSpread,
		Vix
	}
}

