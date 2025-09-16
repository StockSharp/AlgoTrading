using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VR Overturn strategy implementing martingale and anti-martingale principles.
/// </summary>
public class VROverturnStrategy : Strategy
{
	private enum TradeMode
	{
		Martingale,
		AntiMartingale
	}

	private readonly StrategyParam<TradeMode> _tradeMode;
	private readonly StrategyParam<Sides> _startSide;
	private readonly StrategyParam<int> _takePoints;
	private readonly StrategyParam<int> _stopPoints;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _multiplier;

	private decimal _currentVolume;
	private decimal _entryPrice;
	private Sides _currentSide;
	private Order _stopOrder;
	private Order _tpOrder;

	/// <summary>
	/// Trade mode selection.
	/// </summary>
	public TradeMode Mode { get => _tradeMode.Value; set => _tradeMode.Value = value; }

	/// <summary>
	/// Initial trade side.
	/// </summary>
	public Sides StartSide { get => _startSide.Value; set => _startSide.Value = value; }

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit { get => _takePoints.Value; set => _takePoints.Value = value; }

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss { get => _stopPoints.Value; set => _stopPoints.Value = value; }

	/// <summary>
	/// Initial volume.
	/// </summary>
	public decimal StartVolume { get => _startVolume.Value; set => _startVolume.Value = value; }

	/// <summary>
	/// Volume multiplier.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="VROverturnStrategy"/>.
	/// </summary>
	public VROverturnStrategy()
	{
		_tradeMode = Param(nameof(Mode), TradeMode.Martingale)
			.SetDisplay("Trade Mode", "Martingale or AntiMartingale", "General");

		_startSide = Param(nameof(StartSide), Sides.Buy)
			.SetDisplay("Start Side", "Initial trade direction", "General");

		_takePoints = Param(nameof(TakeProfit), 300)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_stopPoints = Param(nameof(StopLoss), 300)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_startVolume = Param(nameof(StartVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Initial trade volume", "Volume");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Volume multiplier", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentVolume = StartVolume;
		_entryPrice = 0m;
		_currentSide = StartSide;
		_stopOrder = null;
		_tpOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		OpenOrder(_currentSide, _currentVolume);
	}

	private void OpenOrder(Sides side, decimal volume)
	{
		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position != 0 && _entryPrice == 0m)
		{
			_entryPrice = trade.Trade.Price;
			RegisterProtection(_currentSide == Sides.Buy);
			return;
		}

		if (Position == 0 && _entryPrice != 0m)
		{
			var exitPrice = trade.Trade.Price;
			var profit = (_currentSide == Sides.Buy ? exitPrice - _entryPrice : _entryPrice - exitPrice) * _currentVolume;

			if (Mode == TradeMode.Martingale)
			{
				_currentVolume = profit > 0m ? StartVolume : _currentVolume * Multiplier;
			}
			else
			{
				_currentVolume = profit > 0m ? _currentVolume * Multiplier : StartVolume;
			}

			if (profit < 0m)
				_currentSide = _currentSide == Sides.Buy ? Sides.Sell : Sides.Buy;

			_entryPrice = 0m;

			OpenOrder(_currentSide, _currentVolume);
		}
	}

	private void RegisterProtection(bool isLong)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);
		if (_tpOrder != null && _tpOrder.State == OrderStates.Active)
			CancelOrder(_tpOrder);

		var step = Security.PriceStep ?? 1m;
		var stopOffset = StopLoss * step;
		var takeOffset = TakeProfit * step;

		_stopOrder = isLong
			? SellStop(_currentVolume, _entryPrice - stopOffset)
			: BuyStop(_currentVolume, _entryPrice + stopOffset);

		_tpOrder = isLong
			? SellLimit(_currentVolume, _entryPrice + takeOffset)
			: BuyLimit(_currentVolume, _entryPrice - takeOffset);
	}
}
