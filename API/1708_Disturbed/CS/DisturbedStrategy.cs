using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging strategy that opens both buy and sell orders and manages them using spread levels.
/// </summary>
public class DisturbedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _gainMultiplier;

	private bool _ordersOpened;
	private bool _buyActive;
	private bool _sellActive;
	private decimal _eqA;
	private decimal _eqAF;
	private decimal _eqV;
	private decimal _eqVF;

	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public decimal GainMultiplier { get => _gainMultiplier.Value; set => _gainMultiplier.Value = value; }

	public DisturbedStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Trade volume", "General")
			.SetCanOptimize(true);

		_gainMultiplier = Param(nameof(GainMultiplier), 2m)
			.SetDisplay("Gain Multiplier", "Spread multiplier for profit target", "General")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeLevel1(Security)
			.Bind(OnLevel1)
			.Start();
	}

	private void OnLevel1(Level1ChangeMessage message)
	{
		var ask = message.TryGetDecimal(Level1Fields.AskPrice);
		var bid = message.TryGetDecimal(Level1Fields.BidPrice);

		if (ask is not decimal askPrice || bid is not decimal bidPrice)
			return;

		var spread = askPrice - bidPrice;

		if (!_ordersOpened)
		{
			_eqA = askPrice + spread;
			_eqAF = askPrice + GainMultiplier * spread;
			_eqV = bidPrice - spread;
			_eqVF = bidPrice - GainMultiplier * spread;

			BuyMarket(Volume);
			SellMarket(Volume);

			_ordersOpened = true;
			_buyActive = true;
			_sellActive = true;
			return;
		}

		if (_sellActive && bidPrice >= _eqA)
		{
			// Close losing sell position
			BuyMarket(Volume);
			_sellActive = false;
			return;
		}

		if (_buyActive && bidPrice <= _eqV)
		{
			// Close losing buy position
			SellMarket(Volume);
			_buyActive = false;
			return;
		}

		if (_buyActive && !_sellActive)
		{
			if (bidPrice >= _eqAF || bidPrice <= _eqA)
			{
				// Remaining buy position hit target or stop
				SellMarket(Volume);
				_buyActive = false;
			}
		}
		else if (_sellActive && !_buyActive)
		{
			if (askPrice <= _eqVF || askPrice >= _eqV)
			{
				// Remaining sell position hit target or stop
				BuyMarket(Volume);
				_sellActive = false;
			}
		}

		if (!_buyActive && !_sellActive)
			_ordersOpened = false;
	}
}
