using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;
/// <summary>
/// Strategy based on HMA crossover with curvature filter and ATR trailing stop.
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
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevFastChange;
	private bool _hasPrevChange;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _longTrailDist;
	private decimal _shortTrailDist;
	/// <summary>
	/// Fast HMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	/// <summary>
	/// Slow HMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	/// <summary>
	/// Risk percent per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}
	/// <summary>
	/// ATR multiplier for risk sizing.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}
	/// <summary>
	/// ATR multiplier for trailing stop distance.
	/// </summary>
	public decimal TrailMultiplier
	{
		get => _trailMultiplier.Value;
		set => _trailMultiplier.Value = value;
	}
	/// <summary>
	/// Minimum curvature for trades.
	/// </summary>
	public decimal CurvatureThreshold
	{
		get => _curvatureThreshold.Value;
		set => _curvatureThreshold.Value = value;
	}
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="HmaCrossoverAtrCurvatureStrategy"/>.
	/// </summary>
	public HmaCrossoverAtrCurvatureStrategy()
	{
		_fastLength = Param(nameof(FastLength), 15)
		.SetDisplay("Fast HMA", "Fast HMA period", "Indicators");
		_slowLength = Param(nameof(SlowLength), 34)
		.SetDisplay("Slow HMA", "Slow HMA period", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14)
		.SetDisplay("ATR Length", "ATR period", "Indicators");
		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetDisplay("Risk %", "Risk percent of equity", "Risk")
		.SetGreaterThanZero();
		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetDisplay("ATR Mult", "ATR multiplier for stop", "Risk")
		.SetGreaterThanZero();
		_trailMultiplier = Param(nameof(TrailMultiplier), 1m)
		.SetDisplay("Trail Mult", "ATR multiplier for trailing", "Risk")
		.SetGreaterThanZero();
		_curvatureThreshold = Param(nameof(CurvatureThreshold), 0m)
		.SetDisplay("Curvature Thresh", "Min acceleration", "Signals");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevFastChange = 0m;
		_hasPrevChange = false;
		_longStop = 0m;
		_shortStop = 0m;
		_longTrailDist = 0m;
		_shortTrailDist = 0m;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
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
		StartProtection();
	}
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		var fastChange = fast - _prevFast;
		var curvature = fastChange - _prevFastChange;
		var bullish = _hasPrevChange && _prevFast < _prevSlow && fast > slow && curvature > CurvatureThreshold;
		var bearish = _hasPrevChange && _prevFast > _prevSlow && fast < slow && curvature < -CurvatureThreshold;
		if (bullish && Position <= 0)
		{
			var volume = CalculateQty(atr);
			if (volume > 0m)
			{
				BuyMarket(volume + Math.Abs(Position));
				_longTrailDist = atr * TrailMultiplier;
				_longStop = candle.ClosePrice - _longTrailDist;
				_shortStop = 0m;
				_shortTrailDist = 0m;
			}
		}
		else if (bearish && Position >= 0)
		{
			var volume = CalculateQty(atr);
			if (volume > 0m)
			{
				SellMarket(volume + Math.Abs(Position));
				_shortTrailDist = atr * TrailMultiplier;
				_shortStop = candle.ClosePrice + _shortTrailDist;
				_longStop = 0m;
				_longTrailDist = 0m;
			}
		}
		if (Position > 0 && _longTrailDist > 0m)
		{
			var candidate = candle.HighPrice - _longTrailDist;
			if (candidate > _longStop)
			_longStop = candidate;
			if (candle.ClosePrice <= _longStop)
			{
				SellMarket(Math.Abs(Position));
				_longStop = 0m;
				_longTrailDist = 0m;
			}
		}
		else if (Position < 0 && _shortTrailDist > 0m)
		{
			var candidate = candle.LowPrice + _shortTrailDist;
			if (_shortStop == 0m || candidate < _shortStop)
			_shortStop = candidate;
			if (candle.ClosePrice >= _shortStop)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = 0m;
				_shortTrailDist = 0m;
			}
		}
		_prevFastChange = fastChange;
		_prevFast = fast;
		_prevSlow = slow;
		_hasPrevChange = true;
	}
	private decimal CalculateQty(decimal atr)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * RiskPercent / 100m;
		var stopLoss = atr * AtrMultiplier;
		return stopLoss > 0m ? riskAmount / stopLoss : 0m;
	}
}
