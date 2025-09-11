using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Entry Fragger strategy.
/// Tracks candle sequences around EMA50 and uses a volatility cloud for entries.
/// Optional reverse trading allows flipping positions.
/// </summary>
public class EntryFraggerStrategy : Strategy
{
	private readonly StrategyParam<int> _signalAccuracy;
	private readonly StrategyParam<int> _sellSignalAccuracy;
	private readonly StrategyParam<bool> _reverseTrading;
	private readonly StrategyParam<bool> _showBuy;
	private readonly StrategyParam<bool> _showSell;
	private readonly StrategyParam<DataType> _candleType;

	private int _redVectorCount;
	private bool _hasRedVectorBelow;
	private int _greenVectorCount;
	private bool _hasGreenVectorAbove;
	private bool _wasGreen;

	/// <summary>
	/// Required red vectors count before buy.
	/// </summary>
	public int SignalAccuracy
	{
		get => _signalAccuracy.Value;
		set => _signalAccuracy.Value = value;
	}

	/// <summary>
	/// Required green vectors count before sell.
	/// </summary>
	public int SellSignalAccuracy
	{
		get => _sellSignalAccuracy.Value;
		set => _sellSignalAccuracy.Value = value;
	}

	/// <summary>
	/// Enable reverse trading mode.
	/// </summary>
	public bool ReverseTrading
	{
		get => _reverseTrading.Value;
		set => _reverseTrading.Value = value;
	}

	/// <summary>
	/// Allow buy signals.
	/// </summary>
	public bool ShowBuy
	{
		get => _showBuy.Value;
		set => _showBuy.Value = value;
	}

	/// <summary>
	/// Allow sell signals.
	/// </summary>
	public bool ShowSell
	{
		get => _showSell.Value;
		set => _showSell.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EntryFraggerStrategy()
	{
		_signalAccuracy = Param(nameof(SignalAccuracy), 2)
			.SetDisplay("Buy Signal Accuracy", "Required red vectors count before buy", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_sellSignalAccuracy = Param(nameof(SellSignalAccuracy), 2)
			.SetDisplay("Sell Signal Accuracy", "Required green vectors count before sell", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_reverseTrading = Param(nameof(ReverseTrading), false)
			.SetDisplay("Reverse Trading", "Enable reverse trading", "General");

		_showBuy = Param(nameof(ShowBuy), true)
			.SetDisplay("Enable Buy", "Allow buy signals", "General");

		_showSell = Param(nameof(ShowSell), true)
			.SetDisplay("Enable Sell", "Allow sell signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_redVectorCount = 0;
		_greenVectorCount = 0;
		_hasRedVectorBelow = false;
		_hasGreenVectorAbove = false;
		_wasGreen = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var ema50 = new EMA { Length = 50 };
		var ema200 = new EMA { Length = 200 };
		var stDev = new StandardDeviation { Length = 50 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema50, ema200, stDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema50);
			DrawIndicator(area, ema200);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema50Value, decimal ema200Value, decimal stDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upperCloud = ema50Value + stDevValue / 4m;

		var isGreen = candle.ClosePrice > candle.OpenPrice;
		var isRed = candle.ClosePrice < candle.OpenPrice;

		if (isRed && candle.ClosePrice < ema50Value)
		{
			_redVectorCount++;

			if (candle.OpenPrice < ema50Value)
				_hasRedVectorBelow = true;
		}

		if (isGreen && candle.OpenPrice > ema50Value && candle.ClosePrice > ema50Value)
		{
			_greenVectorCount++;
			_hasGreenVectorAbove = true;
		}

		if (ShowBuy && isGreen && candle.OpenPrice > ema50Value && candle.ClosePrice > upperCloud && _hasRedVectorBelow && _redVectorCount >= SignalAccuracy)
		{
			_redVectorCount = 0;
			_hasRedVectorBelow = false;

			if (ReverseTrading && Position < 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		if (ShowSell && _hasGreenVectorAbove && _wasGreen && isRed && candle.ClosePrice > upperCloud && candle.OpenPrice > ema50Value && _greenVectorCount >= SellSignalAccuracy)
		{
			_greenVectorCount = 0;
			_hasGreenVectorAbove = false;

			if (Position > 0)
			{
				if (ReverseTrading)
					SellMarket(Volume + Math.Abs(Position));
				else
					SellMarket(Math.Abs(Position));
			}
			else if (ReverseTrading && Position <= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_wasGreen = isGreen;
	}
}
