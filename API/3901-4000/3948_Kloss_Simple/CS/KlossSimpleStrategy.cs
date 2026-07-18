using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA, CCI, and Stochastic oscillator signals.
/// Converts the original Kloss expert advisor from MetaTrader 4 to StockSharp.
/// </summary>
public class KlossSimpleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSmooth;
	private readonly StrategyParam<decimal> _stochasticLevel;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _riskPercentage;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private CommodityChannelIndex _cci;
	private StochasticOscillator _stochastic;

	private decimal? _previousCci;

	/// <summary>
	/// Initializes a new instance of the <see cref="KlossSimpleStrategy"/> class.
	/// </summary>
	public KlossSimpleStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");

		_maPeriod = Param(nameof(MaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the exponential moving average", "Indicators")
			
			.SetOptimize(3, 20, 1);

		_cciPeriod = Param(nameof(CciPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Length of the commodity channel index", "Indicators")
			
			.SetOptimize(5, 30, 5);

		_cciLevel = Param(nameof(CciLevel), 200m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Distance from zero to trigger signals", "Indicators")

			.SetOptimize(50m, 200m, 10m);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
			
			.SetOptimize(3, 20, 1);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Period of the %D line", "Indicators")

			.SetOptimize(1, 10, 1);

		_stochasticSmooth = Param(nameof(StochasticSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smooth", "Smoothing factor for %K", "Indicators")
			
			.SetOptimize(1, 10, 1);

		_stochasticLevel = Param(nameof(StochasticLevel), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Level", "Distance from 50 to trigger signals", "Indicators")

			.SetOptimize(10m, 40m, 5m);

		_maxOrders = Param(nameof(MaxOrders), 1)
			.SetNotNegative()
			.SetDisplay("Max Orders", "Maximum number of positions per direction", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk");

		_riskPercentage = Param(nameof(RiskPercentage), 10m)
			.SetNotNegative()
			.SetDisplay("Risk %", "Portfolio percentage for dynamic position sizing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series for calculations", "General");
	}

	/// <summary>Base order volume for new entries.</summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>EMA length used to filter price action.</summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>CCI period for momentum detection.</summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>Absolute level that CCI must exceed to signal an entry.</summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>Stochastic %K period.</summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>Stochastic %D period.</summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>Smoothing applied to the %K line.</summary>
	public int StochasticSmooth
	{
		get => _stochasticSmooth.Value;
		set => _stochasticSmooth.Value = value;
	}

	/// <summary>Offset around 50 used for stochastic thresholds.</summary>
	public decimal StochasticLevel
	{
		get => _stochasticLevel.Value;
		set => _stochasticLevel.Value = value;
	}

	/// <summary>Maximum number of simultaneous entries per direction.</summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>Stop loss distance expressed in price points.</summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>Take profit distance expressed in price points.</summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>Portfolio percentage used to size positions dynamically.</summary>
	public decimal RiskPercentage
	{
		get => _riskPercentage.Value;
		set => _riskPercentage.Value = value;
	}

	/// <summary>Candle type used for indicator calculations.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_previousCci = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new EMA { Length = MaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_stochastic = new StochasticOscillator();
		_stochastic.K.Length = StochasticKPeriod;
		_stochastic.D.Length = StochasticDPeriod;

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		Unit stopLossUnit = null;
		Unit takeProfitUnit = null;
		var priceStep = Security?.PriceStep ?? 0m;

		if (StopLossPoints > 0m && priceStep > 0m)
		{
			stopLossUnit = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);
		}

		if (TakeProfitPoints > 0m && priceStep > 0m)
		{
			takeProfitUnit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);
		}

		if (stopLossUnit != null || takeProfitUnit != null)
		{
			StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maResult = _ema.Process(candle).ToNullableDecimal();
		var cciResult = _cci.Process(candle).ToNullableDecimal();
		var stochasticValue = (StochasticOscillatorValue)_stochastic.Process(candle);

		if (maResult == null || cciResult == null)
			return;

		if (stochasticValue.K is not decimal stochasticK)
			return;

		if (!_ema.IsFormed || !_cci.IsFormed || !_stochastic.IsFormed)
			return;

		var maValue = maResult.Value;
		var cciValue = cciResult.Value;

		var lowerStochastic = 50m - StochasticLevel;
		var upperStochastic = 50m + StochasticLevel;

		// Buy signal: CCI crosses up through -level from oversold territory
		var cciBuyXover = _previousCci != null && _previousCci.Value < -CciLevel && cciValue >= -CciLevel;
		// Sell signal: CCI crosses down through +level from overbought territory
		var cciSellXover = _previousCci != null && _previousCci.Value > CciLevel && cciValue <= CciLevel;

		if (cciBuyXover && stochasticK < lowerStochastic)
		{
			CloseShortPositions();
			TryEnterLong(candle);
		}
		else if (cciSellXover && stochasticK > upperStochastic)
		{
			CloseLongPositions();
			TryEnterShort(candle);
		}

		_previousCci = cciValue;
	}

	private void CloseLongPositions()
	{
		var longVolume = Position > 0m ? Position : 0m;
		if (longVolume <= 0m)
			return;

		// Close existing long volume before reversing into short trades.
		SellMarket(longVolume);
	}

	private void CloseShortPositions()
	{
		var shortVolume = Position < 0m ? Position.Abs() : 0m;
		if (shortVolume <= 0m)
			return;

		// Close existing short volume before opening new long trades.
		BuyMarket(shortVolume);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		var currentLongVolume = Position > 0m ? Position : 0m;

		if (MaxOrders > 0)
		{
			var maxVolume = volume * MaxOrders;
			if (currentLongVolume >= maxVolume)
				return;

			var additionalVolume = volume.Min(maxVolume - currentLongVolume);
			if (additionalVolume <= 0m)
				return;

			// Add new long exposure without exceeding MaxOrders limit.
			BuyMarket(additionalVolume);
		}
		else
		{
			BuyMarket(volume);
		}
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		var currentShortVolume = Position < 0m ? Position.Abs() : 0m;

		if (MaxOrders > 0)
		{
			var maxVolume = volume * MaxOrders;
			if (currentShortVolume >= maxVolume)
				return;

			var additionalVolume = volume.Min(maxVolume - currentShortVolume);
			if (additionalVolume <= 0m)
				return;

			// Add new short exposure without exceeding MaxOrders limit.
			SellMarket(additionalVolume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	private decimal CalculateOrderVolume(decimal referencePrice)
	{
		var volume = OrderVolume;

		if (RiskPercentage > 0m)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			var riskCapital = portfolioValue * RiskPercentage / 100m;

			if (riskCapital > 0m)
			{
				var margin = GetSecurityValue<decimal?>(Level1Fields.MarginBuy) ?? GetSecurityValue<decimal?>(Level1Fields.MarginSell) ?? 0m;

				if (margin > 0m)
				{
					volume = riskCapital / margin;
				}
				else if (referencePrice > 0m)
				{
					volume = riskCapital / referencePrice;
				}
			}
		}

		volume = RoundVolume(volume);

		var minVolume = Security?.MinVolume;
		if (minVolume != null && minVolume.Value > 0m && volume < minVolume.Value)
		{
			volume = minVolume.Value;
		}

		var maxVolume = Security?.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
		{
			volume = maxVolume.Value;
		}

		return volume;
	}

	private decimal RoundVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			return volume;

		var steps = Math.Floor(volume / step);
		var rounded = steps * step;

		if (rounded <= 0m)
			rounded = step;

		return rounded;
	}
}

