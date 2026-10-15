using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold trade setup strategy.
/// A Kaufman adaptive moving average (AmaLength, FastLength, SlowLength) gives the direction and a SuperTrend (AtrPeriod, Factor) the
/// flips. When the AMA is rising and the SuperTrend flips to an uptrend the strategy sells; when the AMA is falling and the SuperTrend
/// flips to a downtrend it buys. The target is TargetMultiplier ATRs and the stop RiskMultiplier ATRs from the entry.
/// </summary>
public class GoldTradeSetupStrategy : Strategy
{
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _targetMultiplier;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevAma;
	private bool? _prevUpTrend;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// AMA efficiency ratio length.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// AMA fast smoothing period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// AMA slow smoothing period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// ATR period of SuperTrend and the exits.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend ATR factor.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Target distance in ATRs.
	/// </summary>
	public decimal TargetMultiplier
	{
		get => _targetMultiplier.Value;
		set => _targetMultiplier.Value = value;
	}

	/// <summary>
	/// Stop distance in ATRs.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
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
	public GoldTradeSetupStrategy()
	{
		_amaLength = Param(nameof(AmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "AMA efficiency ratio length", "AMA");

		_fastLength = Param(nameof(FastLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "AMA fast smoothing period", "AMA");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "AMA slow smoothing period", "AMA");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period of SuperTrend and the exits", "SuperTrend");

		_factor = Param(nameof(Factor), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "SuperTrend ATR factor", "SuperTrend");

		_targetMultiplier = Param(nameof(TargetMultiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Target Multiplier", "Target distance in ATRs", "Risk");

		_riskMultiplier = Param(nameof(RiskMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Multiplier", "Stop distance in ATRs", "Risk");

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
		_prevAma = null;
		_prevUpTrend = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaLength,
			FastSCPeriod = FastLength,
			SlowSCPeriod = SlowLength,
		};
		var superTrend = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ama, superTrend, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ama);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue amaValue, IIndicatorValue superTrendValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!amaValue.IsFormed || !superTrendValue.IsFormed || !atrValue.IsFormed || superTrendValue is not SuperTrendIndicatorValue st)
			return;

		var ama = amaValue.GetValue<decimal>();
		var upTrend = st.IsUpTrend;
		var prevAma = _prevAma;
		var prevUpTrend = _prevUpTrend;
		_prevAma = ama;
		_prevUpTrend = upTrend;

		if (ManagePosition(candle))
			return;

		if (prevAma is not decimal pa || prevUpTrend is not bool pu)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || Position != 0)
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (ama > pa && upTrend && !pu)
		{
			SellMarket(Volume);
			_stopPrice = close + atr * RiskMultiplier;
			_takePrice = close - atr * TargetMultiplier;
		}
		else if (ama < pa && !upTrend && pu)
		{
			BuyMarket(Volume);
			_stopPrice = close - atr * RiskMultiplier;
			_takePrice = close + atr * TargetMultiplier;
		}
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _takePrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}
