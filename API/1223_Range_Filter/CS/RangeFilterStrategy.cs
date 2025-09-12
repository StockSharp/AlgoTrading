using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
/// <summary>
/// Range filter trend strategy with fixed risk and reward.
/// </summary>
public class RangeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _riskPoints;
	private readonly StrategyParam<decimal> _rewardPoints;
	private readonly StrategyParam<bool> _useRealisticEntry;
	private readonly StrategyParam<decimal> _spreadBuffer;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _avrng;
	private decimal _smrng;
	private decimal? _prevSrc;
	private decimal _filt;
	private decimal _upward;
	private decimal _downward;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	public decimal RiskPoints
	{
		get => _riskPoints.Value;
		set => _riskPoints.Value = value;
	}
	public decimal RewardPoints
	{
		get => _rewardPoints.Value;
		set => _rewardPoints.Value = value;
	}
	public bool UseRealisticEntry
	{
		get => _useRealisticEntry.Value;
		set => _useRealisticEntry.Value = value;
	}
	public decimal SpreadBuffer
	{
		get => _spreadBuffer.Value;
		set => _spreadBuffer.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public RangeFilterStrategy()
	{
		_period = Param(nameof(Period), 100)
		.SetGreaterThanZero()
		.SetDisplay("Sample Period", "Length for smoothing", "Range Filter");
		_multiplier = Param(nameof(Multiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Multiplier", "Range multiplier", "Range Filter");
		_riskPoints = Param(nameof(RiskPoints), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Points", "Stop loss distance", "Risk Management");
		_rewardPoints = Param(nameof(RewardPoints), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Reward Points", "Take profit distance", "Risk Management");
		_useRealisticEntry = Param(nameof(UseRealisticEntry), true)
		.SetDisplay("Use HL2 Entry", "Use HL2 instead of close", "Execution");
		_spreadBuffer = Param(nameof(SpreadBuffer), 2m)
		.SetDisplay("Spread Buffer", "Additional price buffer", "Execution");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_avrng = 0m;
		_smrng = 0m;
		_prevSrc = null;
			_filt = 0m;
		_upward = 0m;
		_downward = 0m;
		_longStop = _longTarget = _shortStop = _shortTarget = null;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		var src = candle.ClosePrice;
		if (_prevSrc is null)
		{
			_prevSrc = src;
			_filt = src;
			return;
		}
		var absDiff = Math.Abs(src - _prevSrc.Value);
		var k1 = 2m / (Period + 1m);
		_avrng += (absDiff - _avrng) * k1;
		var wper = Period * 2 - 1;
		var k2 = 2m / (wper + 1m);
		_smrng += (_avrng - _smrng) * k2;
		var smrng = _smrng * Multiplier;
		var prevFilt = _filt;
		if (src > prevFilt)
			_filt = src - smrng < prevFilt ? prevFilt : src - smrng;
		else
			_filt = src + smrng > prevFilt ? prevFilt : src + smrng;
		_upward = _filt > prevFilt ? _upward + 1 : _filt < prevFilt ? 0 : _upward;
		_downward = _filt < prevFilt ? _downward + 1 : _filt > prevFilt ? 0 : _downward;
		_prevSrc = src;
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		if (Position > 0)
		{
			if ((_longStop.HasValue && candle.LowPrice <= _longStop.Value) ||
			(_longTarget.HasValue && candle.HighPrice >= _longTarget.Value))
			{
				SellMarket(Position);
				_longStop = _longTarget = null;
			}
		}
		else if (Position < 0)
		{
			if ((_shortStop.HasValue && candle.HighPrice >= _shortStop.Value) ||
			(_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value))
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = _shortTarget = null;
			}
		}
		else
		{
			var longCond = src > _filt && _upward > 0;
			var shortCond = src < _filt && _downward > 0;
			if (longCond)
			{
				var entryPrice = UseRealisticEntry ? (candle.HighPrice + candle.LowPrice) / 2m : src;
				entryPrice += SpreadBuffer;
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_longStop = entryPrice - RiskPoints;
				_longTarget = entryPrice + RewardPoints;
			}
			else if (shortCond)
			{
				var entryPrice = UseRealisticEntry ? (candle.HighPrice + candle.LowPrice) / 2m : src;
				entryPrice -= SpreadBuffer;
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_shortStop = entryPrice + RiskPoints;
				_shortTarget = entryPrice - RewardPoints;
			}
		}
	}
}
