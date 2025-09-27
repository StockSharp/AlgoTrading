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
/// Multi-currency template strategy converted from MetaTrader 4.
/// Implements EMA crossover entries, martingale averaging and trailing stop management.
/// </summary>
public class MultiCurrencyTemplateStrategy : Strategy
{
	private readonly StrategyParam<OrderMethodTypes> _orderMethod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<bool> _enableMartingale;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<int> _martingaleStepPoints;
	private readonly StrategyParam<bool> _enableTakeProfitAverage;
	private readonly StrategyParam<int> _takeProfitAveragePoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _longTrailingStop;
	private decimal _shortTrailingStop;
	private bool _longTrailingArmed;
	private bool _shortTrailingArmed;
	private decimal _nextLongVolume;
	private decimal _nextShortVolume;
	private decimal? _longReferencePrice;
	private decimal? _shortReferencePrice;
	private int _longEntryCount;
	private int _shortEntryCount;

	/// <summary>
	/// Supported order directions.
	/// </summary>
	public enum OrderMethodTypes
	{
		/// <summary>
		/// Allow both long and short signals.
		/// </summary>
		BuyAndSell,

		/// <summary>
		/// Allow long entries only.
		/// </summary>
		BuyOnly,

		/// <summary>
		/// Allow short entries only.
		/// </summary>
		SellOnly
	}

