using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fundamental quality screener that logs financial ratios and corresponding scores.
/// </summary>
public class QualityScreenStrategy : Strategy
{
	private readonly StrategyParam<string> _period;

	/// <summary>
	/// Financial period used to request values (FY or FQ).
	/// </summary>
	public string Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="QualityScreenStrategy"/> class.
	/// </summary>
	public QualityScreenStrategy()
	{
		_period = Param(nameof(Period), "FY")
			.SetDisplay("Period", "Financial period (FY or FQ)", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Process(Security);
	}

	private void Process(Security security)
	{
		var deq = GetFinancial(security, "DEBT_TO_EQUITY", Period);
		LogInfo($"DEBT_TO_EQUITY={deq} score={ScoreDebtToEquity(deq)}");

		var dta = GetFinancial(security, "DEBT_TO_ASSET", Period);
		LogInfo($"DEBT_TO_ASSET={dta} score={ScoreDebtToAsset(dta)}");

		var ldta = GetFinancial(security, "LONG_TERM_DEBT_TO_ASSETS", Period);
		LogInfo($"LONG_TERM_DEBT_TO_ASSETS={ldta} score={ScoreLongTermDebtToAssets(ldta)}");

		var altz = GetFinancial(security, "ALTMAN_Z_SCORE", Period);
		LogInfo($"ALTMAN_Z_SCORE={altz} score={ScoreAltmanZ(altz)}");

		var springate = GetFinancial(security, "SPRINGATE_SCORE", Period);
		LogInfo($"SPRINGATE_SCORE={springate} score={ScoreSpringate(springate)}");

		var roe = GetFinancial(security, "RETURN_ON_EQUITY", Period);
		LogInfo($"RETURN_ON_EQUITY={roe} score={ScoreReturnOnEquity(roe)}");

		var roa = GetFinancial(security, "RETURN_ON_ASSETS", Period);
		LogInfo($"RETURN_ON_ASSETS={roa} score={ScoreReturnOnAssets(roa)}");

		var roic = GetFinancial(security, "RETURN_ON_INVESTED_CAPITAL", Period);
		LogInfo($"RETURN_ON_INVESTED_CAPITAL={roic} score={ScoreReturnOnInvestedCapital(roic)}");

		var netMargin = GetFinancial(security, "NET_MARGIN", Period);
		LogInfo($"NET_MARGIN={netMargin} score={ScoreNetMargin(netMargin)}");

		var fcfMargin = GetFinancial(security, "FREE_CASH_FLOW_MARGIN", Period);
		LogInfo($"FREE_CASH_FLOW_MARGIN={fcfMargin} score={ScoreFreeCashFlowMargin(fcfMargin)}");

		var currentRatio = GetFinancial(security, "CURRENT_RATIO", Period);
		LogInfo($"CURRENT_RATIO={currentRatio} score={ScoreCurrentRatio(currentRatio)}");

		var quickRatio = GetFinancial(security, "QUICK_RATIO", Period);
		LogInfo($"QUICK_RATIO={quickRatio} score={ScoreQuickRatio(quickRatio)}");

		var sloan = GetFinancial(security, "SLOAN_RATIO", Period);
		LogInfo($"SLOAN_RATIO={sloan} score={ScoreSloanRatio(sloan)}");

		var interestCover = GetFinancial(security, "INTERST_COVER", Period);
		LogInfo($"INTERST_COVER={interestCover} score={ScoreInterestCover(interestCover)}");

		var pfScore = GetFinancial(security, "PIOTROSKI_F_SCORE", Period);
		LogInfo($"PIOTROSKI_F_SCORE={pfScore} score={ScorePiotroski(pfScore)}");

		var sgr = GetFinancial(security, "SUSTAINABLE_GROWTH_RATE", Period);
		LogInfo($"SUSTAINABLE_GROWTH_RATE={sgr} score={ScoreSustainableGrowthRate(sgr)}");
	}

	private decimal? GetFinancial(Security security, string id, string period)
	{
		// TODO: implement fundamental data retrieval
		return null;
	}

	private static int ScoreDebtToEquity(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v > 0m && v <= 1m)
			return 2;
		if (v > 1m && v <= 2m)
			return 1;
		return v > 2m ? -1 : -2;
	}

	private static int ScoreDebtToAsset(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v > 0m && v <= 0.2m)
			return 2;
		if (v > 0.2m && v <= 0.4m)
			return 1;
		if (v > 0.4m && v <= 0.6m)
			return 0;
		return v > 0.6m ? 1 : -2;
	}

	private static int ScoreLongTermDebtToAssets(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v > 0m && v <= 0.2m)
			return 2;
		if (v > 0.2m && v <= 0.4m)
			return 1;
		if (v > 0.4m && v <= 0.6m)
			return 0;
		return v > 0.6m ? 1 : -2;
	}

	private static int ScoreAltmanZ(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v > 5m)
			return 2;
		if (v > 3m)
			return 1;
		if (v > 1.8m)
			return 0;
		return v > 1m ? -1 : -2;
	}

	private static int ScoreSpringate(decimal? value)
	{
		if (value is null)
			return 0;

		return value > 0.862m ? 1 : -1;
	}

	private static int ScoreReturnOnEquity(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 40m)
			return 2;
		if (v >= 10m)
			return 1;
		if (v >= 0m)
			return 0;
		return v >= -20m ? -1 : -2;
	}

	private static int ScoreReturnOnAssets(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 20m)
			return 2;
		if (v >= 5m)
			return 1;
		if (v >= 0m)
			return 0;
		return v >= -10m ? -1 : -2;
	}

	private static int ScoreReturnOnInvestedCapital(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 10m)
			return 2;
		if (v >= 2m)
			return 1;
		if (v >= 0m)
			return 0;
		return v >= -5m ? -1 : -2;
	}

	private static int ScoreNetMargin(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 20m)
			return 2;
		if (v >= 10m)
			return 1;
		if (v >= 0m)
			return 0;
		return v >= -5m ? -1 : -2;
	}

	private static int ScoreFreeCashFlowMargin(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 15m)
			return 2;
		if (v >= 10m)
			return 1;
		if (v >= 0m)
			return 0;
		return v >= -10m ? -1 : -2;
	}

	private static int ScoreCurrentRatio(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 1.2m)
			return v <= 2m ? 2 : 1;
		if (v >= 1m)
			return 0;
		return v >= 0.5m ? -1 : -2;
	}

	private static int ScoreQuickRatio(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 1m)
			return v <= 2m ? 2 : 1;
		if (v >= 0.9m)
			return 0;
		return v >= 0.4m ? -1 : -2;
	}

	private static int ScoreSloanRatio(decimal? value)
	{
		if (value is null)
			return 0;

		var v = Math.Abs(value.Value);
		if (v < 10m)
			return 1;
		if (v < 25m)
			return 0;
		return -1;
	}

	private static int ScoreInterestCover(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v > 3m)
			return 1;
		return v > 2m ? 0 : -1;
	}

	private static int ScorePiotroski(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v >= 8m)
			return 2;
		if (v >= 5m)
			return 1;
		if (v > 2m)
			return 0;
		return v > 1m ? -1 : -2;
	}

	private static int ScoreSustainableGrowthRate(decimal? value)
	{
		if (value is null)
			return 0;

		var v = value.Value;
		if (v > 10m)
			return 2;
		if (v > 5m)
			return 1;
		if (v > 0m)
			return 0;
		return v > -5m ? -1 : -2;
	}
}
