using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily SMA strategy comparing moving average to open price.
/// Opens position when the moving average crosses daily open in trend direction.
/// </summary>
public class JsMaDayStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
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
	public JsMaDayStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "SMA period on daily candles", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse Signals", "Reverse entry direction", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for moving average", "General");
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

		var sma = new SimpleMovingAverage
		{
			Length = MaPeriod,
			CandlePrice = CandlePrice.Median,
		};

		var subscription = SubscribeCandles(CandleType);

		decimal? prevMa = null;
		decimal? prevPrevMa = null;
		decimal? prevOpen = null;
		decimal? prevPrevOpen = null;

		subscription
			.Bind(sma, (candle, ma) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var open = candle.OpenPrice;

				if (prevMa is decimal pMa && prevPrevMa is decimal ppMa && prevOpen is decimal pOpen && prevPrevOpen is decimal ppOpen)
				{
					var buyCondition = ma < pMa && ma > open && pMa < ppMa && pMa > pOpen;
					var sellCondition = ma > pMa && ma < open && pMa > ppMa && pMa < pOpen;

					if (buyCondition)
					{
						if (!Reverse)
						{
							if (Position <= 0)
								BuyMarket(Volume + Math.Abs(Position));
						}
						else
						{
							if (Position >= 0)
								SellMarket(Volume + Math.Abs(Position));
						}
					}
					else if (sellCondition)
					{
						if (!Reverse)
						{
							if (Position >= 0)
								SellMarket(Volume + Math.Abs(Position));
						}
						else
						{
							if (Position <= 0)
								BuyMarket(Volume + Math.Abs(Position));
						}
					}
				}

				prevPrevMa = prevMa;
				prevMa = ma;
				prevPrevOpen = prevOpen;
				prevOpen = open;
			})
			.Start();

		StartProtection();
	}
}
