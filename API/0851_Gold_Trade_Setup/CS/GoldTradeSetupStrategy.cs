using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Kaufman Adaptive Moving Average with SuperTrend.
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

	private decimal _prevAma;
	private bool _prevIsUpTrend;
	private decimal _entryPrice;
	private decimal _targetPrice;
	private decimal _stopPrice;
	private bool _isFirst;

	public int AmaLength { get => _amaLength.Value; set => _amaLength.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public decimal TargetMultiplier { get => _targetMultiplier.Value; set => _targetMultiplier.Value = value; }
	public decimal RiskMultiplier { get => _riskMultiplier.Value; set => _riskMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GoldTradeSetupStrategy()
	{
		_amaLength = Param(nameof(AmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "Lookback period for AMA", "AMA")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_fastLength = Param(nameof(FastLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period for AMA", "AMA")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period for AMA", "AMA")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_factor = Param(nameof(Factor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "Multiplier for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_targetMultiplier = Param(nameof(TargetMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Target Mult", "Multiplier for target level", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_riskMultiplier = Param(nameof(RiskMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Mult", "Multiplier for stop level", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

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

		_prevAma = 0m;
		_prevIsUpTrend = false;
		_entryPrice = 0m;
		_targetPrice = 0m;
		_stopPrice = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaLength,
			FastSCPeriod = FastLength,
			SlowSCPeriod = SlowLength
		};

		var supertrend = new SuperTrend
		{
			Length = AtrPeriod,
			Multiplier = Factor
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(ama, supertrend, ProcessCandle)
			.Start();

		StartProtection(new Unit(0), new Unit(0), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ama);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue amaVal, IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (amaVal is not { IsFinal: true, Value: decimal ama })
			return;

		var st = (SuperTrendIndicatorValue)stVal;
		var isUpTrend = st.IsUpTrend;
		var amaTrendUp = ama > _prevAma;

		if (_isFirst)
		{
			_prevAma = ama;
			_prevIsUpTrend = isUpTrend;
			_isFirst = false;
			return;
		}

		var amaGreen = amaTrendUp && candle.ClosePrice > ama;
		var amaRed = !amaTrendUp && candle.ClosePrice < ama;
		var upTrendCross = isUpTrend && !_prevIsUpTrend;
		var downTrendCross = !isUpTrend && _prevIsUpTrend;

		if (amaGreen && upTrendCross && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = candle.ClosePrice;
			_targetPrice = _entryPrice - (_entryPrice - candle.LowPrice) * TargetMultiplier;
			_stopPrice = _entryPrice + (candle.HighPrice - _entryPrice) * RiskMultiplier;
			SellMarket(volume);
		}
		else if (amaRed && downTrendCross && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = candle.ClosePrice;
			_targetPrice = _entryPrice + (candle.HighPrice - _entryPrice) * TargetMultiplier;
			_stopPrice = _entryPrice - (_entryPrice - candle.LowPrice) * RiskMultiplier;
			BuyMarket(volume);
		}
		else if (Position > 0)
		{
			if (candle.HighPrice >= _targetPrice || candle.LowPrice <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				_entryPrice = _targetPrice = _stopPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice <= _targetPrice || candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = _targetPrice = _stopPrice = 0m;
			}
		}

		_prevAma = ama;
		_prevIsUpTrend = isUpTrend;
	}
}
