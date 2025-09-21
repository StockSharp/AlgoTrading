using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Accelerator Oscillator reversal strategy converted from the MQL5 "Executer AC" expert advisor.
/// </summary>
public class ExecuterAcStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly AcceleratorOscillator _ac = new();
	private readonly decimal[] _acHistory = new decimal[4];
	private int _acCount;

	private decimal _pipSize;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _longTrail;
	private decimal? _longEntry;

	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _shortTrail;
	private decimal? _shortEntry;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ExecuterAcStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base order volume", "Orders");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimal profit before trailing moves", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
	}

	/// <summary>
	/// Base order volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Profit threshold, in pips, that must be exceeded before the trailing stop tightens.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the Accelerator Oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_acHistory, 0, _acHistory.Length);
		_acCount = 0;
		ResetRisk();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ac, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ac);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal acValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ac.IsFormed)
			return;

		UpdateAcHistory(acValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (PositionPrice is decimal longPrice && (_longEntry is null || longPrice != _longEntry.Value))
				UpdateLongReference(longPrice, false);

			if (ShouldCloseLongByAc())
			{
				ClosePosition();
				ResetRisk();
				return;
			}

			if (CheckLongStops(candle))
				return;

			if (UpdateLongTrailing(candle))
				return;

			return;
		}

		if (Position < 0)
		{
			if (PositionPrice is decimal shortPrice && (_shortEntry is null || shortPrice != _shortEntry.Value))
				UpdateShortReference(shortPrice, false);

			if (ShouldCloseShortByAc())
			{
				ClosePosition();
				ResetRisk();
				return;
			}

			if (CheckShortStops(candle))
				return;

			if (UpdateShortTrailing(candle))
				return;

			return;
		}

		TryEnter(candle);
	}

	private void UpdateAcHistory(decimal value)
	{
		for (var i = _acHistory.Length - 1; i > 0; i--)
			_acHistory[i] = _acHistory[i - 1];

		_acHistory[0] = value;

		if (_acCount < _acHistory.Length)
			_acCount++;
	}

	private bool HasAcHistory(int count)
	{
		return _acCount >= count;
	}

	private bool ShouldCloseLongByAc()
	{
		return HasAcHistory(2) && _acHistory[0] < _acHistory[1];
	}

	private bool ShouldCloseShortByAc()
	{
		return HasAcHistory(2) && _acHistory[0] > _acHistory[1];
	}

	private bool CheckLongStops(ICandleMessage candle)
	{
		if (_longStop is decimal stop && candle.LowPrice <= stop)
		{
			ClosePosition();
			ResetRisk();
			return true;
		}

		if (_longTake is decimal take && candle.HighPrice >= take)
		{
			ClosePosition();
			ResetRisk();
			return true;
		}

		return false;
	}

	private bool CheckShortStops(ICandleMessage candle)
	{
		if (_shortStop is decimal stop && candle.HighPrice >= stop)
		{
			ClosePosition();
			ResetRisk();
			return true;
		}

		if (_shortTake is decimal take && candle.LowPrice <= take)
		{
			ClosePosition();
			ResetRisk();
			return true;
		}

		return false;
	}

	private bool UpdateLongTrailing(ICandleMessage candle)
	{
		var trailingDistance = GetPriceOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
			return false;

		var entryPrice = PositionPrice ?? _longEntry;
		if (entryPrice is not decimal entry || entry <= 0m)
			return false;

		var trailingStep = GetPriceOffset(TrailingStepPips);
		var profit = candle.ClosePrice - entry;

		if (profit > trailingDistance + trailingStep)
		{
			var candidate = NormalizePrice(candle.ClosePrice - trailingDistance);
			var minimalImprovement = trailingStep > 0m ? trailingStep : (_pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m);

			if (_longTrail is not decimal existing || candidate - existing >= minimalImprovement)
			{
				_longTrail = candidate;
				_longStop = candidate;
			}
		}

		if (_longTrail is decimal trail && candle.LowPrice <= trail)
		{
			ClosePosition();
			ResetRisk();
			return true;
		}

		return false;
	}

	private bool UpdateShortTrailing(ICandleMessage candle)
	{
		var trailingDistance = GetPriceOffset(TrailingStopPips);
		if (trailingDistance <= 0m)
			return false;

		var entryPrice = PositionPrice ?? _shortEntry;
		if (entryPrice is not decimal entry || entry <= 0m)
			return false;

		var trailingStep = GetPriceOffset(TrailingStepPips);
		var profit = entry - candle.ClosePrice;

		if (profit > trailingDistance + trailingStep)
		{
			var candidate = NormalizePrice(candle.ClosePrice + trailingDistance);
			var minimalImprovement = trailingStep > 0m ? trailingStep : (_pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m);

			if (_shortTrail is not decimal existing || existing - candidate >= minimalImprovement)
			{
				_shortTrail = candidate;
				_shortStop = candidate;
			}
		}

		if (_shortTrail is decimal trail && candle.HighPrice >= trail)
		{
			ClosePosition();
			ResetRisk();
			return true;
		}

		return false;
	}

	private void TryEnter(ICandleMessage candle)
	{
		if (!HasAcHistory(2))
			return;

		var ac1 = _acHistory[0];
		var ac2 = _acHistory[1];
		var hasAc3 = HasAcHistory(3);
		var hasAc4 = HasAcHistory(4);
		var ac3 = hasAc3 ? _acHistory[2] : 0m;
		var ac4 = hasAc4 ? _acHistory[3] : 0m;

		if (ac1 > 0m && ac2 > 0m)
		{
			if (hasAc3 && ac1 > ac2 && ac2 > ac3)
			{
				OpenLong(candle);
				return;
			}

			if (hasAc4 && ac1 < ac2 && ac2 < ac3 && ac3 < ac4)
			{
				OpenShort(candle);
				return;
			}
		}

		if (ac1 < 0m && ac2 < 0m)
		{
			if (hasAc4 && ac1 > ac2 && ac2 > ac3 && ac3 > ac4)
			{
				OpenLong(candle);
				return;
			}

			if (hasAc3 && ac1 < ac2 && ac2 < ac3)
			{
				OpenShort(candle);
				return;
			}
		}

		if (ac1 > 0m && ac2 < 0m)
		{
			OpenLong(candle);
			return;
		}

		if (ac1 < 0m && ac2 > 0m)
		{
			OpenShort(candle);
			return;
		}
	}

	private void OpenLong(ICandleMessage candle)
	{
		var volume = GetSafeVolume(TradeVolume);
		if (volume <= 0m)
			return;

		ResetRisk();
		BuyMarket(volume);

		UpdateLongReference(candle.ClosePrice, true);
	}

	private void OpenShort(ICandleMessage candle)
	{
		var volume = GetSafeVolume(TradeVolume);
		if (volume <= 0m)
			return;

		ResetRisk();
		SellMarket(volume);

		UpdateShortReference(candle.ClosePrice, true);
	}

	private void UpdateLongReference(decimal entryPrice, bool resetTrailing)
	{
		_longEntry = entryPrice;

		if (resetTrailing)
			_longTrail = null;

		var stopOffset = GetPriceOffset(StopLossPips);
		if (_longTrail is null)
			_longStop = stopOffset > 0m ? NormalizePrice(entryPrice - stopOffset) : null;

		var takeOffset = GetPriceOffset(TakeProfitPips);
		_longTake = takeOffset > 0m ? NormalizePrice(entryPrice + takeOffset) : null;
	}

	private void UpdateShortReference(decimal entryPrice, bool resetTrailing)
	{
		_shortEntry = entryPrice;

		if (resetTrailing)
			_shortTrail = null;

		var stopOffset = GetPriceOffset(StopLossPips);
		if (_shortTrail is null)
			_shortStop = stopOffset > 0m ? NormalizePrice(entryPrice + stopOffset) : null;

		var takeOffset = GetPriceOffset(TakeProfitPips);
		_shortTake = takeOffset > 0m ? NormalizePrice(entryPrice - takeOffset) : null;
	}

	private decimal GetPriceOffset(int pips)
	{
		if (pips <= 0)
			return 0m;

		if (_pipSize > 0m)
			return pips * _pipSize;

		var priceStep = Security?.PriceStep ?? 0m;
		return priceStep > 0m ? pips * priceStep : 0m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security?.Decimals is int digits && digits >= 0)
			return Math.Round(price, digits, MidpointRounding.AwayFromZero);

		var step = security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		var steps = Math.Round(price / step, MidpointRounding.AwayFromZero);
		return steps * step;
	}

	private decimal GetSafeVolume(decimal desiredVolume)
	{
		var security = Security;
		if (security == null)
			return desiredVolume;

		var volume = desiredVolume;
		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 0m;

		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private void ResetRisk()
	{
		_longStop = null;
		_longTake = null;
		_longTrail = null;
		_longEntry = null;

		_shortStop = null;
		_shortTake = null;
		_shortTrail = null;
		_shortEntry = null;
	}
}
