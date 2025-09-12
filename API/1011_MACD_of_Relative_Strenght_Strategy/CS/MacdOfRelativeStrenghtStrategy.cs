using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that applies MACD to relative strength.
/// </summary>
public class MacdOfRelativeStrenghtStrategy : Strategy
{
	private readonly StrategyParam<int> _rsLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Relative strength length.
	/// </summary>
	public int RsLength
	{
		get => _rsLength.Value;
		set => _rsLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MacdOfRelativeStrenghtStrategy"/>.
	/// </summary>
	public MacdOfRelativeStrenghtStrategy()
	{
		_rsLength = Param(nameof(RsLength), 300)
			.SetGreaterThanZero()
			.SetDisplay("RS Length", "Relative strength length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 100);

		_fastLength = Param(nameof(FastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "MACD fast EMA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 20, 2);

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "MACD slow EMA length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "MACD signal smoothing", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_stopLossPercent = Param(nameof(StopLossPercent), 8m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Max risk per trade", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var highest = new Highest { Length = RsLength };

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(highest, (candle, highestValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (highestValue == 0)
					return;

				var rs = candle.ClosePrice / highestValue;

				var macdValue = macd.Process(rs, candle.CloseTime, true);
				var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

				if (typed.Macd is not decimal macdLine ||
						typed.Signal is not decimal signal)
					return;

				var hist = macdLine - signal;

				if (Position > 0 && hist < 0)
				{
					SellMarket();
					return;
				}

				if (hist > 0 && Position <= 0)
				{
					BuyMarket();
				}
			})
			.Start();

		StartProtection(
				takeProfit: new Unit(0),
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}
}
