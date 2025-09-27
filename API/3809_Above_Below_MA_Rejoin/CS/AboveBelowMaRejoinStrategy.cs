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
/// Trend-following strategy converted from the MetaTrader expert advisor "AboveBelowMA".
/// Buys when price trades below a rising EMA and sells when price trades above a falling EMA.
/// </summary>
public class AboveBelowMaRejoinStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _useDynamicVolume;
	private readonly StrategyParam<decimal> _balanceToVolumeDivider;
	private readonly StrategyParam<decimal> _maxVolume;

	private ExponentialMovingAverage _ema = null!;
	private decimal? _previousEmaValue;
	private decimal _cachedPriceStep;

	/// <summary>
	/// Initializes a new instance of <see cref="AboveBelowMaRejoinStrategy"/>.
	/// </summary>
	public AboveBelowMaRejoinStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 1)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Period of the exponential moving average", "Moving Average");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signal calculation", "General");

		_initialVolume = Param(nameof(InitialVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Initial Volume", "Fallback order volume used when dynamic sizing is disabled", "Money Management");

		_useDynamicVolume = Param(nameof(UseDynamicVolume), true)
		.SetDisplay("Use Dynamic Volume", "Derive order size from portfolio value", "Money Management");

		_balanceToVolumeDivider = Param(nameof(BalanceToVolumeDivider), 10000m)
		.SetGreaterThanZero()
		.SetDisplay("Balance Divider", "Portfolio value divider that emulates the MetaTrader lot formula", "Money Management");

		_maxVolume = Param(nameof(MaxVolume), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Max Volume", "Upper cap applied to the calculated order size", "Money Management");
	}

	/// <summary>
	/// EMA period used by the signal.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Candle type that feeds the indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fallback order volume when dynamic sizing is disabled.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Enables or disables portfolio-based sizing.
	/// </summary>
	public bool UseDynamicVolume
	{
		get => _useDynamicVolume.Value;
		set => _useDynamicVolume.Value = value;
	}

	/// <summary>
	/// Divider applied to the portfolio value to emulate the MetaTrader formula.
	/// </summary>
	public decimal BalanceToVolumeDivider
	{
		get => _balanceToVolumeDivider.Value;
		set => _balanceToVolumeDivider.Value = value;
	}

	/// <summary>
	/// Maximum order volume allowed by the sizing block.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
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

		_previousEmaValue = null;
		_cachedPriceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cachedPriceStep = GetPriceStep();

		_ema = new ExponentialMovingAverage
		{
			Length = EmaLength,
			CandlePrice = CandlePrice.Typical,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_previousEmaValue is null)
		_previousEmaValue = emaValue;

		if (!_ema.IsFormed)
		{
			_previousEmaValue = emaValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousEmaValue = emaValue;
			return;
		}

		var priceStep = _cachedPriceStep > 0m ? _cachedPriceStep : GetPriceStep();
		var offset = priceStep > 0m ? priceStep : 0m;

		var openPrice = candle.OpenPrice;
		var closePrice = candle.ClosePrice;
		var previousEma = _previousEmaValue ?? emaValue;

		var shouldBuy = openPrice < emaValue - offset && closePrice < emaValue && previousEma < emaValue;
		var shouldSell = openPrice > emaValue + offset && closePrice > emaValue && previousEma > emaValue;

		if (shouldBuy)
		{
			if (Position < 0m)
			{
				// Close short positions before opening a new long cycle.
				ClosePosition();
			}
			else if (Position == 0m)
			{
				var volume = GetTradeVolume();
				if (volume > 0m)
				{
					// Enter a long position after confirming the bullish bias.
					BuyMarket(volume);
				}
			}
		}
		else if (shouldSell)
		{
			if (Position > 0m)
			{
				// Close long positions before opening a new short cycle.
				ClosePosition();
			}
			else if (Position == 0m)
			{
				var volume = GetTradeVolume();
				if (volume > 0m)
				{
					// Enter a short position after confirming the bearish bias.
					SellMarket(volume);
				}
			}
		}

		_previousEmaValue = emaValue;
	}

	private decimal GetTradeVolume()
	{
		var volume = InitialVolume;

		if (UseDynamicVolume)
		{
			var balance = GetPortfolioValue();
			if (balance > 0m && BalanceToVolumeDivider > 0m)
			volume = balance / BalanceToVolumeDivider;
		}

		if (MaxVolume > 0m)
		volume = Math.Min(volume, MaxVolume);

		return AlignVolume(volume);
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
		return 0m;

		if (portfolio.CurrentValue > 0m)
		return portfolio.CurrentValue;

		return portfolio.BeginValue;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var min = security.MinVolume ?? 0m;
		if (min > 0m && volume < min)
		volume = min;

		var max = security.MaxVolume ?? 0m;
		if (max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0m;
	}
}

