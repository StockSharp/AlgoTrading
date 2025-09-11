using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with percentage based stop-loss and take-profit.
/// </summary>
public class PercentStopTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Fast SMA period length.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow SMA period length.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }

	/// <summary>
	/// Take-profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// The type of candles to use for calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public PercentStopTakeProfitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_stopPercent = Param(nameof(StopPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetPosition();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		var prevFast = 0m;
		var prevSlow = 0m;
		var isInitialized = false;

		subscription
			.Bind(fastMa, slowMa, (candle, fast, slow) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!isInitialized)
				{
					prevFast = fast;
					prevSlow = slow;
					isInitialized = true;
					return;
				}

				// Check exits first
				if (Position != 0)
				{
					if (_isLong)
					{
						if (candle.LowPrice <= _stopPrice || candle.ClosePrice <= _stopPrice)
						{
							SellMarket(Position);
							ResetPosition();
						}
						else if (candle.HighPrice >= _takeProfitPrice || candle.ClosePrice >= _takeProfitPrice)
						{
							SellMarket(Position);
							ResetPosition();
						}
					}
					else
					{
						if (candle.HighPrice >= _stopPrice || candle.ClosePrice >= _stopPrice)
						{
							BuyMarket(-Position);
							ResetPosition();
						}
						else if (candle.LowPrice <= _takeProfitPrice || candle.ClosePrice <= _takeProfitPrice)
						{
							BuyMarket(-Position);
							ResetPosition();
						}
					}
				}

				var wasFastBelowSlow = prevFast < prevSlow;
				var isFastAboveSlow = fast > slow;

				if (wasFastBelowSlow && isFastAboveSlow && Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					_entryPrice = candle.ClosePrice;
					_isLong = true;
					_stopPrice = _entryPrice * (1m - StopPercent / 100m);
					_takeProfitPrice = _entryPrice * (1m + TakeProfitPercent / 100m);
				}
				else if (!wasFastBelowSlow && !isFastAboveSlow && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					_entryPrice = candle.ClosePrice;
					_isLong = false;
					_stopPrice = _entryPrice * (1m + StopPercent / 100m);
					_takeProfitPrice = _entryPrice * (1m - TakeProfitPercent / 100m);
				}

				prevFast = fast;
				prevSlow = slow;
			}
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ResetPosition()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_isLong = false;
	}
}
