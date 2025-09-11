using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR with moving average crossovers and fixed risk reward exits.
/// </summary>
public class TrendFollowingParabolicBuySellStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initial SAR acceleration factor.
	/// </summary>
	public decimal SarStart
	{
		get => _sarStart.Value;
		set => _sarStart.Value = value;
	}

	/// <summary>
	/// SAR increment step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration factor.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Trend moving average length.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Risk reward ratio for exits.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TrendFollowingParabolicBuySellStrategy()
	{
		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Start", "Initial acceleration", "Parabolic SAR");
		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Increment step", "Parabolic SAR");
		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration", "Parabolic SAR");

		_trendLength = Param(nameof(TrendLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Trendline period", "Moving Averages");
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Moving Averages");
		_slowLength = Param(nameof(SlowLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Moving Averages");

		_riskReward = Param(nameof(RiskReward), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Take profit to stop ratio", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for strategy", "General");
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

		var psar = new ParabolicSar
		{
			AfStart = SarStart,
			AfStep = SarStep,
			AfMax = SarMax
		};
		var trendMa = new SimpleMovingAverage { Length = TrendLength };
		var fastMa = new ExponentialMovingAverage { Length = FastLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		decimal stopLevel = 0m;
		decimal takeProfit = 0m;
		decimal prevFast = 0m;
		decimal prevSlow = 0m;
		bool inTrade = false;

		subscription
			.Bind(psar, trendMa, fastMa, slowMa, (candle, sar, trend, fast, slow) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var direction = sar < candle.ClosePrice ? 1 : 0;

				if (!inTrade)
				{
					if (candle.ClosePrice > trend && prevFast <= prevSlow && fast > slow && direction == 1 && Position <= 0)
					{
						var qty = Volume + Math.Abs(Position);
						BuyMarket(qty);
						stopLevel = trend;
						takeProfit = candle.ClosePrice + (candle.ClosePrice - trend) * RiskReward;
						inTrade = true;
					}
					else if (candle.ClosePrice < trend && prevFast >= prevSlow && fast < slow && direction == 0 && Position >= 0)
					{
						var qty = Volume + Math.Abs(Position);
						SellMarket(qty);
						stopLevel = trend;
						takeProfit = candle.ClosePrice - (trend - candle.ClosePrice) * RiskReward;
						inTrade = true;
					}
				}
				else
				{
					if (Position > 0)
					{
						if (candle.LowPrice <= stopLevel || candle.HighPrice >= takeProfit)
						{
							SellMarket(Math.Abs(Position));
							inTrade = false;
						}
					}
					else if (Position < 0)
					{
						if (candle.HighPrice >= stopLevel || candle.LowPrice <= takeProfit)
						{
							BuyMarket(Math.Abs(Position));
							inTrade = false;
						}
					}
				}

				prevFast = fast;
				prevSlow = slow;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trendMa);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, psar);
			DrawOwnTrades(area);
		}
	}
}
