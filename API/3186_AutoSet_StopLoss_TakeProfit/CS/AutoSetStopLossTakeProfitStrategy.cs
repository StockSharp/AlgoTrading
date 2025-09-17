namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class AutoSetStopLossTakeProfitStrategy : Strategy
{
	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
		?? TryGetField("MinStopPrice")
		?? TryGetField("StopPrice")
		?? TryGetField("StopDistance");

	private static readonly Level1Fields? FreezeLevelField = TryGetField("FreezeLevel")
		?? TryGetField("FreezePrice")
		?? TryGetField("FreezeDistance");

	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<TradeDirection> _tradeDirection;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _stopLevel;
	private decimal? _freezeLevel;
	private decimal _pointValue;
	private decimal _priceStep;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;

	public AutoSetStopLossTakeProfitStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop loss (pips)", "Distance from price to the protective stop in MetaTrader pips.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 140)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take profit (pips)", "Distance from price to the protective take profit in MetaTrader pips.", "Risk")
			.SetCanOptimize(true);

		_tradeDirection = Param(nameof(DirectionFilter), TradeDirection.Buy)
			.SetDisplay("Managed side", "Which position direction should receive automatic stop/take placement.", "Execution");
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set
		{
			_stopLossPips.Value = value;
			TryUpdateProtection();
		}
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set
		{
			_takeProfitPips.Value = value;
			TryUpdateProtection();
		}
	}

	public TradeDirection DirectionFilter
	{
		get => _tradeDirection.Value;
		set
		{
			_tradeDirection.Value = value;
			TryUpdateProtection();
		}
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_stopLevel = null;
		_freezeLevel = null;
		_pointValue = 0m;
		_priceStep = 0m;

		_stopOrder = null;
		_takeProfitOrder = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();
		_priceStep = GetPriceStep();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		TryUpdateProtection();
	}

	protected override void OnStopped()
	{
		base.OnStopped();

		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		TryUpdateProtection();
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		TryUpdateProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAsk = ask;

		if (StopLevelField is Level1Fields stopField && message.Changes.TryGetValue(stopField, out var stopValue))
			_stopLevel = ToDecimal(stopValue);

		if (FreezeLevelField is Level1Fields freezeField && message.Changes.TryGetValue(freezeField, out var freezeValue))
			_freezeLevel = ToDecimal(freezeValue);

		TryUpdateProtection();
	}

	private void TryUpdateProtection()
	{
		if (ProcessState != ProcessStates.Started)
			return;

		var position = Position;
		if (position == 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var isLong = position > 0m;
		if (!IsDirectionManaged(isLong))
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var volume = NormalizeVolume(Math.Abs(position));
		if (volume <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var referencePrice = isLong ? _bestAsk : _bestBid;
		if (referencePrice == null)
			return;

		var minDistance = GetMinimalDistance();

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pointValue : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pointValue : 0m;

		decimal? stopPrice = null;
		if (stopDistance > 0m)
		{
			var distance = Math.Max(stopDistance, minDistance);
			stopPrice = isLong
				? referencePrice.Value - distance
				: referencePrice.Value + distance;
		}

		decimal? takePrice = null;
		if (takeDistance > 0m)
		{
			var distance = Math.Max(takeDistance, minDistance);
			takePrice = isLong
				? referencePrice.Value + distance
				: referencePrice.Value - distance;
		}

		UpdateStopOrder(stopPrice, volume, isLong);
		UpdateTakeProfitOrder(takePrice, volume, isLong);
	}

	private void UpdateStopOrder(decimal? targetPrice, decimal volume, bool isLong)
	{
		if (targetPrice == null)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		var normalizedPrice = NormalizePrice(targetPrice.Value);
		if (normalizedPrice <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		if (_stopOrder == null)
		{
			_stopOrder = isLong
				? SellStop(volume, normalizedPrice)
				: BuyStop(volume, normalizedPrice);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_stopOrder = null;
			UpdateStopOrder(targetPrice, volume, isLong);
			return;
		}

		if (_stopOrder.Price != normalizedPrice || _stopOrder.Volume != volume)
		{
			ReRegisterOrder(_stopOrder, normalizedPrice, volume);
		}
	}

	private void UpdateTakeProfitOrder(decimal? targetPrice, decimal volume, bool isLong)
	{
		if (targetPrice == null)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var normalizedPrice = NormalizePrice(targetPrice.Value);
		if (normalizedPrice <= 0m)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		if (_takeProfitOrder == null)
		{
			_takeProfitOrder = isLong
				? SellLimit(volume, normalizedPrice)
				: BuyLimit(volume, normalizedPrice);
			return;
		}

		if (_takeProfitOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_takeProfitOrder = null;
			UpdateTakeProfitOrder(targetPrice, volume, isLong);
			return;
		}

		if (_takeProfitOrder.Price != normalizedPrice || _takeProfitOrder.Volume != volume)
		{
			ReRegisterOrder(_takeProfitOrder, normalizedPrice, volume);
		}
	}

	private decimal GetMinimalDistance()
	{
		var level = Math.Max(_stopLevel ?? 0m, _freezeLevel ?? 0m);

		if (level <= 0m && _bestBid != null && _bestAsk != null)
		{
			var spread = Math.Abs(_bestAsk.Value - _bestBid.Value);
			level = spread > 0m ? spread * 3m : 0m;
		}

		return level > 0m ? level * 1.1m : 0m;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;

		return step * multiplier;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		return step;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
			return price;

		var steps = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return steps * _priceStep;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var min = security.MinVolume;
		if (min != null && volume < min.Value)
			volume = min.Value;

		var max = security.MaxVolume;
		if (max != null && volume > max.Value)
			volume = max.Value;

		return volume;
	}

	private void CancelProtectiveOrder(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Active or OrderStates.Pending)
			CancelOrder(order);

		order = null;
	}

	private bool IsDirectionManaged(bool isLong)
	{
		return DirectionFilter switch
		{
			TradeDirection.Buy => isLong,
			TradeDirection.Sell => !isLong,
			TradeDirection.BuySell => true,
			_ => true,
		};
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float fl => (decimal)fl,
			long l => l,
			int i => i,
			short s => s,
			byte b => b,
			null => null,
			IConvertible convertible => Convert.ToDecimal(convertible, CultureInfo.InvariantCulture),
			_ => null,
		};
	}

	public enum TradeDirection
	{
		Buy,
		Sell,
		BuySell,
	}
}
