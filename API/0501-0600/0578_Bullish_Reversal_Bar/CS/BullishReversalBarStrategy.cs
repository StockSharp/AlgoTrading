using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Confirms a bullish reversal below the shifted Alligator with a later close above its high.
/// Optional AO and MFI squat filters qualify the setup; its low protects the long position.
/// </summary>
public class BullishReversalBarStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableAo;
	private readonly StrategyParam<bool> _enableMfi;
	private readonly StrategyParam<DataType> _candleType;
	private Alligator _alligator;
	private AwesomeOscillator _ao;
	private MarketFacilitationIndex _mfi;
	private decimal? _previousLow, _previousLips, _previousAo, _previousMfi;
	private decimal _previousVolume;
	private (decimal High, decimal Low)? _pending;
	private decimal _stopLoss;
	private bool _exitPending;

	public bool EnableAo { get => _enableAo.Value; set => _enableAo.Value = value; }
	public bool EnableMfi { get => _enableMfi.Value; set => _enableMfi.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BullishReversalBarStrategy()
	{
		_enableAo = Param(nameof(EnableAo), false);
		_enableMfi = Param(nameof(EnableMfi), false);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
		_alligator = null;
		_ao = null;
		_mfi = null;
	}

	private void ResetState()
	{
		_previousLow = _previousLips = _previousAo = _previousMfi = null;
		_previousVolume = _stopLoss = 0m;
		_pending = null;
		_exitPending = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetState();
		_alligator = new Alligator();
		_ao = EnableAo ? new AwesomeOscillator() : null;
		_mfi = EnableMfi ? new MarketFacilitationIndex() : null;
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_alligator, (candle, value) => ProcessCandle(candle, (IAlligatorValue)value), true).Start();
		var quotes = new Subscription(DataType.Level1, Security);
		quotes.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeLevel1(quotes).Bind(ProcessQuote).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _alligator);
			if (_ao != null)
				DrawIndicator(area, _ao);
			if (_mfi != null)
				DrawIndicator(area, _mfi);
			DrawOwnTrades(area);
		}
	}

	private static decimal? ProcessFilter(IIndicator indicator, ICandleMessage candle)
	{
		if (indicator == null)
			return null;
		var value = indicator.Process(new CandleIndicatorValue(indicator, candle));
		return indicator.IsFormed && !value.IsEmpty ? value.GetValue<decimal>() : null;
	}

	private void ProcessCandle(ICandleMessage candle, IAlligatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousLow = _previousLow;
		var previousLips = _previousLips;
		var previousAo = _previousAo;
		var previousMfi = _previousMfi;
		var previousVolume = _previousVolume;
		var ao = ProcessFilter(_ao, candle);
		var mfi = ProcessFilter(_mfi, candle);
		_previousLow = candle.LowPrice;
		_previousLips = value.Lips;
		_previousAo = ao;
		_previousMfi = mfi;
		_previousVolume = candle.TotalVolume;

		if (value.Jaw is not decimal jaw || value.Teeth is not decimal teeth || value.Lips is not decimal lips
			|| !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0m)
		{
			if (candle.LowPrice <= _stopLoss || lips < previousLips)
				CloseLong();
			return;
		}
		if (Position != 0m)
			return;
		_exitPending = false;

		// A setup is invalidated before confirmation if its low has already been touched.
		if (_pending is { } invalidated && candle.LowPrice <= invalidated.Low)
			_pending = null;
		if (_pending is { } confirmation && candle.ClosePrice > confirmation.High)
		{
			_stopLoss = confirmation.Low;
			_pending = null;
			BuyMarket();
			return;
		}

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var reversal = candle.LowPrice < previousLow && candle.ClosePrice > median
			&& candle.HighPrice < Math.Min(jaw, Math.Min(teeth, lips)) && lips > previousLips;
		var aoFilter = !EnableAo || ao is decimal a && previousAo is decimal pa && a > pa;
		var squat = !EnableMfi || mfi is decimal m && previousMfi is decimal pm && m < pm && candle.TotalVolume > previousVolume;
		if (reversal && aoFilter && squat)
			_pending = (candle.HighPrice, candle.LowPrice);
	}

	private void ProcessQuote(Level1ChangeMessage message)
	{
		if (Position == 0m)
		{
			_exitPending = false;
			return;
		}
		if (Position > 0m && _stopLoss > 0m
			&& message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid <= _stopLoss)
			CloseLong();
	}

	private void CloseLong()
	{
		if (_exitPending || Position <= 0m)
			return;
		_exitPending = true;
		SellMarket(Position);
	}
}