	/// <summary>
	/// Initializes parameters with defaults that mirror the MetaTrader template.
	/// </summary>
	public MultiCurrencyTemplateStrategy()
	{
		_orderMethod = Param(nameof(OrderMethod), OrderMethodTypes.BuyAndSell)
			.SetDisplay("Order Method", "Choose whether the strategy trades long, short or both directions.", "Trading")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(Lots), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume (lots)", "Base order volume used for the first market entry.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.10m, 0.01m);

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in MetaTrader pips.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Initial profit target distance in MetaTrader pips.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 25);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 15)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pts)", "Activation distance for the trailing stop expressed in MetaTrader points.", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pts)", "Minimal improvement required before updating the trailing stop.", "Risk");

		_enableMartingale = Param(nameof(EnableMartingale), true)
			.SetDisplay("Enable Martingale", "Enable averaging orders when the market moves against the position.", "Martingale");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Multiplier applied to the next averaging order volume.", "Martingale")
			.SetCanOptimize(true)
			.SetOptimize(1.1m, 2.0m, 0.1m);

		_martingaleStepPoints = Param(nameof(MartingaleStepPoints), 150)
			.SetNotNegative()
			.SetDisplay("Step (pts)", "Distance in MetaTrader points before placing the next averaging order.", "Martingale")
			.SetCanOptimize(true)
			.SetOptimize(50, 250, 25);

		_enableTakeProfitAverage = Param(nameof(EnableTakeProfitAverage), true)
			.SetDisplay("Average Take Profit", "Average the take-profit using the weighted position price when multiple trades are open.", "Martingale");

		_takeProfitAveragePoints = Param(nameof(TakeProfitAveragePoints), 20)
			.SetNotNegative()
			.SetDisplay("Average TP Offset (pts)", "Offset used for the averaged take-profit target in MetaTrader points.", "Martingale");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for EMA calculations and trade management.", "Data");
	}

	/// <summary>
	/// Defines which direction the strategy may trade.
	/// </summary>
	public OrderMethodTypes OrderMethod
	{
		get => _orderMethod.Value;
		set => _orderMethod.Value = value;
	}

	/// <summary>
	/// Base trade volume in lots.
	/// </summary>
	public decimal Lots
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in MetaTrader pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in MetaTrader pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Activation distance for the trailing stop expressed in MetaTrader points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Step required to move the trailing stop closer to price.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enables martingale style averaging orders.
	/// </summary>
	public bool EnableMartingale
	{
		get => _enableMartingale.Value;
		set => _enableMartingale.Value = value;
	}

	/// <summary>
	/// Multiplier for the next averaging order volume.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Distance in MetaTrader points before adding a new averaging order.
	/// </summary>
	public int MartingaleStepPoints
	{
		get => _martingaleStepPoints.Value;
		set => _martingaleStepPoints.Value = value;
	}

	/// <summary>
	/// Uses averaged take-profit levels when multiple orders are open.
	/// </summary>
	public bool EnableTakeProfitAverage
	{
		get => _enableTakeProfitAverage.Value;
		set => _enableTakeProfitAverage.Value = value;
	}

	/// <summary>
	/// Offset applied to the averaged take-profit in MetaTrader points.
	/// </summary>
	public int TakeProfitAveragePoints
	{
		get => _takeProfitAveragePoints.Value;
		set => _takeProfitAveragePoints.Value = value;
	}

	/// <summary>
	/// Candle type used for EMA calculations.
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
		_pipSize = 0m;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Determine pip size using security metadata.
		_pipSize = CalculatePipSize();
		if (_pipSize <= 0m)
		{
			var step = Security?.PriceStep ?? 0m;
			_pipSize = step > 0m ? step : 0.0001m;
		}

		ResetLongState();
		ResetShortState();

		var fastEma = new EMA { Length = 20 };
		var slowEma = new EMA { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		// Activate protective block so risk controls remain engaged.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0m)
			ManageLongPosition(candle);
		if (Position < 0m)
			ManageShortPosition(candle);

		var signal = fastEma > slowEma ? 1 : fastEma < slowEma ? -1 : 0;

		if (signal > 0 && OrderMethod != OrderMethodTypes.SellOnly)
			TryEnterLong(candle);
		else if (signal < 0 && OrderMethod != OrderMethodTypes.BuyOnly)
			TryEnterShort(candle);

		if (EnableMartingale)
			ApplyMartingale(candle);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (Lots <= 0m)
			return;

		if (Position < 0m)
		{
			ClosePosition();
			ResetShortState();
		}

		if (Position > 0m)
			return;

		BuyMarket(Lots);
		_longEntryCount = 1;
		_longReferencePrice = candle.ClosePrice;
		_longTrailingArmed = false;
		_longTrailingStop = 0m;
		_nextLongVolume = EnableMartingale ? Lots * MartingaleMultiplier : Lots;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (Lots <= 0m)
			return;

		if (Position > 0m)
		{
			ClosePosition();
			ResetLongState();
		}

		if (Position < 0m)
			return;

		SellMarket(Lots);
		_shortEntryCount = 1;
		_shortReferencePrice = candle.ClosePrice;
		_shortTrailingArmed = false;
		_shortTrailingStop = 0m;
		_nextShortVolume = EnableMartingale ? Lots * MartingaleMultiplier : Lots;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var price = candle.ClosePrice;
		var entryPrice = PositionPrice ?? _longReferencePrice ?? price;
		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		if (stopDistance > 0m && price <= entryPrice - stopDistance)
		{
			ClosePosition();
			ResetLongState();
			return;
		}

		var takeDistance = GetLongTakeProfitDistance();
		if (takeDistance > 0m && price >= entryPrice + takeDistance)
		{
			ClosePosition();
			ResetLongState();
			return;
		}

		UpdateLongTrailing(price, entryPrice);
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var price = candle.ClosePrice;
		var entryPrice = PositionPrice ?? _shortReferencePrice ?? price;
		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		if (stopDistance > 0m && price >= entryPrice + stopDistance)
		{
			ClosePosition();
			ResetShortState();
			return;
		}

		var takeDistance = GetShortTakeProfitDistance();
		if (takeDistance > 0m && price <= entryPrice - takeDistance)
		{
			ClosePosition();
			ResetShortState();
			return;
		}

		UpdateShortTrailing(price, entryPrice);
	}

	private void UpdateLongTrailing(decimal price, decimal entryPrice)
	{
		if (_longTrailingArmed && _longTrailingStop > 0m && price <= _longTrailingStop)
		{
			ClosePosition();
			ResetLongState();
			return;
		}

		var activationDistance = TrailingStopPoints > 0 ? TrailingStopPoints * _pipSize : 0m;
		if (activationDistance <= 0m)
			return;

		if (price - entryPrice < activationDistance)
			return;

		var candidateStop = price - activationDistance;
		if (!_longTrailingArmed)
		{
			_longTrailingArmed = true;
			_longTrailingStop = candidateStop;
			return;
		}

		var stepDistance = TrailingStepPoints > 0 ? TrailingStepPoints * _pipSize : 0m;
		if (stepDistance <= 0m)
		{
			if (candidateStop > _longTrailingStop)
				_longTrailingStop = candidateStop;
		}
		else if (candidateStop - _longTrailingStop >= stepDistance)
		{
			_longTrailingStop = candidateStop;
		}
	}

	private void UpdateShortTrailing(decimal price, decimal entryPrice)
	{
		if (_shortTrailingArmed && _shortTrailingStop > 0m && price >= _shortTrailingStop)
		{
			ClosePosition();
			ResetShortState();
			return;
		}

		var activationDistance = TrailingStopPoints > 0 ? TrailingStopPoints * _pipSize : 0m;
		if (activationDistance <= 0m)
			return;

		if (entryPrice - price < activationDistance)
			return;

		var candidateStop = price + activationDistance;
		if (!_shortTrailingArmed)
		{
			_shortTrailingArmed = true;
			_shortTrailingStop = candidateStop;
			return;
		}

		var stepDistance = TrailingStepPoints > 0 ? TrailingStepPoints * _pipSize : 0m;
		if (stepDistance <= 0m)
		{
			if (candidateStop < _shortTrailingStop || _shortTrailingStop <= 0m)
				_shortTrailingStop = candidateStop;
		}
		else if (_shortTrailingStop <= 0m || _shortTrailingStop - candidateStop >= stepDistance)
		{
			_shortTrailingStop = candidateStop;
		}
	}

	private void ApplyMartingale(ICandleMessage candle)
	{
		if (Lots <= 0m)
			return;

		var step = MartingaleStepPoints > 0 ? MartingaleStepPoints * _pipSize : 0m;
		if (step <= 0m)
			return;

		var price = candle.ClosePrice;

		if (Position > 0m && OrderMethod != OrderMethodTypes.SellOnly)
		{
			var reference = _longReferencePrice ?? PositionPrice ?? price;
			if (price <= reference - step)
			{
				var volume = _nextLongVolume;
				if (volume > 0m)
				{
					BuyMarket(volume);
					_longEntryCount++;
					_longReferencePrice = price;
					_nextLongVolume = volume * MartingaleMultiplier;
					_longTrailingArmed = false;
				}
			}
		}
		else if (Position < 0m && OrderMethod != OrderMethodTypes.BuyOnly)
		{
			var reference = _shortReferencePrice ?? PositionPrice ?? price;
			if (price >= reference + step)
			{
				var volume = _nextShortVolume;
				if (volume > 0m)
				{
					SellMarket(volume);
					_shortEntryCount++;
					_shortReferencePrice = price;
					_nextShortVolume = volume * MartingaleMultiplier;
					_shortTrailingArmed = false;
				}
			}
		}
	}

	private decimal GetLongTakeProfitDistance()
	{
		var points = EnableTakeProfitAverage && _longEntryCount >= 2 ? TakeProfitAveragePoints : TakeProfitPips;
		return points > 0 ? points * _pipSize : 0m;
	}

	private decimal GetShortTakeProfitDistance()
	{
		var points = EnableTakeProfitAverage && _shortEntryCount >= 2 ? TakeProfitAveragePoints : TakeProfitPips;
		return points > 0 ? points * _pipSize : 0m;
	}

	private void ResetLongState()
	{
		_longTrailingStop = 0m;
		_longTrailingArmed = false;
		_nextLongVolume = Lots;
		_longReferencePrice = null;
		_longEntryCount = 0;
	}

	private void ResetShortState()
	{
		_shortTrailingStop = 0m;
		_shortTrailingArmed = false;
		_nextShortVolume = Lots;
		_shortReferencePrice = null;
		_shortEntryCount = 0;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var digits = 0;
		var value = step;
		while (value < 1m && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		var multiplier = (digits == 3 || digits == 5) ? 10m : 1m;
		return step * multiplier;
	}
}

