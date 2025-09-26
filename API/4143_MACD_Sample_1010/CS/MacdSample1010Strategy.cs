using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reimplementation of the MetaTrader expert "macd sample 1010".
/// Trades Bollinger Band breakouts with optional balance-based position scaling and pip-based exits.
/// </summary>
public class MacdSample1010Strategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTargetPips;
	private readonly StrategyParam<decimal> _lossLimitPips;
	private readonly StrategyParam<decimal> _bandDistancePips;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _lotIncrease;
	private readonly StrategyParam<bool> _oneOrderOnly;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bollinger;
	private decimal _currentVolume;
	private decimal? _initialBalancePerLot;
	private bool _tradeAllowed;

	/// <summary>
	/// Number of pips required to close a trade with profit.
	/// </summary>
	public decimal ProfitTargetPips
	{
		get => _profitTargetPips.Value;
		set => _profitTargetPips.Value = value;
	}

	/// <summary>
	/// Maximum adverse excursion in pips tolerated before closing a trade.
	/// </summary>
	public decimal LossLimitPips
	{
		get => _lossLimitPips.Value;
		set => _lossLimitPips.Value = value;
	}

	/// <summary>
	/// Extra distance in pips added to the Bollinger Bands breakout filter.
	/// </summary>
	public decimal BandDistancePips
	{
		get => _bandDistancePips.Value;
		set => _bandDistancePips.Value = value;
	}

	/// <summary>
	/// Length of the Bollinger Bands indicator.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier applied by the Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Base trading volume expressed in lots.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Enables dynamic volume scaling based on the current portfolio balance.
	/// </summary>
	public bool LotIncrease
	{
		get => _lotIncrease.Value;
		set => _lotIncrease.Value = value;
	}

	/// <summary>
	/// Restricts the system to a single open position at any time when enabled.
	/// </summary>
	public bool OneOrderOnly
	{
		get => _oneOrderOnly.Value;
		set => _oneOrderOnly.Value = value;
	}

	/// <summary>
	/// Candle type used to compute Bollinger Bands and evaluate trades.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdSample1010Strategy"/> class.
	/// </summary>
	public MacdSample1010Strategy()
	{
		_profitTargetPips = Param(nameof(ProfitTargetPips), 3m)
		.SetDisplay("Profit Target (pips)", "Number of pips required to close a profitable trade.", "Risk Management")
		.SetNotNegative();

		_lossLimitPips = Param(nameof(LossLimitPips), 20m)
		.SetDisplay("Loss Limit (pips)", "Maximum adverse movement in pips before closing the trade.", "Risk Management")
		.SetNotNegative();

		_bandDistancePips = Param(nameof(BandDistancePips), 3m)
		.SetDisplay("Band Distance (pips)", "Additional distance added to the Bollinger Band breakout threshold.", "Signal")
		.SetNotNegative();

		_bollingerPeriod = Param(nameof(BollingerPeriod), 4)
		.SetDisplay("Bollinger Period", "Number of candles used by the Bollinger Bands.", "Signal")
		.SetGreaterThanZero();

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetDisplay("Bollinger Deviation", "Standard deviation multiplier applied to the Bollinger Bands.", "Signal")
		.SetGreaterThanZero();

		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Initial trade size in lots before scaling is applied.", "Money Management")
		.SetNotNegative();

		_lotIncrease = Param(nameof(LotIncrease), true)
		.SetDisplay("Lot Increase", "Scale trade size proportionally to the portfolio balance.", "Money Management");

		_oneOrderOnly = Param(nameof(OneOrderOnly), true)
		.SetDisplay("One Order Only", "Prevent simultaneous opposing positions when enabled.", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Series used to calculate indicators and signals.", "Data");
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

		_bollinger?.Reset();
		_currentVolume = 0m;
		_initialBalancePerLot = null;
		_tradeAllowed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_currentVolume = BaseVolume;
		_initialBalancePerLot = null;
		_tradeAllowed = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Replicate the MetaTrader chart layout by plotting candles and Bollinger Bands.
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		// Work only with completed candles to mirror the original bar-close evaluation.
		if (candle.State != CandleStates.Finished)
			return;

		if (_bollinger is null || !_bollinger.IsFormed)
			return;

		UpdateDynamicVolume();
		_tradeAllowed = true;

		var offset = CalculatePriceOffset(BandDistancePips);
		var closePrice = candle.ClosePrice;
		var allowEntry = _tradeAllowed && (!OneOrderOnly || Position == 0m);
		var volume = _currentVolume;

		// Attempt new entries before managing the existing position, just like the MQL script.
		if (allowEntry && volume > 0m)
		{
			if (closePrice > upperBand + offset)
			{
				// Price closed above the upper band plus buffer -> open short position.
				SellMarket(volume);
				_tradeAllowed = false;
			}
			else if (closePrice < lowerBand - offset)
			{
				// Price closed below the lower band minus buffer -> open long position.
				BuyMarket(volume);
				_tradeAllowed = false;
			}
		}

		if (Position == 0m)
			return;

		var entryPrice = Position.AveragePrice ?? closePrice;
		var profitTarget = CalculatePriceOffset(ProfitTargetPips);
		var lossLimit = CalculatePriceOffset(LossLimitPips);

		if (Position > 0m)
		{
			var profitDistance = closePrice - entryPrice;

			if (profitTarget > 0m && profitDistance >= profitTarget)
			{
				// Close the long trade once the configured profit is reached.
				SellMarket(Position);
				return;
			}

			if (lossLimit > 0m && profitDistance <= -lossLimit)
			{
				// Close the long trade if the loss limit is exceeded.
				SellMarket(Position);
			}
		}
		else if (Position < 0m)
		{
			var profitDistance = entryPrice - closePrice;

			if (profitTarget > 0m && profitDistance >= profitTarget)
			{
				// Close the short trade once the configured profit is reached.
				BuyMarket(-Position);
				return;
			}

			if (lossLimit > 0m && profitDistance <= -lossLimit)
			{
				// Close the short trade if the loss limit is exceeded.
				BuyMarket(-Position);
			}
		}
	}

	private void UpdateDynamicVolume()
	{
		var baseVolume = BaseVolume;

		if (!LotIncrease)
		{
			_currentVolume = baseVolume;
			return;
		}

		if (baseVolume <= 0m)
		{
			_currentVolume = 0m;
			return;
		}

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (!_initialBalancePerLot.HasValue && balance.HasValue)
		{
			// Store the initial balance per lot so we can scale volume proportionally to balance changes.
			_initialBalancePerLot = balance.Value / baseVolume;
		}

		if (!_initialBalancePerLot.HasValue || _initialBalancePerLot.Value <= 0m)
		{
			_currentVolume = baseVolume;
			return;
		}

		if (balance.HasValue)
		{
			var ratio = balance.Value / _initialBalancePerLot.Value;
			ratio = Math.Max(0m, ratio);

			var normalized = Math.Round(ratio, 1, MidpointRounding.AwayFromZero);
			_currentVolume = Math.Min(normalized, 500m);
		}
		else
		{
			_currentVolume = baseVolume;
		}
	}

	private decimal CalculatePriceOffset(decimal pipCount)
	{
		if (pipCount <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep;
		if (priceStep is null || priceStep.Value <= 0m)
			return pipCount;

		return pipCount * priceStep.Value;
	}
}
