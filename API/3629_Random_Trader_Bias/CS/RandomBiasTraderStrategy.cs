using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Random trade generator with ATR-based risk management.
/// Opens a random long or short position on each candle when flat,
/// with stop loss and take profit based on ATR.
/// </summary>
public class RandomBiasTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;

	private Random _random;
	private decimal _entryPrice;
	private int _direction; // 1=long, -1=short, 0=flat

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal RewardRiskRatio
	{
		get => _rewardRiskRatio.Value;
		set => _rewardRiskRatio.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public RandomBiasTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "Data");

		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 3m)
			.SetDisplay("Reward/Risk", "Reward to risk ratio", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop distance", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR indicator period", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = new Random(42);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0)
			return;

		var stopDistance = atrValue * AtrMultiplier;
		var takeDistance = stopDistance * RewardRiskRatio;
		var close = candle.ClosePrice;

		// Check exit for existing position
		if (_direction > 0)
		{
			if (close >= _entryPrice + takeDistance || close <= _entryPrice - stopDistance)
			{
				SellMarket();
				_direction = 0;
			}
			return;
		}
		else if (_direction < 0)
		{
			if (close <= _entryPrice - takeDistance || close >= _entryPrice + stopDistance)
			{
				BuyMarket();
				_direction = 0;
			}
			return;
		}

		// Open new random position
		if (_random.Next(4) != 0)
			return;

		if (_random.Next(2) == 0)
		{
			BuyMarket();
			_entryPrice = close;
			_direction = 1;
		}
		else
		{
			SellMarket();
			_entryPrice = close;
			_direction = -1;
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_random = null;
		_entryPrice = 0;
		_direction = 0;

		base.OnReseted();
	}
}
