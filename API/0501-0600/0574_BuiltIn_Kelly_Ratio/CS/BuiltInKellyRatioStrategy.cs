using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Built-in Kelly Ratio strategy.
/// The channel is an EMA (or SMA when UseEma is off) of Length closes plus and minus Multiplier times ATR(AtrLength). A close
/// crossing above the upper band goes long and a close crossing below the lower band goes short, reversing an opposite position.
/// With UseKelly the order size is Volume times the Kelly ratio W - (1 - W) / R of the closed trades (W = win rate, R = average
/// win / average loss); until both a win and a loss exist the full Volume is used, and a non-positive ratio opens nothing.
/// Optional percent take profit and stop loss protect the position.
/// </summary>
public class BuiltInKellyRatioStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<bool> _useKelly;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	private decimal _lastPosition;
	private decimal _pnlAtOpen;
	private int _wins;
	private int _losses;
	private decimal _grossWin;
	private decimal _grossLoss;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// ATR multiple of the band width.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Use EMA instead of SMA as the channel center.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// Size orders by the Kelly ratio.
	/// </summary>
	public bool UseKelly
	{
		get => _useKelly.Value;
		set => _useKelly.Value = value;
	}

	/// <summary>
	/// Enable the take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable the stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BuiltInKellyRatioStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Moving average period", "Channel");

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR multiple of the band width", "Channel");

		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Channel");

		_useEma = Param(nameof(UseEma), true)
			.SetDisplay("Use EMA", "Use EMA instead of SMA as the channel center", "Channel");

		_useKelly = Param(nameof(UseKelly), true)
			.SetDisplay("Use Kelly", "Size orders by the Kelly ratio", "Money Management");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable the take profit", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Use Stop Loss", "Enable the stop loss", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit in percent", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk");

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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		IIndicator ma = UseEma
			? new ExponentialMovingAverage { Length = Length }
			: new SimpleMovingAverage { Length = Length };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, atr, ProcessCandle)
			.Start();

		var take = UseTakeProfit && TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Percent) : new Unit();
		var stop = UseStopLoss && StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Percent) : new Unit();
		if (UseTakeProfit || UseStopLoss)
			StartProtection(take, stop, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_lastPosition = 0;
		_pnlAtOpen = 0;
		_wins = 0;
		_losses = 0;
		_grossWin = 0;
		_grossLoss = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var upper = ma + atr * Multiplier;
		var lower = ma - atr * Multiplier;

		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;

		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;

		if (prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (pc <= pu && close > upper && Position <= 0)
		{
			var volume = GetEntryVolume() + Math.Abs(Position);
			if (volume > 0)
				BuyMarket(volume);
		}
		else if (pc >= pl && close < lower && Position >= 0)
		{
			var volume = GetEntryVolume() + Math.Abs(Position);
			if (volume > 0)
				SellMarket(volume);
		}
	}

	private decimal GetEntryVolume()
	{
		if (!UseKelly || _wins == 0 || _losses == 0)
			return Volume;

		var winRate = (decimal)_wins / (_wins + _losses);
		var avgWin = _grossWin / _wins;
		var avgLoss = _grossLoss / _losses;
		if (avgLoss <= 0)
			return Volume;

		var kelly = winRate - (1m - winRate) / (avgWin / avgLoss);
		if (kelly <= 0)
			return 0;

		var volume = Volume * Math.Min(kelly, 1m);

		if (Security?.VolumeStep is decimal step && step > 0)
			volume = Math.Floor(volume / step) * step;

		if (Security?.MinVolume is decimal minVolume && volume < minVolume)
			volume = minVolume;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var position = Position;

		// A trade is complete when the position returns to flat or flips its side.
		if (_lastPosition != 0 && (position == 0 || Math.Sign(position) != Math.Sign(_lastPosition)))
		{
			var result = PnL - _pnlAtOpen;
			if (result > 0)
			{
				_wins++;
				_grossWin += result;
			}
			else if (result < 0)
			{
				_losses++;
				_grossLoss -= result;
			}

			_pnlAtOpen = PnL;
		}
		else if (_lastPosition == 0 && position != 0)
		{
			_pnlAtOpen = PnL;
		}

		_lastPosition = position;
	}
}
