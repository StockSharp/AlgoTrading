using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Directed Movement Strategy - contrarian RSI cross system.
/// </summary>
public class DirectedMovementStrategy : Strategy
{
	/// <summary>
	/// Types of moving averages available for smoothing.
	/// </summary>
	public enum MaTypes
	{
		SMA,
		EMA,
		SMMA,
		WMA,
		HMA,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<MaTypes> _firstMaType;
	private readonly StrategyParam<int> _firstMaLength;
	private readonly StrategyParam<MaTypes> _secondMaType;
	private readonly StrategyParam<int> _secondMaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private RelativeStrengthIndex _rsi = null!;
	private LengthIndicator<decimal> _fastMa = null!;
	private LengthIndicator<decimal> _slowMa = null!;
	private decimal _prevFast;
	private decimal _prevSlow;

	public DirectedMovementStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "RSI calculation period", "General")
				.SetCanOptimize(true);

		_firstMaType = Param(nameof(FirstMaType), MaTypes.SMA)
				.SetDisplay("Fast MA Type", "Moving average for fast line", "General");

		_firstMaLength = Param(nameof(FirstMaLength), 12)
				.SetGreaterThanZero()
				.SetDisplay("Fast MA Length", "Period of fast moving average", "General")
				.SetCanOptimize(true);

		_secondMaType = Param(nameof(SecondMaType), MaTypes.EMA)
				.SetDisplay("Slow MA Type", "Moving average for slow line", "General");

		_secondMaLength = Param(nameof(SecondMaLength), 5)
				.SetGreaterThanZero()
				.SetDisplay("Slow MA Length", "Period of slow moving average", "General")
				.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
				.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
				.SetDisplay("Take Profit %", "Take profit in percent", "Risk");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public MaTypes FirstMaType { get => _firstMaType.Value; set => _firstMaType.Value = value; }
	public int FirstMaLength { get => _firstMaLength.Value; set => _firstMaLength.Value = value; }
	public MaTypes SecondMaType { get => _secondMaType.Value; set => _secondMaType.Value = value; }
	public int SecondMaLength { get => _secondMaLength.Value; set => _secondMaLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_fastMa = CreateMa(FirstMaType, FirstMaLength);
		_slowMa = CreateMa(SecondMaType, SecondMaLength);
		_prevFast = 0m;
		_prevSlow = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fast = _fastMa.Process(rsiValue, candle.OpenTime, true).ToDecimal();
		var slow = _slowMa.Process(fast, candle.OpenTime, true).ToDecimal();

		if (!_slowMa.IsFormed)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast > _prevSlow && fast <= slow)
		{
			if (Position < 0)
				BuyMarket(-Position); // close short
			BuyMarket(); // open long
		}
		else if (_prevFast < _prevSlow && fast >= slow)
		{
			if (Position > 0)
				SellMarket(Position); // close long
			SellMarket(); // open short
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private static LengthIndicator<decimal> CreateMa(MaTypes type, int length)
		=> type switch
		{
			MaTypes.EMA => new ExponentialMovingAverage { Length = length },
			MaTypes.SMMA => new SmoothedMovingAverage { Length = length },
			MaTypes.WMA => new WeightedMovingAverage { Length = length },
			MaTypes.HMA => new HullMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
}
