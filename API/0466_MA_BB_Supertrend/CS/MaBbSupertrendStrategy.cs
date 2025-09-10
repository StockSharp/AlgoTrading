using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover confirmed by SuperTrend with Bollinger Bands exits.
/// </summary>
public class MaBbSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _maSignalLength;
	private readonly StrategyParam<decimal> _maRatio;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendFactor;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _maBasis;
	private MovingAverage _maSignal;
	private BollingerBands _bollinger;
	private SuperTrend _supertrend;

	private decimal _prevBasis;
	private decimal _prevSignal;
	private bool _isInitialized;

	/// <summary>
	/// Length of the signal moving average.
	/// </summary>
	public int MaSignalLength
	{
		get => _maSignalLength.Value;
		set => _maSignalLength.Value = value;
	}

	/// <summary>
	/// Ratio to calculate basis MA length.
	/// </summary>
	public decimal MaRatio
	{
		get => _maRatio.Value;
		set => _maRatio.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal SupertrendFactor
	{
		get => _supertrendFactor.Value;
		set => _supertrendFactor.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MaBbSupertrendStrategy()
	{
		_maSignalLength = Param(nameof(MaSignalLength), 89)
			.SetGreaterThanZero()
			.SetDisplay("MA Signal Length", "Length of the signal moving average", "MA")
			.SetCanOptimize(true)
			.SetOptimize(30, 150, 10);

		_maRatio = Param(nameof(MaRatio), 1.08m)
			.SetRange(0.5m, 3m)
			.SetDisplay("MA Ratio", "Basis length = Signal length * Ratio", "MA")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.1m);

		_bbLength = Param(nameof(BbLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_bbMultiplier = Param(nameof(BbMultiplier), 3m)
			.SetRange(1m, 10m)
			.SetDisplay("BB Width", "Bollinger Bands width", "Bollinger")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_supertrendPeriod = Param(nameof(SupertrendPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Period", "ATR period for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_supertrendFactor = Param(nameof(SupertrendFactor), 4m)
			.SetRange(1m, 10m)
			.SetDisplay("SuperTrend Factor", "ATR multiplier for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 8m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var basisLength = (int)Math.Round(MaSignalLength * MaRatio);

		_maBasis = new SMA { Length = basisLength };
		_maSignal = new SMA { Length = MaSignalLength };
		_bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		_supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendFactor };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_maBasis, _maSignal, _supertrend, _bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _maBasis);
			DrawIndicator(area, _maSignal);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis, decimal signal,
		decimal supertrendValue, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_maBasis.IsFormed || !_maSignal.IsFormed || !_bollinger.IsFormed || !_supertrend.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevBasis = basis;
			_prevSignal = signal;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevSignal < _prevBasis && signal > basis;
		var crossDown = _prevSignal > _prevBasis && signal < basis;

		var uptrend = candle.ClosePrice > supertrendValue;

		if (crossUp && uptrend && Position <= 0)
			BuyMarket();
		else if (crossDown && !uptrend && Position >= 0)
			SellMarket();

		if (Position > 0)
		{
			if (candle.ClosePrice >= upper || candle.ClosePrice < supertrendValue)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= lower || candle.ClosePrice > supertrendValue)
				BuyMarket();
		}

		_prevBasis = basis;
		_prevSignal = signal;
	}
}
