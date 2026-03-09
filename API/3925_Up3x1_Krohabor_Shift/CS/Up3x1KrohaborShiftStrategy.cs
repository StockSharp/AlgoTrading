using System;



using StockSharp.Algo.Indicators;

using StockSharp.Algo.Strategies;

using StockSharp.BusinessEntities;

using StockSharp.Messages;



namespace StockSharp.Samples.Strategies;



public class Up3x1KrohaborShiftStrategy : Strategy

{

	private readonly StrategyParam<int> _channelPeriod;

	private readonly StrategyParam<int> _emaPeriod;

	private readonly StrategyParam<DataType> _candleType;



	private decimal _prevClose; private decimal _prevMid; private bool _hasPrev;

	private int _cooldown;



	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }



	public Up3x1KrohaborShiftStrategy()

	{

		_channelPeriod = Param(nameof(ChannelPeriod), 20).SetDisplay("Channel Period", "Channel lookback", "Indicators");

		_emaPeriod = Param(nameof(EmaPeriod), 14).SetDisplay("EMA Period", "EMA filter", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");

	}



	/// <inheritdoc />

	protected override void OnReseted()

	{

		base.OnReseted();

		_prevClose = default;

		_prevMid = default;

		_hasPrev = default;

		_cooldown = default;

	}



	/// <inheritdoc />

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)

	{

		base.OnStarted2(time);

		_hasPrev = false;

		var highest = new Highest { Length = ChannelPeriod };

		var lowest = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(highest, lowest, ProcessCandle).Start();

	}



	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)

	{

		if (candle.State != CandleStates.Finished) return;

		if (!IsFormedAndOnlineAndAllowTrading()) return;

		var close = candle.ClosePrice;

		var mid = (highest + lowest) / 2;

		if (!_hasPrev) { _prevClose = close; _prevMid = mid; _hasPrev = true; return; }

		if (_cooldown > 0)

		{

			_cooldown--;

			_prevClose = close; _prevMid = mid;

			return;

		}



		if (_prevClose <= _prevMid && close > mid && Position <= 0)

		{

			var volume = Volume + Math.Abs(Position);

			BuyMarket(volume);

			_cooldown = 6;

		}

		else if (_prevClose >= _prevMid && close < mid && Position >= 0)

		{

			var volume = Volume + Math.Abs(Position);

			SellMarket(volume);

			_cooldown = 6;

		}

		_prevClose = close; _prevMid = mid;

	}

}

