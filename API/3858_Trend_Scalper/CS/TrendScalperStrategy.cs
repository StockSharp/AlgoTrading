using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Scalper conversion from Currencyprofits_01_1.mq4.
/// </summary>
public class TrendScalperStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _moneyManagementMode;
	private readonly StrategyParam<decimal> _moneyManagementRisk;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _breakoutWindow;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fastEma = null!;
	private SMA _slowSma = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;

	/// <summary>
	/// Fixed volume used when money management is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Money management mode: 0 = fixed volume, negative = fractional lots, positive = rounded lots.
	/// </summary>
	public int MoneyManagementMode
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Risk factor used when money management is enabled.
	/// </summary>
	public decimal MoneyManagementRisk
	{
		get => _moneyManagementRisk.Value;
		set => _moneyManagementRisk.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Breakout window for recent highs and lows.
	/// </summary>
	public int BreakoutWindow
	{
		get => _breakoutWindow.Value;
		set => _breakoutWindow.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrendScalperStrategy"/>.
	/// </summary>
	public TrendScalperStrategy()
	{
		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Fixed Volume", "Lots used when money management is disabled", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss (points)", "Distance from entry to stop loss in price points", "Risk");

		_moneyManagementMode = Param(nameof(MoneyManagementMode), 0)
			.SetDisplay("Money Management Mode", "0=fixed, <0=fractional, >0=rounded lots", "Risk");

		_moneyManagementRisk = Param(nameof(MoneyManagementRisk), 40m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Risk Factor", "Risk multiplier used in the lot calculation", "Risk");

		_fastLength = Param(nameof(FastLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA", "Indicator");

		_slowLength = Param(nameof(SlowLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length of the slow SMA", "Indicator");

		_breakoutWindow = Param(nameof(BreakoutWindow), 6)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Window", "Number of candles for high/low breakout", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used for calculations", "General");
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

		_fastEma = null!;
		_slowSma = null!;
		_highest = null!;
		_lowest = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_fastEma = new EMA { Length = FastLength };
		_slowSma = new SMA { Length = SlowLength };
		_highest = new Highest { Length = BreakoutWindow, CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = BreakoutWindow, CandlePrice = CandlePrice.Low };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowSma, _highest, _lowest, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _fastEma);
			DrawIndicator(priceArea, _slowSma);
		}

		if (StopLossPoints > 0m && Security?.PriceStep is decimal step && step > 0m)
		{
			// Attach the platform-managed stop-loss once at start-up.
			StartProtection(stopLoss: new Unit(StopLossPoints * step, UnitTypes.Absolute));
		}

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastEma.IsFormed || !_slowSma.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Close long positions when price reaches the recent high band.
		if (Position > 0 && candle.HighPrice >= highestValue)
		{
			SellMarket(Position);
			return;
		}

		// Close short positions when price touches the recent low band.
		if (Position < 0 && candle.LowPrice <= lowestValue)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		if (Position != 0)
			return;

		var orderVolume = CalculateOrderVolume();
		if (orderVolume <= 0m)
			return;

		// Enter long when the fast EMA crosses above the slow SMA and price flushes into the lower band.
		if (fastValue > slowValue && candle.LowPrice <= lowestValue)
		{
			BuyMarket(orderVolume);
			return;
		}

		// Enter short when the fast EMA is below the slow SMA and price spikes into the upper band.
		if (fastValue < slowValue && candle.HighPrice >= highestValue)
		{
			SellMarket(orderVolume);
		}
	}

	private decimal CalculateOrderVolume()
	{
		var mode = MoneyManagementMode;
		if (mode == 0)
		{
			// Fixed mode mimics the original LotsIfNoMM parameter.
			return FixedVolume;
		}

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
		{
			// Fall back to the fixed volume if the portfolio value is unavailable.
			return FixedVolume;
		}

		var riskVolume = Math.Ceiling(balance * MoneyManagementRisk / 10000m) / 10m;

		if (mode > 0)
		{
			if (riskVolume < 1m)
			{
				riskVolume = 1m;
			}
			else
			{
				riskVolume = Math.Ceiling(riskVolume);
			}
		}

		if (riskVolume > 100m)
		{
			riskVolume = 100m;
		}

		return riskVolume;
	}
}
