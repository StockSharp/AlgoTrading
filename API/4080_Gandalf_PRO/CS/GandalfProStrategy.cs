using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gandalf PRO trend-following strategy converted from the original MetaTrader 4 expert advisor.
/// Reconstructs the adaptive smoothing filter used by the EA and opens trades when the projected price exceeds a buffer.
/// </summary>
public class GandalfProStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<int> _countBuy;
	private readonly StrategyParam<decimal> _buyPriceFactor;
	private readonly StrategyParam<decimal> _buyTrendFactor;
	private readonly StrategyParam<int> _buyStopLossPips;
	private readonly StrategyParam<decimal> _buyRiskMultiplier;

	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<int> _countSell;
	private readonly StrategyParam<decimal> _sellPriceFactor;
	private readonly StrategyParam<decimal> _sellTrendFactor;
	private readonly StrategyParam<int> _sellStopLossPips;
	private readonly StrategyParam<decimal> _sellRiskMultiplier;

	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeBuffer = new();

	private decimal _pipSize;
	private decimal _entryBuffer;
	private decimal? _longTarget;
	private decimal? _longStop;
	private decimal? _shortTarget;
	private decimal? _shortStop;

	/// <summary>
	/// Initializes a new instance of the <see cref="GandalfProStrategy"/> class.
	/// </summary>
	public GandalfProStrategy()
	{
		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long trades", "General");

		_countBuy = Param(nameof(CountBuy), 24)
			.SetGreaterThanZero()
			.SetDisplay("Buy Length", "Filter length for long projections", "Filter");

		_buyPriceFactor = Param(nameof(BuyPriceFactor), 0.18m)
			.SetDisplay("Buy Price Factor", "Weight of the close price in the long filter", "Filter");

		_buyTrendFactor = Param(nameof(BuyTrendFactor), 0.18m)
			.SetDisplay("Buy Trend Factor", "Weight of the trend term in the long filter", "Filter");

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 62)
			.SetNotNegative()
			.SetDisplay("Buy SL", "Stop-loss distance in pips for long trades", "Risk");

		_buyRiskMultiplier = Param(nameof(BuyRiskMultiplier), 0m)
			.SetDisplay("Buy Risk", "Volume multiplier applied to long entries", "Risk");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow short trades", "General");

		_countSell = Param(nameof(CountSell), 24)
			.SetGreaterThanZero()
			.SetDisplay("Sell Length", "Filter length for short projections", "Filter");

		_sellPriceFactor = Param(nameof(SellPriceFactor), 0.18m)
			.SetDisplay("Sell Price Factor", "Weight of the close price in the short filter", "Filter");

		_sellTrendFactor = Param(nameof(SellTrendFactor), 0.18m)
			.SetDisplay("Sell Trend Factor", "Weight of the trend term in the short filter", "Filter");

		_sellStopLossPips = Param(nameof(SellStopLossPips), 62)
			.SetNotNegative()
			.SetDisplay("Sell SL", "Stop-loss distance in pips for short trades", "Risk");

		_sellRiskMultiplier = Param(nameof(SellRiskMultiplier), 0m)
			.SetDisplay("Sell Risk", "Volume multiplier applied to short entries", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Default order volume when risk multipliers are zero", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe processed by the strategy", "Trading");
	}

	/// <summary>
	/// Enables or disables long-side trading.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filter used for long projections.
	/// </summary>
	public int CountBuy
	{
		get => _countBuy.Value;
		set => _countBuy.Value = value;
	}

	/// <summary>
	/// Close-price weight in the long smoothing filter.
	/// </summary>
	public decimal BuyPriceFactor
	{
		get => _buyPriceFactor.Value;
		set => _buyPriceFactor.Value = value;
	}

	/// <summary>
	/// Trend-term weight in the long smoothing filter.
	/// </summary>
	public decimal BuyTrendFactor
	{
		get => _buyTrendFactor.Value;
		set => _buyTrendFactor.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in pips.
	/// </summary>
	public int BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to long trades when the original EA requested dynamic sizing.
	/// </summary>
	public decimal BuyRiskMultiplier
	{
		get => _buyRiskMultiplier.Value;
		set => _buyRiskMultiplier.Value = value;
	}

	/// <summary>
	/// Enables or disables short-side trading.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filter used for short projections.
	/// </summary>
	public int CountSell
	{
		get => _countSell.Value;
		set => _countSell.Value = value;
	}

	/// <summary>
	/// Close-price weight in the short smoothing filter.
	/// </summary>
	public decimal SellPriceFactor
	{
		get => _sellPriceFactor.Value;
		set => _sellPriceFactor.Value = value;
	}

	/// <summary>
	/// Trend-term weight in the short smoothing filter.
	/// </summary>
	public decimal SellTrendFactor
	{
		get => _sellTrendFactor.Value;
		set => _sellTrendFactor.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in pips.
	/// </summary>
	public int SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to short trades when the original EA requested dynamic sizing.
	/// </summary>
	public decimal SellRiskMultiplier
	{
		get => _sellRiskMultiplier.Value;
		set => _sellRiskMultiplier.Value = value;
	}

	/// <summary>
	/// Base volume used when risk multipliers are set to zero.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_closeBuffer.Clear();
		_longTarget = null;
		_longStop = null;
		_shortTarget = null;
		_shortStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security is null)
			throw new InvalidOperationException("Security is not assigned.");


		var priceStep = Security.PriceStep ?? 0.0001m;
		var multiplier = Security.Decimals >= 5 ? 10m : 1m;
		_pipSize = priceStep * multiplier;
		if (_pipSize <= 0m)
		{
			_pipSize = priceStep;
		}

		_entryBuffer = 15m * _pipSize;
		Volume = BaseVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeBuffer.Add(candle.ClosePrice);
		var maxDepth = Math.Max(CountBuy, CountSell) + 2;
		while (_closeBuffer.Count > maxDepth)
		{
			_closeBuffer.RemoveAt(0);
		}

		ManageOpenPosition(candle);

		if (_closeBuffer.Count <= Math.Max(CountBuy, CountSell))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (EnableBuy)
		{
			var target = CalculateTarget(CountBuy, BuyPriceFactor, BuyTrendFactor);
			if (target is decimal targetPrice)
			{
				TryEnterLong(candle, targetPrice);
			}
		}

		if (EnableSell)
		{
			var target = CalculateTarget(CountSell, SellPriceFactor, SellTrendFactor);
			if (target is decimal targetPrice)
			{
				TryEnterShort(candle, targetPrice);
			}
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				_longStop = null;
				_longTarget = null;
				ClosePosition();
			}
			else if (_longTarget is decimal target && candle.HighPrice >= target)
			{
				_longStop = null;
				_longTarget = null;
				ClosePosition();
			}
		}
		else
		{
			_longStop = null;
			_longTarget = null;
		}

		if (Position < 0)
		{
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				_shortStop = null;
				_shortTarget = null;
				ClosePosition();
			}
			else if (_shortTarget is decimal target && candle.LowPrice <= target)
			{
				_shortStop = null;
				_shortTarget = null;
				ClosePosition();
			}
		}
		else
		{
			_shortStop = null;
			_shortTarget = null;
		}
	}

	private void TryEnterLong(ICandleMessage candle, decimal targetPrice)
	{
		if (Position > 0)
			return;

		var currentPrice = candle.ClosePrice;
		if (targetPrice <= currentPrice + _entryBuffer)
			return;

		var volume = GetVolume(BuyRiskMultiplier);
		if (volume <= 0m)
			return;

		var orderVolume = volume;
		if (Position < 0)
		{
			orderVolume += Math.Abs(Position);
		}

		if (orderVolume <= 0m)
			return;

		BuyMarket(orderVolume);

		_longTarget = targetPrice;
		_longStop = currentPrice - BuyStopLossPips * _pipSize;
		_shortTarget = null;
		_shortStop = null;
	}

	private void TryEnterShort(ICandleMessage candle, decimal targetPrice)
	{
		if (Position < 0)
			return;

		var currentPrice = candle.ClosePrice;
		if (targetPrice >= currentPrice - _entryBuffer)
			return;

		var volume = GetVolume(SellRiskMultiplier);
		if (volume <= 0m)
			return;

		var orderVolume = volume;
		if (Position > 0)
		{
			orderVolume += Math.Abs(Position);
		}

		if (orderVolume <= 0m)
			return;

		SellMarket(orderVolume);

		_shortTarget = targetPrice;
		_shortStop = currentPrice + SellStopLossPips * _pipSize;
		_longTarget = null;
		_longStop = null;
	}

	private decimal? CalculateTarget(int length, decimal priceFactor, decimal trendFactor)
	{
		if (length < 2)
			return null;

		if (_closeBuffer.Count < length + 1)
			return null;

		var n = length;
		var sum = 0m;
		for (var i = 1; i <= n; i++)
		{
			sum += GetClose(i);
		}

		var sm = sum / n;
		var weightedSum = 0m;
		for (var i = 0; i < n; i++)
		{
			var price = GetClose(i + 1);
			var weight = n - i;
			weightedSum += price * weight;
		}

		var denominator = (decimal)n * (n + 1) / 2m;
		if (denominator <= 0m)
			return null;

		var lm = weightedSum / denominator;
		var divisor = n - 1;
		if (divisor <= 0)
			return null;

		var s = new decimal[n + 2];
		var t = new decimal[n + 2];

		var tn = (6m * lm - 6m * sm) / divisor;
		var sn = 4m * sm - 3m * lm - tn;
		s[n] = sn;
		t[n] = tn;

		for (var k = n - 1; k > 0; k--)
		{
			var close = GetClose(k);
			s[k] = priceFactor * close + (1m - priceFactor) * (s[k + 1] + t[k + 1]);
			t[k] = trendFactor * (s[k] - s[k + 1]) + (1m - trendFactor) * t[k + 1];
		}

		return s[1] + t[1];
	}

	private decimal GetClose(int index)
	{
		var idx = _closeBuffer.Count - 1 - index;
		if (idx < 0)
			idx = 0;
		if (idx >= _closeBuffer.Count)
			idx = _closeBuffer.Count - 1;
		return _closeBuffer[idx];
	}

	private decimal GetVolume(decimal riskMultiplier)
	{
		var baseVolume = BaseVolume;
		if (riskMultiplier <= 0m)
			return baseVolume;
		return baseVolume * riskMultiplier;
	}
}
