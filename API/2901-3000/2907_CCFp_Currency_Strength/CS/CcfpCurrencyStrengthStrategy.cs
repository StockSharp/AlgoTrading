using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCFp-style relative currency strength across the seven USD majors.
/// </summary>
public class CcfpCurrencyStrengthStrategy : Strategy
{
	private readonly StrategyParam<string> _eurusd;
	private readonly StrategyParam<string> _gbpusd;
	private readonly StrategyParam<string> _audusd;
	private readonly StrategyParam<string> _nzdusd;
	private readonly StrategyParam<string> _usdcad;
	private readonly StrategyParam<string> _usdchf;
	private readonly StrategyParam<string> _usdjpy;
	private readonly StrategyParam<int> _fastMa;
	private readonly StrategyParam<int> _slowMa;
	private readonly StrategyParam<decimal> _strengthStep;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<string, PairState> _pairs = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, decimal> _previousStrengths = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _directions = new(StringComparer.OrdinalIgnoreCase);

	public string EURUSD { get => _eurusd.Value; set => _eurusd.Value = value; }
	public string GBPUSD { get => _gbpusd.Value; set => _gbpusd.Value = value; }
	public string AUDUSD { get => _audusd.Value; set => _audusd.Value = value; }
	public string NZDUSD { get => _nzdusd.Value; set => _nzdusd.Value = value; }
	public string USDCAD { get => _usdcad.Value; set => _usdcad.Value = value; }
	public string USDCHF { get => _usdchf.Value; set => _usdchf.Value = value; }
	public string USDJPY { get => _usdjpy.Value; set => _usdjpy.Value = value; }
	public int FastMa { get => _fastMa.Value; set => _fastMa.Value = value; }
	public int SlowMa { get => _slowMa.Value; set => _slowMa.Value = value; }
	public decimal StrengthStep { get => _strengthStep.Value; set => _strengthStep.Value = value; }
	public bool CloseOpposite { get => _closeOpposite.Value; set => _closeOpposite.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CcfpCurrencyStrengthStrategy()
	{
		_eurusd = Param(nameof(EURUSD), "EURUSD").SetDisplay("EURUSD", "EUR/USD security id.", "Securities");
		_gbpusd = Param(nameof(GBPUSD), "GBPUSD").SetDisplay("GBPUSD", "GBP/USD security id.", "Securities");
		_audusd = Param(nameof(AUDUSD), "AUDUSD").SetDisplay("AUDUSD", "AUD/USD security id.", "Securities");
		_nzdusd = Param(nameof(NZDUSD), "NZDUSD").SetDisplay("NZDUSD", "NZD/USD security id.", "Securities");
		_usdcad = Param(nameof(USDCAD), "USDCAD").SetDisplay("USDCAD", "USD/CAD security id.", "Securities");
		_usdchf = Param(nameof(USDCHF), "USDCHF").SetDisplay("USDCHF", "USD/CHF security id.", "Securities");
		_usdjpy = Param(nameof(USDJPY), "USDJPY").SetDisplay("USDJPY", "USD/JPY security id.", "Securities");
		_fastMa = Param(nameof(FastMa), 5).SetGreaterThanZero().SetDisplay("Fast MA", "Fast SMA period.", "Indicators");
		_slowMa = Param(nameof(SlowMa), 20).SetGreaterThanZero().SetDisplay("Slow MA", "Slow SMA period.", "Indicators");
		_strengthStep = Param(nameof(StrengthStep), 0.001m).SetGreaterThanZero().SetDisplay("Strength Step", "Minimum top/down strength spread.", "Signal");
		_closeOpposite = Param(nameof(CloseOpposite), true).SetDisplay("Close Opposite", "Close opposite exposure before entering.", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Common timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var (_, id, _, _) in PairDefinitions())
			yield return (Resolve(id), CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_pairs.Clear();
		_previousStrengths.Clear();
		_directions.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		foreach (var (name, id, baseCurrency, quoteCurrency) in PairDefinitions())
		{
			var security = Resolve(id);
			var fast = new SimpleMovingAverage { Length = FastMa };
			var slow = new SimpleMovingAverage { Length = SlowMa };
			var state = new PairState(name, security, baseCurrency, quoteCurrency);
			_pairs[name] = state;

			SubscribeCandles(CandleType, security: security)
				.Bind(fast, slow, (candle, fastValue, slowValue) =>
				{
					if (candle.State != CandleStates.Finished || !fast.IsFormed || !slow.IsFormed || slowValue == 0m)
						return;

					state.Ratio = (fastValue - slowValue) / slowValue;
					state.HasValue = true;

					if (_pairs.Values.All(p => p.HasValue))
						Evaluate();
				})
				.Start();
		}
	}

	private void Evaluate()
	{
		var strengths = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
		{
			["USD"] = 0m, ["EUR"] = 0m, ["GBP"] = 0m, ["CHF"] = 0m,
			["JPY"] = 0m, ["AUD"] = 0m, ["CAD"] = 0m, ["NZD"] = 0m,
		};

		foreach (var pair in _pairs.Values)
		{
			strengths[pair.BaseCurrency] += pair.Ratio;
			strengths[pair.QuoteCurrency] -= pair.Ratio;
		}

		var top = strengths.MaxBy(p => p.Value);
		var down = strengths.MinBy(p => p.Value);
		var spread = top.Value - down.Value;

		if (_previousStrengths.Count == strengths.Count)
		{
			var previousSpread = _previousStrengths[top.Key] - _previousStrengths[down.Key];
			var topRising = top.Value > _previousStrengths[top.Key];
			var downFalling = down.Value < _previousStrengths[down.Key];

			if (previousSpread < StrengthStep && spread >= StrengthStep && topRising && downFalling)
				TradeCurrencySpread(top.Key, down.Key);
		}

		_previousStrengths.Clear();
		foreach (var pair in strengths)
			_previousStrengths[pair.Key] = pair.Value;
	}

	private void TradeCurrencySpread(string top, string down)
	{
		if (top.EqualsIgnoreCase("USD"))
			TradeAgainstUsd(down, wantCurrencyLong: false);
		else if (down.EqualsIgnoreCase("USD"))
			TradeAgainstUsd(top, wantCurrencyLong: true);
		else
		{
			TradeAgainstUsd(top, wantCurrencyLong: true);
			TradeAgainstUsd(down, wantCurrencyLong: false);
		}
	}

	private void TradeAgainstUsd(string currency, bool wantCurrencyLong)
	{
		var pair = _pairs.Values.FirstOrDefault(p =>
			(p.BaseCurrency.EqualsIgnoreCase(currency) && p.QuoteCurrency.EqualsIgnoreCase("USD")) ||
			(p.BaseCurrency.EqualsIgnoreCase("USD") && p.QuoteCurrency.EqualsIgnoreCase(currency)));

		if (pair is null)
			return;

		var currencyIsBase = pair.BaseCurrency.EqualsIgnoreCase(currency);
		var buyPair = wantCurrencyLong == currencyIsBase;
		Submit(pair.Security, buyPair ? Sides.Buy : Sides.Sell);
	}

	private void Submit(Security security, Sides side)
	{
		var desired = side == Sides.Buy ? 1 : -1;
		var current = _directions.TryGetValue(security.Id, out var direction) ? direction : 0;
		var volume = Volume;

		if (current != 0 && current != desired)
		{
			if (!CloseOpposite)
				return;

			volume *= 2m;
		}
		else if (current == desired)
			return;

		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = "(TOPDOWN)",
		});

		_directions[security.Id] = desired;
	}

	private Security Resolve(string id)
		=> this.LookupById(id) ?? new Security { Id = id };

	private IEnumerable<(string Name, string Id, string Base, string Quote)> PairDefinitions()
	{
		yield return ("EURUSD", EURUSD, "EUR", "USD");
		yield return ("GBPUSD", GBPUSD, "GBP", "USD");
		yield return ("AUDUSD", AUDUSD, "AUD", "USD");
		yield return ("NZDUSD", NZDUSD, "NZD", "USD");
		yield return ("USDCAD", USDCAD, "USD", "CAD");
		yield return ("USDCHF", USDCHF, "USD", "CHF");
		yield return ("USDJPY", USDJPY, "USD", "JPY");
	}

	private sealed class PairState(string name, Security security, string baseCurrency, string quoteCurrency)
	{
		public string Name { get; } = name;
		public Security Security { get; } = security;
		public string BaseCurrency { get; } = baseCurrency;
		public string QuoteCurrency { get; } = quoteCurrency;
		public decimal Ratio { get; set; }
		public bool HasValue { get; set; }
	}
}
