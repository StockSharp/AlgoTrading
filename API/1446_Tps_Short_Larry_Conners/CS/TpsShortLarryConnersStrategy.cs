using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Conners TPS short strategy with scaling entries.
/// </summary>
public class TpsShortLarryConnersStrategy : Strategy
{
	private readonly StrategyParam<int> _sma200Len;
	private readonly StrategyParam<int> _rsiLen;
	private readonly StrategyParam<int> _sma10Len;
	private readonly StrategyParam<int> _sma30Len;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma200;
	private RSI _rsi;
	private SMA _sma10;
	private SMA _sma30;

	private decimal _prevRsi;
	private decimal _prevSma10;
	private decimal _prevSma30;

	private decimal? _entryPrice;
	private int _scaleStep;

	public int Sma200Length { get => _sma200Len.Value; set => _sma200Len.Value = value; }
	public int RsiLength { get => _rsiLen.Value; set => _rsiLen.Value = value; }
	public int Sma10Length { get => _sma10Len.Value; set => _sma10Len.Value = value; }
	public int Sma30Length { get => _sma30Len.Value; set => _sma30Len.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TpsShortLarryConnersStrategy()
	{
		_sma200Len = Param(nameof(Sma200Length), 200);
		_rsiLen = Param(nameof(RsiLength), 2);
		_sma10Len = Param(nameof(Sma10Length), 10);
		_sma30Len = Param(nameof(Sma30Length), 30);
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
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
		_entryPrice = null;
		_scaleStep = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma200 = new SMA { Length = Sma200Length };
		_rsi = new RSI { Length = RsiLength };
		_sma10 = new SMA { Length = Sma10Length };
		_sma30 = new SMA { Length = Sma30Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma200, _rsi, _sma10, _sma30, Process).Start();
	}

	private void Process(ICandleMessage candle, decimal sma200, decimal rsi, decimal sma10, decimal sma30)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma200.IsFormed || !_rsi.IsFormed || !_sma10.IsFormed || !_sma30.IsFormed)
			return;

		var rsiAbove75TwoDays = _prevRsi > 75m && rsi > 75m;
		var belowSma200 = candle.ClosePrice < sma200;

		if (belowSma200 && rsiAbove75TwoDays && Position == 0)
		{
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_scaleStep = 0;
		}
		else if (_entryPrice.HasValue && candle.ClosePrice > _entryPrice && _scaleStep < 3)
		{
			SellMarket(Volume);
			_scaleStep++;
		}

		var rsiExit = rsi < 30m;
		var smaCross = _prevSma10 <= _prevSma30 && sma10 > sma30;

		if ((rsiExit || smaCross) && Position != 0)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
			_scaleStep = 0;
		}

		_prevRsi = rsi;
		_prevSma10 = sma10;
		_prevSma30 = sma30;
	}
}

