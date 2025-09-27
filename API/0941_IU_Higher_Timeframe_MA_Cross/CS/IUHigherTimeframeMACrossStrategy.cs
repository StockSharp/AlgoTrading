namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class IUHigherTimeframeMACrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskToReward;
	private readonly StrategyParam<DataType> _ma1CandleType;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<MovingAverageTypeEnum> _ma1Type;
	private readonly StrategyParam<DataType> _ma2CandleType;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<MovingAverageTypeEnum> _ma2Type;

	private decimal? _ma1;
	private decimal? _ma2;
	private decimal? _prevMa1;
	private decimal? _prevMa2;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	private decimal? _prevLow;
	private decimal? _prevHigh;

	private ICandleMessage _lastMa1Candle;

	private LengthIndicator<decimal> _ma1Indicator;
	private LengthIndicator<decimal> _ma2Indicator;

	public IUHigherTimeframeMACrossStrategy()
	{
		_riskToReward = Param(nameof(RiskToReward), 2m)
		.SetGreaterThanZero()
		.SetDisplay("RTR", "Risk to reward ratio", "Protection")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

		_ma1CandleType = Param(nameof(Ma1CandleType), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("MA1 Timeframe", "Timeframe for first MA", "Moving Averages");

		_ma1Length = Param(nameof(Ma1Length), 20)
		.SetGreaterThanZero()
		.SetDisplay("MA1 Length", "Period for first MA", "Moving Averages")
		.SetCanOptimize(true)
		.SetOptimize(5, 100, 5);

		_ma1Type = Param(nameof(Ma1Type), MovingAverageTypeEnum.Exponential)
		.SetDisplay("MA1 Type", "Type of first MA", "Moving Averages");

		_ma2CandleType = Param(nameof(Ma2CandleType), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("MA2 Timeframe", "Timeframe for second MA", "Moving Averages");

		_ma2Length = Param(nameof(Ma2Length), 50)
		.SetGreaterThanZero()
		.SetDisplay("MA2 Length", "Period for second MA", "Moving Averages")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 5);

		_ma2Type = Param(nameof(Ma2Type), MovingAverageTypeEnum.Exponential)
		.SetDisplay("MA2 Type", "Type of second MA", "Moving Averages");
	}

	public decimal RiskToReward { get => _riskToReward.Value; set => _riskToReward.Value = value; }
	public DataType Ma1CandleType { get => _ma1CandleType.Value; set => _ma1CandleType.Value = value; }
	public int Ma1Length { get => _ma1Length.Value; set => _ma1Length.Value = value; }
	public MovingAverageTypeEnum Ma1Type { get => _ma1Type.Value; set => _ma1Type.Value = value; }
	public DataType Ma2CandleType { get => _ma2CandleType.Value; set => _ma2CandleType.Value = value; }
	public int Ma2Length { get => _ma2Length.Value; set => _ma2Length.Value = value; }
	public MovingAverageTypeEnum Ma2Type { get => _ma2Type.Value; set => _ma2Type.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, Ma1CandleType), (Security, Ma2CandleType)];

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1Indicator = CreateMa(Ma1Type, Ma1Length);
		_ma2Indicator = CreateMa(Ma2Type, Ma2Length);

		var ma1Sub = SubscribeCandles(Ma1CandleType);
		ma1Sub.Bind(_ma1Indicator, ProcessMa1).Start();

		var ma2Sub = SubscribeCandles(Ma2CandleType);
		ma2Sub.Bind(_ma2Indicator, ProcessMa2).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, ma1Sub);
			DrawIndicator(area, _ma1Indicator);
			DrawIndicator(area, _ma2Indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMa1(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastMa1Candle != null)
		{
			_prevLow = _lastMa1Candle.LowPrice;
			_prevHigh = _lastMa1Candle.HighPrice;
		}
		_lastMa1Candle = candle;

		_prevMa1 = _ma1;
		_ma1 = value;

		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice)
				CloseLong();
			else if (_takePrice.HasValue && candle.HighPrice >= _takePrice)
				CloseLong();
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice)
				CloseShort();
			else if (_takePrice.HasValue && candle.LowPrice <= _takePrice)
				CloseShort();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevMa1.HasValue && _prevMa2.HasValue && _ma1.HasValue && _ma2.HasValue && Position == 0)
		{
			var crossUp = _prevMa1 < _prevMa2 && _ma1 > _ma2;
			var crossDown = _prevMa1 > _prevMa2 && _ma1 < _ma2;

			if (crossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				var sl = _prevLow ?? candle.LowPrice;
				_stopPrice = sl;
				_takePrice = (_entryPrice - sl) * RiskToReward + _entryPrice;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				var sl = _prevHigh ?? candle.HighPrice;
				_stopPrice = sl;
				_takePrice = _entryPrice - (sl - _entryPrice) * RiskToReward;
			}
		}
	}

	private void ProcessMa2(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevMa2 = _ma2;
		_ma2 = value;
	}

	private void CloseLong()
	{
		SellMarket(Position);
		ResetProtection();
	}

	private void CloseShort()
	{
		BuyMarket(Math.Abs(Position));
		ResetProtection();
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private static LengthIndicator<decimal> CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	public enum MovingAverageTypeEnum
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		VolumeWeighted
	}
}