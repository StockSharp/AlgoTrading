using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI histogram color changes.
/// Opens long when RSI leaves the overbought zone.
/// Opens short when RSI leaves the oversold zone.
/// </summary>
public class RsiHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevClass = -1;
	private int _prevPrevClass = -1;

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
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
	/// Allow closing long positions on opposite signal.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on opposite signal.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiHistogramStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Length of the RSI indicator", "RSI")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Overbought threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(55m, 70m, 5m);

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetDisplay("Low Level", "Oversold threshold", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(30m, 45m, 5m);

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Enable Buy Entry", "Allow long entries when signal appears", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Enable Sell Entry", "Allow short entries when signal appears", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Buy Positions", "Allow closing longs on opposite signal", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Sell Positions", "Allow closing shorts on opposite signal", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for RSI calculation", "General");
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
		_prevClass = -1;
		_prevPrevClass = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(rsi, (candle, rsiValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var currentClass = rsiValue > HighLevel ? 0 : rsiValue < LowLevel ? 2 : 1;

			if (_prevPrevClass == 0 && _prevClass > 0)
			{
				if (SellPosClose && Position < 0)
					BuyMarket(Math.Abs(Position));

				if (BuyPosOpen && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else if (_prevPrevClass == 2 && _prevClass < 2)
			{
				if (BuyPosClose && Position > 0)
					SellMarket(Math.Abs(Position));

				if (SellPosOpen && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}

			_prevPrevClass = _prevClass;
			_prevClass = currentClass;
		})
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}