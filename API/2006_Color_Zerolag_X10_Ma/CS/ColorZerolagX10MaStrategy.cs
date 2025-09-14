using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend detection strategy based on the slope of a zero lag moving average.
/// </summary>
public class ColorZerolagX10MaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private ZeroLagExponentialMovingAverage _zlma = null!;

	private decimal _prev1;
	private decimal _prev2;
	private bool _hasPrev1;
	private bool _hasPrev2;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorZerolagX10MaStrategy"/>.
	/// </summary>
	public ColorZerolagX10MaStrategy()
	{
		_length = Param(nameof(Length), 20).SetDisplay("Length", "MA length", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
		_buyPosOpen = Param(nameof(BuyPosOpen), true).SetDisplay("Buy Open", "Allow long entries", "Trading");
		_sellPosOpen = Param(nameof(SellPosOpen), true).SetDisplay("Sell Open", "Allow short entries", "Trading");
		_buyPosClose = Param(nameof(BuyPosClose), true).SetDisplay("Buy Close", "Allow closing longs", "Trading");
		_sellPosClose = Param(nameof(SellPosClose), true).SetDisplay("Sell Close", "Allow closing shorts", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_zlma = new ZeroLagExponentialMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_zlma, ProcessCandle)
			.Start();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_hasPrev1 && _hasPrev2)
		{
			var trendUp = _prev1 < _prev2 && ma > _prev1;
			var trendDown = _prev1 > _prev2 && ma < _prev1;

			if (trendUp)
			{
				if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
				if (BuyPosOpen && Position <= 0)
				BuyMarket(Volume);
			}
			else if (trendDown)
			{
				if (BuyPosClose && Position > 0)
				SellMarket(Position);
				if (SellPosOpen && Position >= 0)
				SellMarket(Volume);
			}
		}

		_prev2 = _prev1;
		_prev1 = ma;
		_hasPrev2 = _hasPrev1;
		_hasPrev1 = true;
	}
}
