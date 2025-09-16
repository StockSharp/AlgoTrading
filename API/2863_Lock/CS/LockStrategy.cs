using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedged lock strategy that simultaneously opens long and short legs.
/// </summary>
public class LockStrategy : Strategy
{
	private sealed class HedgeLeg
	{
		public bool IsLong;
		public decimal Volume;
		public decimal EntryPrice;
	}

	private readonly List<HedgeLeg> _legs = new();

	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _lotExponential;
	private readonly StrategyParam<decimal> _excessBalanceOverEquity;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentVolume;
	private decimal _accountBalance;
	private decimal _startBalance;
	private decimal _pipSize;

	/// <summary>
	/// Base lot size used when opening hedge legs.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips for each leg.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the lot size after successful cycle.
	/// </summary>
	public decimal LotExponential
	{
		get => _lotExponential.Value;
		set => _lotExponential.Value = value;
	}

	/// <summary>
	/// Maximum allowed balance minus equity gap before locking profits.
	/// </summary>
	public decimal ExcessBalanceOverEquity
	{
		get => _excessBalanceOverEquity.Value;
		set => _excessBalanceOverEquity.Value = value;
	}

	/// <summary>
	/// Minimum equity growth required to trigger full liquidation.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Candle type that drives the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public LockStrategy()
	{
		_lotSize = Param(nameof(LotSize), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Base order size for each hedged leg", "Trading")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetDisplay("Take Profit (pips)", "Profit target distance for every position in pips", "Risk")
			.SetCanOptimize(true);

		_lotExponential = Param(nameof(LotExponential), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Scaling factor applied after both legs open successfully", "Trading")
			.SetCanOptimize(true);

		_excessBalanceOverEquity = Param(nameof(ExcessBalanceOverEquity), 3000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Balance - Equity Threshold", "Maximum allowed floating loss before locking profit", "Risk")
			.SetCanOptimize(true);

		_minProfit = Param(nameof(MinProfit), 500m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Minimum Profit", "Required equity gain before harvesting profits", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to drive the strategy logic", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_legs.Clear();
		_currentVolume = LotSize;
		_startBalance = Portfolio?.CurrentValue ?? 0m;
		_accountBalance = _startBalance;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

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

		// Simulate broker-side take-profit handling before new decisions.
		CheckTakeProfits(candle);

		var equity = Portfolio?.CurrentValue ?? 0m;

		if (_legs.Count == 0)
			_accountBalance = equity;

		var balanceExcess = _accountBalance - equity;

		if (balanceExcess > ExcessBalanceOverEquity && equity > _startBalance + MinProfit)
		{
			CloseAllLegs();
			_startBalance = equity;
			_accountBalance = equity;
			_currentVolume = LotSize;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buyCount = 0;
		var sellCount = 0;

		CountLegs(ref buyCount, ref sellCount);

		var totalPositions = buyCount + sellCount;

		if (totalPositions == 0)
			_currentVolume = LotSize;

		if (totalPositions <= 1)
		{
			if (TryOpenPair(candle))
			{
				var nextVolume = AdjustVolume(_currentVolume * LotExponential);
				_currentVolume = nextVolume > 0m ? nextVolume : LotSize;
			}
		}
	}

	private void CheckTakeProfits(ICandleMessage candle)
	{
		var tp = TakeProfitPips <= 0 ? 0m : TakeProfitPips * _pipSize;
		if (tp <= 0m)
			return;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (leg.IsLong)
			{
				if (candle.ClosePrice >= leg.EntryPrice + tp)
				{
					SellMarket(leg.Volume);
					_legs.RemoveAt(i);
				}
			}
			else
			{
				if (candle.ClosePrice <= leg.EntryPrice - tp)
				{
					BuyMarket(leg.Volume);
					_legs.RemoveAt(i);
				}
			}
		}
	}

	private bool TryOpenPair(ICandleMessage candle)
	{
		if (_currentVolume <= 0m)
			return false;

		var entryPrice = candle.ClosePrice;

		var buyOrder = BuyMarket(_currentVolume);
		var sellOrder = SellMarket(_currentVolume);

		var buySuccess = buyOrder is not null;
		var sellSuccess = sellOrder is not null;

		if (buySuccess)
		{
			_legs.Add(new HedgeLeg
			{
				IsLong = true,
				Volume = _currentVolume,
				EntryPrice = entryPrice
			});
		}

		if (sellSuccess)
		{
			_legs.Add(new HedgeLeg
			{
				IsLong = false,
				Volume = _currentVolume,
				EntryPrice = entryPrice
			});
		}

		return buySuccess && sellSuccess;
	}

	private void CloseAllLegs()
	{
		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (leg.IsLong)
				SellMarket(leg.Volume);
			else
				BuyMarket(leg.Volume);
		}

		_legs.Clear();
	}

	private void CountLegs(ref int buyCount, ref int sellCount)
	{
		for (var i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];

			if (leg.IsLong)
				buyCount++;
			else
				sellCount++;
		}
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;

		if (step > 0m)
			volume = step * Math.Floor(volume / step);

		var minVolume = Security.MinVolume ?? 0m;

		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = Security.MaxVolume;

		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		if (Security is null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals ?? GetDecimalsFromStep(step);

		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;

		return step * factor;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
			return 0;

		var value = Math.Abs(Math.Log10((double)step));
		return (int)Math.Round(value);
	}
}
