using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HMA Crossover ATR Curvature strategy.
/// Goes long when the fast HMA crosses above the slow HMA while the curvature (second difference) of the fast HMA is above
/// CurvatureThreshold, and short on the opposite cross with curvature below -CurvatureThreshold, reversing an opposite position.
/// The order size risks RiskPercent of the portfolio over a distance of AtrMultiplier ATRs, and an ATR trailing stop of
/// TrailMultiplier ATRs closes the position.
/// </summary>
public class HmaCrossoverAtrCurvatureStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _trailMultiplier;
	private readonly StrategyParam<decimal> _curvatureThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevFast2;
	private decimal? _prevSlow;
	private decimal? _trailStop;

	/// <summary>
	/// Fast HMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow HMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Percent of the portfolio risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// ATR multiple used as the risk distance for position sizing.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiple of the trailing stop distance.
	/// </summary>
	public decimal TrailMultiplier
	{
		get => _trailMultiplier.Value;
		set => _trailMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum curvature of the fast HMA.
	/// </summary>
	public decimal CurvatureThreshold
	{
		get => _curvatureThreshold.Value;
		set => _curvatureThreshold.Value = value;
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
	public HmaCrossoverAtrCurvatureStrategy()
	{
		_fastLength = Param(nameof(FastLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast HMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow HMA length", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percent of the portfolio risked per trade", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple used as risk distance for sizing", "Risk");

		_trailMultiplier = Param(nameof(TrailMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Multiplier", "ATR multiple of the trailing stop distance", "Risk");

		_curvatureThreshold = Param(nameof(CurvatureThreshold), 0m)
			.SetDisplay("Curvature Threshold", "Minimum curvature of the fast HMA", "Signals");

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
		_prevFast = null;
		_prevFast2 = null;
		_prevSlow = null;
		_trailStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastHma = new HullMovingAverage { Length = FastLength };
		var slowHma = new HullMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastHma, slowHma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastHma);
			DrawIndicator(area, slowHma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevFast2 = _prevFast2;
		var prevSlow = _prevSlow;

		_prevFast2 = _prevFast;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevFast2 is not decimal pf2 || prevSlow is not decimal ps || atr <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trailDistance = atr * TrailMultiplier;

		if (Position > 0)
		{
			var stop = candle.ClosePrice - trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Max(s, stop) : stop;

			if (candle.LowPrice <= _trailStop)
			{
				SellMarket(Position);
				_trailStop = null;
				return;
			}
		}
		else if (Position < 0)
		{
			var stop = candle.ClosePrice + trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Min(s, stop) : stop;

			if (candle.HighPrice >= _trailStop)
			{
				BuyMarket(-Position);
				_trailStop = null;
				return;
			}
		}

		var curvature = fast - 2m * pf + pf2;
		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		if (crossUp && curvature > CurvatureThreshold && Position <= 0)
		{
			BuyMarket(GetOrderVolume(atr) + Math.Abs(Position));
			_trailStop = candle.ClosePrice - trailDistance;
		}
		else if (crossDown && curvature < -CurvatureThreshold && Position >= 0)
		{
			SellMarket(GetOrderVolume(atr) + Math.Abs(Position));
			_trailStop = candle.ClosePrice + trailDistance;
		}
	}

	private decimal GetOrderVolume(decimal atr)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskDistance = atr * AtrMultiplier;

		if (equity <= 0 || riskDistance <= 0)
			return Volume;

		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var volume = Math.Floor(equity * RiskPercent / 100m / riskDistance / step) * step;
		return volume > 0 ? volume : Volume;
	}
}
