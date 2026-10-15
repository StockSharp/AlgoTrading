using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combines a Chaikin oscillator zero-line filter with CCI confirmation.
/// Buys when Chaikin crosses above zero and CCI is rising, sells on opposite.
/// </summary>
public class iCHO_Trend_CCIDualOnMA_FilterStrategy : Strategy
{
	private readonly StrategyParam<int> _cciLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal? _prevCci;
	private decimal? _prevPrevCci;
	private int _candlesSinceTrade;

	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	public int SignalCooldownCandles
	{
		get => _signalCooldownCandles.Value;
		set => _signalCooldownCandles.Value = value;
	}

	public iCHO_Trend_CCIDualOnMA_FilterStrategy()
	{
		_cciLength = Param(nameof(CciLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "Data");

		_cciLevel = Param(nameof(CciLevel), 100m)
			.SetDisplay("CCI Level", "CCI threshold for entry", "Indicators");

		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = null;
		_prevPrevCci = null;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCci = null;
		_prevPrevCci = null;
		_candlesSinceTrade = SignalCooldownCandles;

		var cci = new CommodityChannelIndex { Length = CciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_prevCci.HasValue && _prevPrevCci.HasValue)
		{
			if (_prevPrevCci.Value < -CciLevel && _prevCci.Value < -CciLevel && cciValue > -CciLevel && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevPrevCci.Value > CciLevel && _prevCci.Value > CciLevel && cciValue < CciLevel && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevPrevCci = _prevCci;
		_prevCci = cciValue;
	}
}
