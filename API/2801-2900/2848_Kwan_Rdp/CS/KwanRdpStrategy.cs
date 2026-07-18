namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// KWAN RDP trend strategy converted from MetaTrader version.
/// Combines DeMarker and Momentum indicators to detect trend reversals.
/// Opens long when DeMarker rises and momentum is positive, short when DeMarker falls and momentum is negative.
/// </summary>
public class KwanRdpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<int> _momentumPeriod;

	private decimal _prevDem;
	private decimal _prevMom;
	private bool _initialized;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public KwanRdpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "DeMarker indicator length", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum indicator length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDem = 0m;
		_prevMom = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDem = 0;
		_prevMom = 0;
		_initialized = false;

		var deMarker = new DeMarker { Length = DeMarkerPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(deMarker, momentum, (ICandleMessage candle, decimal demValue, decimal momValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!_initialized)
				{
					_prevDem = demValue;
					_prevMom = momValue;
					_initialized = true;
					return;
				}

				var demUp = demValue > _prevDem;
				var demDown = demValue < _prevDem;
				var momUp = momValue > _prevMom;
				var momDown = momValue < _prevMom;

				// Long when DeMarker and Momentum both turn up
				if (demUp && momUp && Position <= 0)
				{
					BuyMarket();
				}
				// Short when DeMarker and Momentum both turn down
				else if (demDown && momDown && Position >= 0)
				{
					SellMarket();
				}

				_prevDem = demValue;
				_prevMom = momValue;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}
}
