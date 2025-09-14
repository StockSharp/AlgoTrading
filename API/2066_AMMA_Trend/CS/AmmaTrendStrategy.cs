namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class AmmaTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;

	private decimal? _mma0;
	private decimal? _mma1;
	private decimal? _mma2;
	private decimal? _mma3;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	public AmmaTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use for analysis", "General");

		_maPeriod = Param(nameof(MaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("AMMA Period", "Period of the modified moving average", "Indicator");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading");
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
		_mma0 = _mma1 = _mma2 = _mma3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var mma = new ModifiedMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mma, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal mmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_mma3 = _mma2;
		_mma2 = _mma1;
		_mma1 = _mma0;
		_mma0 = mmaValue;

		if (_mma1 is null || _mma2 is null || _mma3 is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Upward movement detected
		if (_mma2 < _mma3 && _mma1 > _mma2)
		{
			if (AllowLongEntry && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			if (AllowShortExit && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		// Downward movement detected
		else if (_mma2 > _mma3 && _mma1 < _mma2)
		{
			if (AllowShortEntry && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			if (AllowLongExit && Position > 0)
				SellMarket(Math.Abs(Position));
		}
	}
}
