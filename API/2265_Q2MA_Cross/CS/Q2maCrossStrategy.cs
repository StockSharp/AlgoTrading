using System;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Q2MA cross strategy based on open and close moving averages.
/// </summary>
public class Q2maCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<bool> _invert;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;

	private SimpleMovingAverage _closeMa = null!;
	private SimpleMovingAverage _openMa = null!;
	private decimal _prevUp;
	private decimal _prevDn;
	private bool _hasPrev;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions by trend.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow closing short positions by trend.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Invert indicator lines.
	/// </summary>
	public bool Invert { get => _invert.Value; set => _invert.Value = value; }

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Initializes <see cref="Q2maCrossStrategy"/>.
	/// </summary>
	public Q2maCrossStrategy()
	{
		_length = Param(nameof(Length), 8)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Moving average length", "Indicator")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Trading")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in ticks", "Trading")
			.SetCanOptimize(true);

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");

		_invert = Param(nameof(Invert), false)
			.SetDisplay("Invert", "Invert indicator lines", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Indicator timeframe", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
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

		_stopLossPrice = null;
		_takeProfitPrice = null;
		_prevUp = 0m;
		_prevDn = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_closeMa = new SimpleMovingAverage { Length = Length };
		_openMa = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeMa);
			DrawIndicator(area, _openMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upValue = _closeMa.Process(candle.ClosePrice);
		var dnValue = _openMa.Process(candle.OpenPrice);

		if (!upValue.IsFinal || !dnValue.IsFinal)
			return;

		var up = upValue.GetValue<decimal>();
		var dn = dnValue.GetValue<decimal>();

		if (Invert)
			(up, dn) = (dn, up);

		if (_hasPrev)
		{
			if (_prevUp > _prevDn)
			{
			if (SellPosClose && Position < 0)
			{
			BuyMarket(-Position);
			_stopLossPrice = null;
			_takeProfitPrice = null;
			}

			if (BuyPosOpen && up <= dn && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
			{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			var step = Security?.PriceStep ?? 1m;
			BuyMarket(volume);
			_stopLossPrice = candle.ClosePrice - StopLoss * step;
			_takeProfitPrice = candle.ClosePrice + TakeProfit * step;
			}
			}
			else if (_prevUp < _prevDn)
			{
			if (BuyPosClose && Position > 0)
			{
			SellMarket(Position);
			_stopLossPrice = null;
			_takeProfitPrice = null;
			}

			if (SellPosOpen && up >= dn && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
			{
			var volume = Volume + (Position > 0 ? Position : 0m);
			var step = Security?.PriceStep ?? 1m;
			SellMarket(volume);
			_stopLossPrice = candle.ClosePrice + StopLoss * step;
			_takeProfitPrice = candle.ClosePrice - TakeProfit * step;
			}
			}
		}

		if (Position > 0 && _stopLossPrice != null && _takeProfitPrice != null)
		{
			if (candle.LowPrice <= _stopLossPrice || candle.HighPrice >= _takeProfitPrice)
			{
			SellMarket(Position);
			_stopLossPrice = null;
			_takeProfitPrice = null;
			}
		}
		else if (Position < 0 && _stopLossPrice != null && _takeProfitPrice != null)
		{
			if (candle.HighPrice >= _stopLossPrice || candle.LowPrice <= _takeProfitPrice)
			{
			BuyMarket(-Position);
			_stopLossPrice = null;
			_takeProfitPrice = null;
			}
		}

		_prevUp = up;
		_prevDn = dn;
		_hasPrev = true;
	}
}
