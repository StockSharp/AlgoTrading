namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on gap between previous close and current open.
/// </summary>
public class IGapStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapSize;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private decimal? _prevClose;

	/// <summary>
	/// Gap size in price steps.
	/// </summary>
	public decimal GapSize
	{
		get => _gapSize.Value;
		set => _gapSize.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions on gap down.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions on gap up.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Close long position on opposite signal.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Close short position on opposite signal.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IGapStrategy()
	{
		_gapSize = Param(nameof(GapSize), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Gap Size", "Gap in price steps required to trigger signal", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for gap detection", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Enable Buy", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Enable Sell", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Buy", "Close long on opposite signal", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Sell", "Close short on opposite signal", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;

		if (_prevClose is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var threshold = step * GapSize;
		var gap = _prevClose.Value - candle.OpenPrice;

		if (gap > threshold)
		{
			if (SellPosClose && Position < 0)
				BuyMarket();

			if (BuyPosOpen && Position <= 0)
				BuyMarket();
		}
		else if (-gap > threshold)
		{
			if (BuyPosClose && Position > 0)
				SellMarket();

			if (SellPosOpen && Position >= 0)
				SellMarket();
		}

		_prevClose = candle.ClosePrice;
	}
}

