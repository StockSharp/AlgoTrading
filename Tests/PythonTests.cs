namespace StockSharp.Tests;

using System.IO;

using Ecng.Compilation;
using Ecng.Reflection;

using StockSharp.Algo.Compilation;

[TestClass]
public class PythonTests
{
	public static async Task RunStrategy(string filePath, Action<Strategy, Security> extra = null)
	{
		var strategyPath = Path.Combine("../../../../API/", filePath);

		var code = new CodeInfo
		{
			Name = Path.GetFileNameWithoutExtension(strategyPath),
			Text = File.ReadAllText(strategyPath),
			Language = FileExts.Python,
		};

		var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, default);

		foreach (var err in errors.ErrorsOnly())
			throw new InvalidOperationException(err.ToString());

		var strategy = code.ObjectType.CreateInstance<Strategy>();

		await AsmInit.RunStrategy(strategy, extra);
	}

	[TestMethod]
	public Task MaCrossoverStrategy()
		=> RunStrategy("0001_MA_CrossOver/PY/ma_crossover_strategy.py");

	[TestMethod]
	public Task NdayBreakoutStrategy()
		=> RunStrategy("0002_NDay_Breakout/PY/nday_breakout_strategy.py");

	[TestMethod]
	public Task AdxTrendStrategy()
		=> RunStrategy("0003_ADX_Trend/PY/adx_trend_strategy.py");

	[TestMethod]
	public Task ParabolicSarTrendStrategy()
		=> RunStrategy("0004_Parabolic_SAR_Trend/PY/parabolic_sar_trend_strategy.py");

	[TestMethod]
	public Task DonchianChannelStrategy()
		=> RunStrategy("0005_Donchian_Channel/PY/donchian_channel_strategy.py");

	[TestMethod]
	public Task TripleMaStrategy()
		=> RunStrategy("0006_Tripple_MA/PY/triple_ma_strategy.py");

	[TestMethod]
	public Task KeltnerChannelBreakoutStrategy()
		=> RunStrategy("0007_Keltner_Channel_Breakout/PY/keltner_channel_breakout_strategy.py");

	[TestMethod]
	public Task HullMaTrendStrategy()
		=> RunStrategy("0008_Hull_MA_Trend/PY/hull_ma_trend_strategy.py");

	[TestMethod]
	public Task MacdTrendStrategy()
		=> RunStrategy("0009_MACD_Trend/PY/macd_trend_strategy.py");

	[TestMethod]
	public Task SupertrendStrategy()
		=> RunStrategy("0010_Super_Trend/PY/supertrend_strategy.py");

	[TestMethod]
	public Task IchimokuKumoBreakoutStrategy()
		=> RunStrategy("0011_Ichimoku_Kumo_Breakout/PY/ichimoku_kumo_breakout_strategy.py");

	[TestMethod]
	public Task HeikinAshiConsecutiveStrategy()
		=> RunStrategy("0012_Heikin_Ashi_Consecutive/PY/heikin_ashi_consecutive_strategy.py");

	[TestMethod]
	public Task DmiPowerMoveStrategy()
		=> RunStrategy("0013_DMI_Power_Move/PY/dmi_power_move_strategy.py");

	[TestMethod]
	public Task TradingviewSupertrendFlipStrategy()
		=> RunStrategy("0014_TradingView_Supertrend_Flip/PY/tradingview_supertrend_flip_strategy.py");

	[TestMethod]
	public Task GannSwingBreakoutStrategy()
		=> RunStrategy("0015_Gann_Swing_Breakout/PY/gann_swing_breakout_strategy.py");

	[TestMethod]
	public Task RsiDivergenceStrategy()
		=> RunStrategy("0016_RSI_Divergence/PY/rsi_divergence_strategy.py");

	[TestMethod]
	public Task WilliamsPercentRStrategy()
		=> RunStrategy("0017_Williams_R/PY/williams_percent_r_strategy.py");

	[TestMethod]
	public Task RocImpulseStrategy()
		=> RunStrategy("0018_ROC_Impulce/PY/roc_impulse_strategy.py");

	[TestMethod]
	public Task CciBreakoutStrategy()
		=> RunStrategy("0019_CCI_Breakout/PY/cci_breakout_strategy.py");

	[TestMethod]
	public Task MomentumPercentageStrategy()
		=> RunStrategy("0020_Momentum_Percentage/PY/momentum_percentage_strategy.py");

	[TestMethod]
	public Task BollingerSqueezeStrategy()
		=> RunStrategy("0021_Bollinger_Squeeze/PY/bollinger_squeeze_strategy.py");

	[TestMethod]
	public Task AdxDiStrategy()
		=> RunStrategy("0022_ADX_DI/PY/adx_di_strategy.py");

	[TestMethod]
	public Task ElderImpulseStrategy()
		=> RunStrategy("0023_Elder_Impulse/PY/elder_impulse_strategy.py");

	[TestMethod]
	public Task LaguerreRsiStrategy()
		=> RunStrategy("0024_RSI_Laguerre/PY/laguerre_rsi_strategy.py");

	[TestMethod]
	public Task StochasticRsiCrossStrategy()
		=> RunStrategy("0025_Stochastic_RSI_Cross/PY/stochastic_rsi_cross_strategy.py");

	[TestMethod]
	public Task RsiReversionStrategy()
		=> RunStrategy("0026_RSI_Reversion/PY/rsi_reversion_strategy.py");

	[TestMethod]
	public Task BollingerReversionStrategy()
		=> RunStrategy("0027_Bollinger_Reversion/PY/bollinger_reversion_strategy.py");

	[TestMethod]
	public Task ZScoreStrategy()
		=> RunStrategy("0028_ZScore/PY/z_score_strategy.py");

	[TestMethod]
	public Task MaDeviationStrategy()
		=> RunStrategy("0029_MA_Deviation/PY/ma_deviation_strategy.py");

	[TestMethod]
	public Task VwapReversionStrategy()
		=> RunStrategy("0030_VWAP_Reversion/PY/vwap_reversion_strategy.py");

	[TestMethod]
	public Task KeltnerReversionStrategy()
		=> RunStrategy("0031_Keltner_Reversion/PY/keltner_reversion_strategy.py");

	[TestMethod]
	public Task AtrReversionStrategy()
		=> RunStrategy("0032_ATR_Reversion/PY/atr_reversion_strategy.py");

	[TestMethod]
	public Task MacdZeroStrategy()
		=> RunStrategy("0033_MACD_Zero/PY/macd_zero_strategy.py");

	[TestMethod]
	public Task LowVolReversionStrategy()
		=> RunStrategy("0034_Low_Vol_Reversion/PY/low_vol_reversion_strategy.py");

	[TestMethod]
	public Task BollingerPercentBStrategy()
		=> RunStrategy("0035_Bollinger_B_Reversion/PY/bollinger_percent_b_strategy.py");

	[TestMethod]
	public Task AtrExpansionStrategy()
		=> RunStrategy("0036_ATR_Expansion/PY/atr_expansion_strategy.py");

	[TestMethod]
	public Task VixTriggerStrategy()
		=> RunStrategy("0037_VIX_Trigger/PY/vix_trigger_strategy.py");

	[TestMethod]
	public Task BollingerBandWidthStrategy()
		=> RunStrategy("0038_BB_Width/PY/bollinger_band_width_strategy.py");

	[TestMethod]
	public Task HvBreakoutStrategy()
		=> RunStrategy("0039_HV_Breakout/PY/hv_breakout_strategy.py");

	[TestMethod]
	public Task AtrTrailingStrategy()
		=> RunStrategy("0040_ATR_Trailing/PY/atr_trailing_strategy.py");

	[TestMethod]
	public Task VolAdjustedMaStrategy()
		=> RunStrategy("0041_Vol_Adjusted_MA/PY/vol_adjusted_ma_strategy.py");

	[TestMethod]
	public Task IvSpikeStrategy()
		=> RunStrategy("0042_IV_Spike/PY/iv_spike_strategy.py");

	[TestMethod]
	public Task VcpStrategy()
		=> RunStrategy("0043_VCP/PY/vcp_strategy.py");

	[TestMethod]
	public Task AtrRangeStrategy()
		=> RunStrategy("0044_ATR_Range/PY/atr_range_strategy.py");

	[TestMethod]
	public Task ChoppinessIndexBreakoutStrategy()
		=> RunStrategy("0045_Choppiness_Index_Breakout/PY/choppiness_index_breakout_strategy.py");

	[TestMethod]
	public Task VolumeSpikeStrategy()
		=> RunStrategy("0046_Volume_Spike/PY/volume_spike_strategy.py");

	[TestMethod]
	public Task ObvBreakoutStrategy()
		=> RunStrategy("0047_OBV_Breakout/PY/obv_breakout_strategy.py");

	[TestMethod]
	public Task VwapBreakoutStrategy()
		=> RunStrategy("0048_VWAP_Breakout/PY/vwap_breakout_strategy.py");

	[TestMethod]
	public Task VwmaStrategy()
		=> RunStrategy("0049_VWMA/PY/vwma_strategy.py");

	[TestMethod]
	public Task AdStrategy()
		=> RunStrategy("0050_AD/PY/ad_strategy.py");

	[TestMethod]
	public Task VolumeWeightedPriceBreakoutStrategy()
		=> RunStrategy("0051_Volume_Weighted_Price_Breakout/PY/volume_weighted_price_breakout_strategy.py");

	[TestMethod]
	public Task VolumeDivergenceStrategy()
		=> RunStrategy("0052_Volume_Divergence/PY/volume_divergence_strategy.py");

	[TestMethod]
	public Task VolumeMaCrossStrategy()
		=> RunStrategy("0053_Volume_MA_Cross/PY/volume_ma_cross_strategy.py");

	[TestMethod]
	public Task CumulativeDeltaBreakoutStrategy()
		=> RunStrategy("0054_Cumulative_Delta_Breakout/PY/cumulative_delta_breakout_strategy.py");

	[TestMethod]
	public Task VolumeSurgeStrategy()
		=> RunStrategy("0055_Volume_Surge/PY/volume_surge_strategy.py");

	[TestMethod]
	public Task DoubleBottomStrategy()
		=> RunStrategy("0056_Double_Bottom/PY/double_bottom_strategy.py");

	[TestMethod]
	public Task DoubleTopStrategy()
		=> RunStrategy("0057_Double_Top/PY/double_top_strategy.py");

	[TestMethod]
	public Task RsiOverboughtOversoldStrategy()
		=> RunStrategy("0058_RSI_Overbought_Oversold/PY/rsi_overbought_oversold_strategy.py");

	[TestMethod]
	public Task HammerCandleStrategy()
		=> RunStrategy("0059_Hammer_Candle/PY/hammer_candle_strategy.py");

	[TestMethod]
	public Task ShootingStarStrategy()
		=> RunStrategy("0060_Shooting_Star/PY/shooting_star_strategy.py");

	[TestMethod]
	public Task MacdDivergenceStrategy()
		=> RunStrategy("0061_MACD_Divergence/PY/macd_divergence_strategy.py");

	[TestMethod]
	public Task StochasticOverboughtOversoldStrategy()
		=> RunStrategy("0062_Stochastic_Overbought_Oversold/PY/stochastic_overbought_oversold_strategy.py");

	[TestMethod]
	public Task EngulfingBullishStrategy()
		=> RunStrategy("0063_Engulfing_Bullish/PY/engulfing_bullish_strategy.py");

	[TestMethod]
	public Task EngulfingBearishStrategy()
		=> RunStrategy("0064_Engulfing_Bearish/PY/engulfing_bearish_strategy.py");

	[TestMethod]
	public Task PinbarReversalStrategy()
		=> RunStrategy("0065_Pinbar_Reversal/PY/pinbar_reversal_strategy.py");

	[TestMethod]
	public Task ThreeBarReversalUpStrategy()
		=> RunStrategy("0066_Three_Bar_Reversal_Up/PY/three_bar_reversal_up_strategy.py");

	[TestMethod]
	public Task ThreeBarReversalDownStrategy()
		=> RunStrategy("0067_Three_Bar_Reversal_Down/PY/three_bar_reversal_down_strategy.py");

	[TestMethod]
	public Task CciDivergenceStrategy()
		=> RunStrategy("0068_CCI_Divergence/PY/cci_divergence_strategy.py");

	[TestMethod]
	public Task BollingerBandReversalStrategy()
		=> RunStrategy("0069_Bollinger_Band_Reversal/PY/bollinger_band_reversal_strategy.py");

	[TestMethod]
	public Task MorningStarStrategy()
		=> RunStrategy("0070_Morning_Star/PY/morning_star_strategy.py");

	[TestMethod]
	public Task EveningStarStrategy()
		=> RunStrategy("0071_Evening_Star/PY/evening_star_strategy.py");

	[TestMethod]
	public Task DojiReversalStrategy()
		=> RunStrategy("0072_Doji_Reversal/PY/doji_reversal_strategy.py");

	[TestMethod]
	public Task KeltnerChannelReversalStrategy()
		=> RunStrategy("0073_Keltner_Channel_Reversal/PY/keltner_channel_reversal_strategy.py");

	[TestMethod]
	public Task WilliamsRStrategy()
		=> RunStrategy("0074_Williams_R_Divergence/PY/williams_percent_r_divergence_strategy.py");

	[TestMethod]
	public Task ObvStrategy()
		=> RunStrategy("0075_OBV_Divergence/PY/obv_divergence_strategy.py");

	[TestMethod]
	public Task FibonacciStrategy()
		=> RunStrategy("0076_Fibonacci_Retracement_Reversal/PY/fibonacci_retracement_reversal_strategy.py");

	[TestMethod]
	public Task InsideBarStrategy()
		=> RunStrategy("0077_Inside_Bar_Breakout/PY/inside_bar_breakout_strategy.py");

	[TestMethod]
	public Task OutsideBarStrategy()
		=> RunStrategy("0078_Outside_Bar_Reversal/PY/outside_bar_reversal_strategy.py");

	[TestMethod]
	public Task TrendlineStrategy()
		=> RunStrategy("0079_Trendline_Bounce/PY/trendline_bounce_strategy.py");

	[TestMethod]
	public Task PivotPointStrategy()
		=> RunStrategy("0080_Pivot_Point_Reversal/PY/pivot_point_reversal_strategy.py");

	[TestMethod]
	public Task VwapBounceStrategy()
		=> RunStrategy("0081_VWAP_Bounce/PY/vwap_bounce_strategy.py");

	[TestMethod]
	public Task VolumeExhaustionStrategy()
		=> RunStrategy("0082_Volume_Exhaustion/PY/volume_exhaustion_strategy.py");

	[TestMethod]
	public Task AdxWeakeningStrategy()
		=> RunStrategy("0083_ADX_Weakening/PY/adx_weakening_strategy.py");

	[TestMethod]
	public Task AtrExhaustionStrategy()
		=> RunStrategy("0084_ATR_Exhaustion/PY/atr_exhaustion_strategy.py");

	[TestMethod]
	public Task IchimokuTenkanKijunStrategy()
		=> RunStrategy("0085_Ichimoku_Tenkan/PY/ichimoku_tenkan_kijun_strategy.py");

	[TestMethod]
	public Task HeikinAshiReversalStrategy()
		=> RunStrategy("0086_Heikin_Ashi_Reversal/PY/heikin_ashi_reversal_strategy.py");

	[TestMethod]
	public Task ParabolicSarReversalStrategy()
		=> RunStrategy("0087_Parabolic_SAR_Reversal/PY/parabolic_sar_reversal_strategy.py");

	[TestMethod]
	public Task SupertrendReversalStrategy()
		=> RunStrategy("0088_Supertrend_Reversal/PY/supertrend_reversal_strategy.py");

	[TestMethod]
	public Task HullMaReversalStrategy()
		=> RunStrategy("0089_Hull_MA_Reversal/PY/hull_ma_reversal_strategy.py");

	[TestMethod]
	public Task DonchianReversalStrategy()
		=> RunStrategy("0090_Donchian_Reversal/PY/donchian_reversal_strategy.py");

	[TestMethod]
	public Task MacdHistogramReversalStrategy()
		=> RunStrategy("0091_MACD_Histogram_Reversal/PY/macd_histogram_reversal_strategy.py");

	[TestMethod]
	public Task RsiHookReversalStrategy()
		=> RunStrategy("0092_RSI_Hook_Reversal/PY/rsi_hook_reversal_strategy.py");

	[TestMethod]
	public Task StochasticHookReversalStrategy()
		=> RunStrategy("0093_Stochastic_Hook_Reversal/PY/stochastic_hook_reversal_strategy.py");

	[TestMethod]
	public Task CciHookReversalStrategy()
		=> RunStrategy("0094_CCI_Hook_Reversal/PY/cci_hook_reversal_strategy.py");

	[TestMethod]
	public Task WilliamsRHookReversalStrategy()
		=> RunStrategy("0095_Williams_R_Hook_Reversal/PY/williams_r_hook_reversal_strategy.py");

	[TestMethod]
	public Task ThreeWhiteSoldiersStrategy()
		=> RunStrategy("0096_Three_White_Soldiers/PY/three_white_soldiers_strategy.py");

	[TestMethod]
	public Task ThreeBlackCrowsStrategy()
		=> RunStrategy("0097_Three_Black_Crows/PY/three_black_crows_strategy.py");

	[TestMethod]
	public Task GapFillReversalStrategy()
		=> RunStrategy("0098_Gap_Fill_Reversal/PY/gap_fill_reversal_strategy.py");

	[TestMethod]
	public Task TweezerBottomStrategy()
		=> RunStrategy("0099_Tweezer_Bottom/PY/tweezer_bottom_strategy.py");

	[TestMethod]
	public Task TweezerTopStrategy()
		=> RunStrategy("0100_Tweezer_Top/PY/tweezer_top_strategy.py");

	[TestMethod]
	public Task HaramiBullishStrategy()
		=> RunStrategy("0101_Harami_Bullish/PY/harami_bullish_strategy.py");

	[TestMethod]
	public Task HaramiBearishStrategy()
		=> RunStrategy("0102_Harami_Bearish/PY/harami_bearish_strategy.py");

	[TestMethod]
	public Task DarkPoolPrintsStrategy()
		=> RunStrategy("0103_Dark_Pool_Prints/PY/dark_pool_prints_strategy.py");

	[TestMethod]
	public Task RejectionCandleStrategy()
		=> RunStrategy("0104_Rejection_Candle/PY/rejection_candle_strategy.py");

	[TestMethod]
	public Task FalseBreakoutTrapStrategy()
		=> RunStrategy("0105_False_Breakout_Trap/PY/false_breakout_trap_strategy.py");

	[TestMethod]
	public Task SpringReversalStrategy()
		=> RunStrategy("0106_Spring_Reversal/PY/spring_reversal_strategy.py");

	[TestMethod]
	public Task UpthrustReversalStrategy()
		=> RunStrategy("0107_Upthrust_Reversal/PY/upthrust_reversal_strategy.py");

	[TestMethod]
	public Task WyckoffAccumulationStrategy()
		=> RunStrategy("0108_Wyckoff_Accumulation/PY/wyckoff_accumulation_strategy.py");

	[TestMethod]
	public Task WyckoffDistributionStrategy()
		=> RunStrategy("0109_Wyckoff_Distribution/PY/wyckoff_distribution_strategy.py");

	[TestMethod]
	public Task RsiFailureSwingStrategy()
		=> RunStrategy("0110_RSI_Failure_Swing/PY/rsi_failure_swing_strategy.py");

	[TestMethod]
	public Task StochasticFailureSwingStrategy()
		=> RunStrategy("0111_Stochastic_Failure_Swing/PY/stochastic_failure_swing_strategy.py");

	[TestMethod]
	public Task CciFailureSwingStrategy()
		=> RunStrategy("0112_CCI_Failure_Swing/PY/cci_failure_swing_strategy.py");

	[TestMethod]
	public Task BullishAbandonedBabyStrategy()
		=> RunStrategy("0113_Bullish_Abandoned_Baby/PY/bullish_abandoned_baby_strategy.py");

	[TestMethod]
	public Task BearishAbandonedBabyStrategy()
		=> RunStrategy("0114_Bearish_Abandoned_Baby/PY/bearish_abandoned_baby_strategy.py");

	[TestMethod]
	public Task VolumeClimaxReversalStrategy()
		=> RunStrategy("0115_Volume_Climax_Reversal/PY/volume_climax_reversal_strategy.py");

	[TestMethod]
	public Task DayOfWeekStrategy()
		=> RunStrategy("0116_Day_of_Week/PY/day_of_week_strategy.py");

	[TestMethod]
	public Task MonthOfYearStrategy()
		=> RunStrategy("0117_Month_of_Year/PY/month_of_year_strategy.py");

	[TestMethod]
	public Task TurnaroundTuesdayStrategy()
		=> RunStrategy("0118_Turnaround_Tuesday/PY/turnaround_tuesday_strategy.py");

	[TestMethod]
	public Task EndOfMonthStrengthStrategy()
		=> RunStrategy("0119_End_of_Month_Strength/PY/end_of_month_strength_strategy.py");

	[TestMethod]
	public Task FirstDayOfMonthStrategy()
		=> RunStrategy("0120_First_Day_of_Month/PY/first_day_of_month_strategy.py");

	[TestMethod]
	public Task SantaClausRallyStrategy()
		=> RunStrategy("0121_Santa_Claus_Rally/PY/santa_claus_rally_strategy.py");

	[TestMethod]
	public Task JanuaryEffectStrategy()
		=> RunStrategy("0122_January_Effect/PY/january_effect_strategy.py");

	[TestMethod]
	public Task MondayWeaknessStrategy()
		=> RunStrategy("0123_Monday_Weakness/PY/monday_weakness_strategy.py");

	[TestMethod]
	public Task PreHolidayStrengthStrategy()
		=> RunStrategy("0124_Pre-Holiday_Strength/PY/pre_holiday_strength_strategy.py");

	[TestMethod]
	public Task PostHolidayWeaknessStrategy()
		=> RunStrategy("0125_Post-Holiday_Weakness/PY/post_holiday_weakness_strategy.py");

	[TestMethod]
	public Task QuarterlyExpiryStrategy()
		=> RunStrategy("0126_Quarterly_Expiry/PY/quarterly_expiry_strategy.py");

	[TestMethod]
	public Task OpenDriveStrategy()
		=> RunStrategy("0127_Open_Drive/PY/open_drive_strategy.py");

	[TestMethod]
	public Task MiddayReversalStrategy()
		=> RunStrategy("0128_Midday_Reversal/PY/midday_reversal_strategy.py");

	[TestMethod]
	public Task OvernightGapStrategy()
		=> RunStrategy("0129_Overnight_Gap/PY/overnight_gap_strategy.py");

	[TestMethod]
	public Task LunchBreakFadeStrategy()
		=> RunStrategy("0130_Lunch_Break_Fade/PY/lunch_break_fade_strategy.py");

	[TestMethod]
	public Task MacdRsiStrategy()
		=> RunStrategy("0131_MACD_RSI/PY/macd_rsi_strategy.py");

	[TestMethod]
	public Task BollingerStochasticStrategy()
		=> RunStrategy("0132_Bollinger_Stochastic/PY/bollinger_stochastic_strategy.py");

	[TestMethod]
	public Task MaVolumeStrategy()
		=> RunStrategy("0133_MA_Volume/PY/ma_volume_strategy.py");

	[TestMethod]
	public Task AdxMacdStrategy()
		=> RunStrategy("0134_ADX_MACD/PY/adx_macd_strategy.py");

	[TestMethod]
	public Task IchimokuRsiStrategy()
		=> RunStrategy("0135_Ichimoku_RSI/PY/ichimoku_rsi_strategy.py");

	[TestMethod]
	public Task SupertrendVolumeStrategy()
		=> RunStrategy("0136_Supertrend_Volume/PY/supertrend_volume_strategy.py");

	[TestMethod]
	public Task BollingerRsiStrategy()
		=> RunStrategy("0137_Bollinger_RSI/PY/bollinger_rsi_strategy.py");

	[TestMethod]
	public Task MaStochasticStrategy()
		=> RunStrategy("0138_MA_Stochastic/PY/ma_stochastic_strategy.py");

	[TestMethod]
	public Task AtrMacdStrategy()
		=> RunStrategy("0139_ATR_MACD/PY/atr_macd_strategy.py");

	[TestMethod]
	public Task VwapRsiStrategy()
		=> RunStrategy("0140_VWAP_RSI/PY/vwap_rsi_strategy.py");

	[TestMethod]
	public Task DonchianVolumeStrategy()
		=> RunStrategy("0141_Donchian_Volume/PY/donchian_volume_strategy.py");

	[TestMethod]
	public Task KeltnerStochasticStrategy()
		=> RunStrategy("0142_Keltner_Stochastic/PY/keltner_stochastic_strategy.py");

	[TestMethod]
	public Task ParabolicSarRsiStrategy()
		=> RunStrategy("0143_Parabolic_SAR_RSI/PY/parabolic_sar_rsi_strategy.py");

	[TestMethod]
	public Task HullMaVolumeStrategy()
		=> RunStrategy("0144_Hull_MA_Volume/PY/hull_ma_volume_strategy.py");

	[TestMethod]
	public Task AdxStochasticStrategy()
		=> RunStrategy("0145_ADX_Stochastic/PY/adx_stochastic_strategy.py");

	[TestMethod]
	public Task MacdVolumeStrategy()
		=> RunStrategy("0146_MACD_Volume/PY/macd_volume_strategy.py");

	[TestMethod]
	public Task BollingerVolumeStrategy()
		=> RunStrategy("0147_Bollinger_Volume/PY/bollinger_volume_strategy.py");

	[TestMethod]
	public Task RsiStochasticStrategy()
		=> RunStrategy("0148_RSI_Stochastic/PY/rsi_stochastic_strategy.py");

	[TestMethod]
	public Task MaAdxStrategy()
		=> RunStrategy("0149_MA_ADX/PY/ma_adx_strategy.py");

	[TestMethod]
	public Task VwapStochasticStrategy()
		=> RunStrategy("0150_VWAP_Stochastic/PY/vwap_stochastic_strategy.py");

	[TestMethod]
	public Task IchimokuVolumeStrategy()
		=> RunStrategy("0151_Ichimoku_Volume/PY/ichimoku_volume_strategy.py");

	[TestMethod]
	public Task SupertrendRsiStrategy()
		=> RunStrategy("0152_Supertrend_RSI/PY/supertrend_rsi_strategy.py");

	[TestMethod]
	public Task BollingerAdxStrategy()
		=> RunStrategy("0153_Bollinger_ADX/PY/bollinger_adx_strategy.py");

	[TestMethod]
	public Task MaCciStrategy()
		=> RunStrategy("0154_MA_CCI/PY/ma_cci_strategy.py");

	[TestMethod]
	public Task VwapVolumeStrategy()
		=> RunStrategy("0155_VWAP_Volume/PY/vwap_volume_strategy.py");

	[TestMethod]
	public Task DonchianRsiStrategy()
		=> RunStrategy("0156_Donchian_RSI/PY/donchian_rsi_strategy.py");

	[TestMethod]
	public Task KeltnerVolumeStrategy()
		=> RunStrategy("0157_Keltner_Volume/PY/keltner_volume_strategy.py");

	[TestMethod]
	public Task ParabolicSarStochasticStrategy()
		=> RunStrategy("0158_Parabolic_SAR_Stochastic/PY/parabolic_sar_stochastic_strategy.py");

	[TestMethod]
	public Task HullMaRsiStrategy()
		=> RunStrategy("0159_Hull_MA_RSI/PY/hull_ma_rsi_strategy.py");

	[TestMethod]
	public Task AdxVolumeStrategy()
		=> RunStrategy("0160_ADX_Volume/PY/adx_volume_strategy.py");

	[TestMethod]
	public Task MacdCciStrategy()
		=> RunStrategy("0161_MACD_CCI/PY/macd_cci_strategy.py");

	[TestMethod]
	public Task BollingerCciStrategy()
		=> RunStrategy("0162_Bollinger_CCI/PY/bollinger_cci_strategy.py");

	[TestMethod]
	public Task RsiWilliamsRStrategy()
		=> RunStrategy("0163_RSI_Williams_R/PY/rsi_williams_r_strategy.py");

	[TestMethod]
	public Task MaWilliamsRStrategy()
		=> RunStrategy("0164_MA_Williams_R/PY/ma_williams_r_strategy.py");

	[TestMethod]
	public Task VwapCciStrategy()
		=> RunStrategy("0165_VWAP_CCI/PY/vwap_cci_strategy.py");

	[TestMethod]
	public Task DonchianStochasticStrategy()
		=> RunStrategy("0166_Donchian_Stochastic/PY/donchian_stochastic_strategy.py");

	[TestMethod]
	public Task KeltnerRsiStrategy()
		=> RunStrategy("0167_Keltner_RSI/PY/keltner_rsi_strategy.py");

	[TestMethod]
	public Task HullMaStochasticStrategy()
		=> RunStrategy("0169_Hull_MA_Stochastic/PY/hull_ma_stochastic_strategy.py");

	[TestMethod]
	public Task AdxCciStrategy()
		=> RunStrategy("0170_ADX_CCI/PY/adx_cci_strategy.py");

	[TestMethod]
	public Task MacdWilliamsRStrategy()
		=> RunStrategy("0171_MACD_Williams_R/PY/macd_williams_r_strategy.py");

	[TestMethod]
	public Task BollingerWilliamsRStrategy()
		=> RunStrategy("0172_Bollinger_Williams_R/PY/bollinger_williams_r_strategy.py");

	[TestMethod]
	public Task MacdVwapStrategy()
		=> RunStrategy("0174_MACD_VWAP/PY/macd_vwap_strategy.py");

	[TestMethod]
	public Task RsiSupertrendStrategy()
		=> RunStrategy("0175_RSI_Supertrend/PY/rsi_supertrend_strategy.py");

	[TestMethod]
	public Task AdxBollingerStrategy()
		=> RunStrategy("0176_ADX_Bollinger/PY/adx_bollinger_strategy.py");

	[TestMethod]
	public Task IchimokuStochasticStrategy()
		=> RunStrategy("0177_Ichimoku_Stochastic/PY/ichimoku_stochastic_strategy.py");

	[TestMethod]
	public Task SupertrendStochasticStrategy()
		=> RunStrategy("0185_Supertrend_Stochastic/PY/supertrend_stochastic_strategy.py");

	[TestMethod]
	public Task DonchianMacdStrategy()
		=> RunStrategy("0187_Donchian_MACD/PY/donchian_macd_strategy.py");

	[TestMethod]
	public Task ParabolicSarVolumeStrategy()
		=> RunStrategy("0188_Parabolic_SAR_Volume/PY/parabolic_sar_volume_strategy.py");

	[TestMethod]
	public Task VwapAdxStrategy()
		=> RunStrategy("0190_VWAP_ADX/PY/vwap_adx_strategy.py");

	[TestMethod]
	public Task SupertrendAdxStrategy()
		=> RunStrategy("0193_Supertrend_ADX/PY/supertrend_adx_strategy.py");

	[TestMethod]
	public Task KeltnerMacdStrategy()
		=> RunStrategy("0194_Keltner_MACD/PY/keltner_macd_strategy.py");

	[TestMethod]
	public Task HullMaAdxStrategy()
		=> RunStrategy("0197_Hull_MA_ADX/PY/hull_ma_adx_strategy.py");

	[TestMethod]
	public Task VwapMacdStrategy()
		=> RunStrategy("0198_VWAP_MACD/PY/vwap_macd_strategy.py");

	[TestMethod]
	public Task IchimokuAdxStrategy()
		=> RunStrategy("0200_Ichimoku_ADX/PY/ichimoku_adx_strategy.py");

	[TestMethod]
	public Task VwapWilliamsRStrategy()
		=> RunStrategy("0201_VWAP_Williams_R/PY/vwap_williams_r_strategy.py");

	[TestMethod]
	public Task DonchianCciStrategy()
		=> RunStrategy("0202_Donchian_CCI/PY/donchian_cci_strategy.py");

	[TestMethod]
	public Task KeltnerWilliamsRStrategy()
		=> RunStrategy("0203_Keltner_Williams_R/PY/keltner_williams_r_strategy.py");

	[TestMethod]
	public Task ParabolicSarCciStrategy()
		=> RunStrategy("0204_Parabolic_SAR_CCI/PY/parabolic_sar_cci_strategy.py");

	[TestMethod]
	public Task HullMaCciStrategy()
		=> RunStrategy("0205_Hull_MA_CCI/PY/hull_ma_cci_strategy.py");

	[TestMethod]
	public Task MacdBollingerStrategy()
		=> RunStrategy("0206_MACD_Bollinger/PY/macd_bollinger_strategy.py");

	[TestMethod]
	public Task RsiHullMaStrategy()
		=> RunStrategy("0207_RSI_Hull_MA/PY/rsi_hull_ma_strategy.py");

	[TestMethod]
	public Task StochasticKeltnerStrategy()
		=> RunStrategy("0208_Stochastic_Keltner/PY/stochastic_keltner_strategy.py");

	[TestMethod]
	public Task VolumeSupertrendStrategy()
		=> RunStrategy("0209_Volume_Supertrend/PY/volume_supertrend_strategy.py");

	[TestMethod]
	public Task AdxDonchianStrategy()
		=> RunStrategy("0210_ADX_Donchian/PY/adx_donchian_strategy.py");

	[TestMethod]
	public Task CciVwapStrategy()
		=> RunStrategy("0211_CCI_VWAP/PY/cci_vwap_strategy.py");

	[TestMethod]
	public Task WilliamsIchimokuStrategy()
		=> RunStrategy("0212_Williams_R_Ichimoku/PY/williams_ichimoku_strategy.py");

	[TestMethod]
	public Task MaParabolicSarStrategy()
		=> RunStrategy("0213_MA_Parabolic_SAR/PY/ma_parabolic_sar_strategy.py");

	[TestMethod]
	public Task BollingerSupertrendStrategy()
		=> RunStrategy("0214_Bollinger_Supertrend/PY/bollinger_supertrend_strategy.py");

	[TestMethod]
	public Task RsiDonchianStrategy()
		=> RunStrategy("0215_RSI_Donchian/PY/rsi_donchian_strategy.py");

	[TestMethod]
	public Task MeanReversionStrategy()
		=> RunStrategy("0216_Mean_Reversion/PY/mean_reversion_strategy.py");

	[TestMethod]
	public Task PairsTradingStrategy()
		=> RunStrategy("0217_Pairs_Trading/PY/pairs_trading_strategy.py", (stra, sec) =>
		{
			stra.Parameters["SecondSecurity"].Value = sec;
		});

	[TestMethod]
	public Task ZScoreReversalStrategy()
		=> RunStrategy("0218_ZScore_Reversal/PY/z_score_reversal_strategy.py");

	[TestMethod]
	public Task StatisticalArbitrageStrategy()
		=> RunStrategy("0219_Statistical_Arbitrage/PY/statistical_arbitrage_strategy.py", (stra, sec) =>
		{
			stra.Parameters["SecondSecurity"].Value = sec;
		});

	[TestMethod]
	public Task VolatilityBreakoutStrategy()
		=> RunStrategy("0220_Volatility_Breakout/PY/volatility_breakout_strategy.py");

	[TestMethod]
	public Task BollingerBandSqueezeStrategy()
		=> RunStrategy("0221_Bollinger_Band_Squeeze/PY/bollinger_band_squeeze_strategy.py");

	[TestMethod]
	public Task CointegrationPairsStrategy()
		=> RunStrategy("0222_Cointegration_Pairs/PY/cointegration_pairs_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Asset2"].Value = sec;
		});

	[TestMethod]
	public Task MomentumDivergenceStrategy()
		=> RunStrategy("0223_Momentum_Divergence/PY/momentum_divergence_strategy.py");

	[TestMethod]
	public Task AtrMeanReversionStrategy()
		=> RunStrategy("0224_ATR_Mean_Reversion/PY/atr_mean_reversion_strategy.py");

	[TestMethod]
	public Task KalmanFilterTrendStrategy()
		=> RunStrategy("0225_Kalman_Filter_Trend/PY/kalman_filter_trend_strategy.py");

	[TestMethod]
	public Task VolatilityAdjustedMeanReversionStrategy()
		=> RunStrategy("0226_Volatility_Adjusted_Mean_Reversion/PY/volatility_adjusted_mean_reversion_strategy.py");

	[TestMethod]
	public Task HurstExponentTrendStrategy()
		=> RunStrategy("0227_Hurst_Exponent_Trend/PY/hurst_exponent_trend_strategy.py");

	[TestMethod]
	public Task HurstExponentReversionStrategy()
		=> RunStrategy("0228_Hurst_Exponent_Reversion/PY/hurst_exponent_reversion_strategy.py");

	[TestMethod]
	public Task AutocorrelationReversionStrategy()
		=> RunStrategy("0229_Autocorrelation_Reversal/PY/autocorrelation_reversion_strategy.py");

	[TestMethod]
	public Task DeltaNeutralArbitrageStrategy()
		=> RunStrategy("0230_Delta_Neutral_Arbitrage/PY/delta_neutral_arbitrage_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Asset2Security"].Value = sec;
			stra.Parameters["Asset2Portfolio"].Value = stra.Portfolio;
		});

	[TestMethod]
	public Task VolatilitySkewArbitrageStrategy()
		=> RunStrategy("0231_Volatility_Skew_Arbitrage/PY/volatility_skew_arbitrage_strategy.py");

	[TestMethod]
	public Task CorrelationBreakoutStrategy()
		=> RunStrategy("0232_Correlation_Breakout/PY/correlation_breakout_strategy.py");

	[TestMethod]
	public Task BetaNeutralArbitrageStrategy()
		=> RunStrategy("0233_Beta_Neutral_Arbitrage/PY/beta_neutral_arbitrage_strategy.py");

	[TestMethod]
	public Task VwapMeanReversionStrategy()
		=> RunStrategy("0235_VWAP_Mean_Reversion/PY/vwap_mean_reversion_strategy.py");

	[TestMethod]
	public Task RsiMeanReversionStrategy()
		=> RunStrategy("0236_RSI_Mean_Reversion/PY/rsi_mean_reversion_strategy.py");

	[TestMethod]
	public Task StochasticMeanReversionStrategy()
		=> RunStrategy("0237_Stochastic_Mean_Reversion/PY/stochastic_mean_reversion_strategy.py");

	[TestMethod]
	public Task CciMeanReversionStrategy()
		=> RunStrategy("0238_CCI_Mean_Reversion/PY/cci_mean_reversion_strategy.py");

	[TestMethod]
	public Task WilliamsRMeanReversionStrategy()
		=> RunStrategy("0239_Williams_R_Mean_Reversion/PY/williams_r_mean_reversion_strategy.py");

	[TestMethod]
	public Task MacdMeanReversionStrategy()
		=> RunStrategy("0240_MACD_Mean_Reversion/PY/macd_mean_reversion_strategy.py");

	[TestMethod]
	public Task AdxMeanReversionStrategy()
		=> RunStrategy("0241_ADX_Mean_Reversion/PY/adx_mean_reversion_strategy.py");

	[TestMethod]
	public Task VolatilityMeanReversionStrategy()
		=> RunStrategy("0242_Volatility_Mean_Reversion/PY/volatility_mean_reversion_strategy.py");

	[TestMethod]
	public Task VolumeMeanReversionStrategy()
		=> RunStrategy("0243_Volume_Mean_Reversion/PY/volume_mean_reversion_strategy.py");

	[TestMethod]
	public Task ObvMeanReversionStrategy()
		=> RunStrategy("0244_OBV_Mean_Reversion/PY/obv_mean_reversion_strategy.py");

	[TestMethod]
	public Task MomentumBreakoutStrategy()
		=> RunStrategy("0245_Momentum_Breakout/PY/momentum_breakout_strategy.py");

	[TestMethod]
	public Task RsiBreakoutStrategy()
		=> RunStrategy("0247_RSI_Breakout/PY/rsi_breakout_strategy.py");

	[TestMethod]
	public Task StochasticBreakoutStrategy()
		=> RunStrategy("0248_Stochastic_Breakout/PY/stochastic_breakout_strategy.py");

	[TestMethod]
	public Task WilliamsRBreakoutStrategy()
		=> RunStrategy("0250_Williams_R_Breakout/PY/williams_r_breakout_strategy.py");

	[TestMethod]
	public Task MacdBreakoutStrategy()
		=> RunStrategy("0251_MACD_Breakout/PY/macd_breakout_strategy.py");

	[TestMethod]
	public Task AdxBreakoutStrategy()
		=> RunStrategy("0252_ADX_Breakout/PY/adx_breakout_strategy.py");

	[TestMethod]
	public Task VolumeBreakoutStrategy()
		=> RunStrategy("0254_Volume_Breakout/PY/volume_breakout_strategy.py");

	[TestMethod]
	public Task BollingerBandWidthBreakoutStrategy()
		=> RunStrategy("0256_Bollinger_Band_Width_Breakout/PY/bollinger_band_width_breakout_strategy.py");

	[TestMethod]
	public Task KeltnerWidthBreakoutStrategy()
		=> RunStrategy("0257_Keltner_Channel_Width_Breakout/PY/keltner_width_breakout_strategy.py");

	[TestMethod]
	public Task DonchianWidthBreakoutStrategy()
		=> RunStrategy("0258_Donchian_Channel_Width_Breakout/PY/donchian_width_breakout_strategy.py");

	[TestMethod]
	public Task IchimokuWidthBreakoutStrategy()
		=> RunStrategy("0259_Ichimoku_Cloud_Width_Breakout/PY/ichimoku_width_breakout_strategy.py");

	[TestMethod]
	public Task SupertrendDistanceBreakoutStrategy()
		=> RunStrategy("0260_Supertrend_Distance_Breakout/PY/supertrend_distance_breakout_strategy.py");

	[TestMethod]
	public Task ParabolicSarDistanceBreakoutStrategy()
		=> RunStrategy("0261_Parabolic_SAR_Distance_Breakout/PY/parabolic_sar_distance_breakout_strategy.py");

	[TestMethod]
	public Task HullMaSlopeBreakoutStrategy()
		=> RunStrategy("0262_Hull_MA_Slope_Breakout/PY/hull_ma_slope_breakout_strategy.py");

	[TestMethod]
	public Task MaSlopeBreakoutStrategy()
		=> RunStrategy("0263_MA_Slope_Breakout/PY/ma_slope_breakout_strategy.py");

	[TestMethod]
	public Task EmaSlopeBreakoutStrategy()
		=> RunStrategy("0264_EMA_Slope_Breakout/PY/ema_slope_breakout_strategy.py");

	[TestMethod]
	public Task VolatilityAdjustedMomentumStrategy()
		=> RunStrategy("0265_Volatility_Adjusted_Momentum/PY/volatility_adjusted_momentum_strategy.py");

	[TestMethod]
	public Task VwapSlopeBreakoutStrategy()
		=> RunStrategy("0266_VWAP_Slope_Breakout/PY/vwap_slope_breakout_strategy.py");

	[TestMethod]
	public Task RsiSlopeBreakoutStrategy()
		=> RunStrategy("0267_RSI_Slope_Breakout/PY/rsi_slope_breakout_strategy.py");

	[TestMethod]
	public Task StochasticSlopeBreakoutStrategy()
		=> RunStrategy("0268_Stochastic_Slope_Breakout/PY/stochastic_slope_breakout_strategy.py");

	[TestMethod]
	public Task CciSlopeBreakoutStrategy()
		=> RunStrategy("0269_CCI_Slope_Breakout/PY/cci_slope_breakout_strategy.py");

	[TestMethod]
	public Task WilliamsRSlopeBreakoutStrategy()
		=> RunStrategy("0270_Williams_R_Slope_Breakout/PY/williams_r_slope_breakout_strategy.py");

	[TestMethod]
	public Task MacdSlopeBreakoutStrategy()
		=> RunStrategy("0271_MACD_Slope_Breakout/PY/macd_slope_breakout_strategy.py");

	[TestMethod]
	public Task AdxSlopeBreakoutStrategy()
		=> RunStrategy("0272_ADX_Slope_Breakout/PY/adx_slope_breakout_strategy.py");

	[TestMethod]
	public Task AtrSlopeBreakoutStrategy()
		=> RunStrategy("0273_ATR_Slope_Breakout/PY/atr_slope_breakout_strategy.py");

	[TestMethod]
	public Task VolumeSlopeBreakoutStrategy()
		=> RunStrategy("0274_Volume_Slope_Breakout/PY/volume_slope_breakout_strategy.py");

	[TestMethod]
	public Task ObvSlopeBreakoutStrategy()
		=> RunStrategy("0275_OBV_Slope_Breakout/PY/obv_slope_breakout_strategy.py");

	[TestMethod]
	public Task BollingerWidthMeanReversionStrategy()
		=> RunStrategy("0276_Bollinger_Width_Mean_Reversion/PY/bollinger_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task KeltnerWidthMeanReversionStrategy()
		=> RunStrategy("0277_Keltner_Width_Mean_Reversion/PY/keltner_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task DonchianWidthMeanReversionStrategy()
		=> RunStrategy("0278_Donchian_Width_Mean_Reversion/PY/donchian_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task IchimokuCloudWidthMeanReversionStrategy()
		=> RunStrategy("0279_Ichimoku_Cloud_Width_Mean_Reversion/PY/ichimoku_cloud_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task SupertrendDistanceMeanReversionStrategy()
		=> RunStrategy("0280_Supertrend_Distance_Mean_Reversion/PY/supertrend_distance_mean_reversion_strategy.py");

	[TestMethod]
	public Task ParabolicSarDistanceMeanReversionStrategy()
		=> RunStrategy("0281_Parabolic_SAR_Distance_Mean_Reversion/PY/parabolic_sar_distance_mean_reversion_strategy.py");

	[TestMethod]
	public Task HullMaSlopeMeanReversionStrategy()
		=> RunStrategy("0282_Hull_MA_Slope_Mean_Reversion/PY/hull_ma_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task MaSlopeMeanReversionStrategy()
		=> RunStrategy("0283_MA_Slope_Mean_Reversion/PY/ma_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task EmaSlopeMeanReversionStrategy()
		=> RunStrategy("0284_EMA_Slope_Mean_Reversion/PY/ema_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task VwapSlopeMeanReversionStrategy()
		=> RunStrategy("0285_VWAP_Slope_Mean_Reversion/PY/vwap_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task RsiSlopeMeanReversionStrategy()
		=> RunStrategy("0286_RSI_Slope_Mean_Reversion/PY/rsi_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task StochasticSlopeMeanReversionStrategy()
		=> RunStrategy("0287_Stochastic_Slope_Mean_Reversion/PY/stochastic_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task CciSlopeMeanReversionStrategy()
		=> RunStrategy("0288_CCI_Slope_Mean_Reversion/PY/cci_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task WilliamsRSlopeMeanReversionStrategy()
		=> RunStrategy("0289_Williams_R_Slope_Mean_Reversion/PY/williams_r_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task MacdSlopeMeanReversionStrategy()
		=> RunStrategy("0290_MACD_Slope_Mean_Reversion/PY/macd_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task AdxSlopeMeanReversionStrategy()
		=> RunStrategy("0291_ADX_Slope_Mean_Reversion/PY/adx_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task AtrSlopeMeanReversionStrategy()
		=> RunStrategy("0292_ATR_Slope_Mean_Reversion/PY/atr_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task VolumeSlopeMeanReversionStrategy()
		=> RunStrategy("0293_Volume_Slope_Mean_Reversion/PY/volume_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task ObvSlopeMeanReversionStrategy()
		=> RunStrategy("0294_OBV_Slope_Mean_Reversion/PY/obv_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task PairsTradingVolatilityFilterStrategy()
		=> RunStrategy("0295_Pairs_Trading_Volatility_Filter/PY/pairs_trading_volatility_filter_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Security2"].Value = sec;
		});

	[TestMethod]
	public Task ZscoreVolumeFilterStrategy()
		=> RunStrategy("0296_Z-Score_Volume_Filter/PY/zscore_volume_filter_strategy.py");

	[TestMethod]
	public Task CorrelationMeanReversionStrategy()
		=> RunStrategy("0298_Correlation_Mean_Reversion/PY/correlation_mean_reversion_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Security2"].Value = sec;
		});

	[TestMethod]
	public Task BetaAdjustedPairsStrategy()
		=> RunStrategy("0299_Beta_Adjusted_Pairs_Trading/PY/beta_adjusted_pairs_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Asset2"].Value = sec;
			stra.Parameters["Asset2Portfolio"].Value = stra.Portfolio;
		});

	[TestMethod]
	public Task HurstVolatilityFilterStrategy()
		=> RunStrategy("0300_Hurst_Exponent_Volatility_Filter/PY/hurst_volatility_filter_strategy.py");

	[TestMethod]
	public Task AdaptiveEmaBreakoutStrategy()
		=> RunStrategy("0301_Adaptive_EMA_Breakout/PY/adaptive_ema_breakout_strategy.py");

	[TestMethod]
	public Task VolatilityClusterBreakoutStrategy()
		=> RunStrategy("0302_Volatility_Cluster_Breakout/PY/volatility_cluster_breakout_strategy.py");

	[TestMethod]
	public Task SeasonalityAdjustedMomentumStrategy()
		=> RunStrategy("0303_Seasonality_Adjusted_Momentum/PY/seasonality_adjusted_momentum_strategy.py");

	[TestMethod]
	public Task RsiDynamicOverboughtOversoldStrategy()
		=> RunStrategy("0305_RSI_Dynamic_Overbought_Oversold/PY/rsi_dynamic_overbought_oversold_strategy.py");

	[TestMethod]
	public Task BollingerVolatilityBreakoutStrategy()
		=> RunStrategy("0306_Bollinger_Volatility_Breakout/PY/bollinger_volatility_breakout_strategy.py");

	[TestMethod]
	public Task MacdAdaptiveHistogramStrategy()
		=> RunStrategy("0307_MACD_Adaptive_Histogram/PY/macd_adaptive_histogram_strategy.py");

	[TestMethod]
	public Task IchimokuVolumeClusterStrategy()
		=> RunStrategy("0308_Ichimoku_Volume_Cluster/PY/ichimoku_volume_cluster_strategy.py");

	[TestMethod]
	public Task SupertrendMomentumFilterStrategy()
		=> RunStrategy("0309_Supertrend_Momentum_Filter/PY/supertrend_momentum_filter_strategy.py");

	[TestMethod]
	public Task DonchianVolatilityContractionStrategy()
		=> RunStrategy("0310_Donchian_Volatility_Contraction/PY/donchian_volatility_contraction_strategy.py");

	[TestMethod]
	public Task KeltnerRsiDivergenceStrategy()
		=> RunStrategy("0311_Keltner_RSI_Divergence/PY/keltner_rsi_divergence_strategy.py");

	[TestMethod]
	public Task HullMaVolumeSpikeStrategy()
		=> RunStrategy("0312_Hull_MA_Volume_Spike/PY/hull_ma_volume_spike_strategy.py");

	[TestMethod]
	public Task VwapAdxTrendStrengthStrategy()
		=> RunStrategy("0313_VWAP_ADX_Trend_Strength/PY/vwap_adx_trend_strength_strategy.py");

	[TestMethod]
	public Task ParabolicSarVolatilityExpansionStrategy()
		=> RunStrategy("0314_Parabolic_SAR_Volatility_Expansion/PY/parabolic_sar_volatility_expansion_strategy.py");

	[TestMethod]
	public Task StochasticWithDynamicZonesStrategy()
		=> RunStrategy("0315_Stochastic_Dynamic_Zones/PY/stochastic_with_dynamic_zones_strategy.py");

	[TestMethod]
	public Task AdxWithVolumeBreakoutStrategy()
		=> RunStrategy("0316_ADX_Volume_Breakout/PY/adx_with_volume_breakout_strategy.py");

	[TestMethod]
	public Task CciWithVolatilityFilterStrategy()
		=> RunStrategy("0317_CCI_Volatility_Filter/PY/cci_with_volatility_filter_strategy.py");

	[TestMethod]
	public Task WilliamsPercentRWithMomentumStrategy()
		=> RunStrategy("0318_Williams_R_Momentum/PY/williams_percent_r_with_momentum_strategy.py");

	[TestMethod]
	public Task BollingerKmeansStrategy()
		=> RunStrategy("0319_Bollinger_K-Means_Cluster/PY/bollinger_kmeans_strategy.py");

	[TestMethod]
	public Task MacdHiddenMarkovModelStrategy()
		=> RunStrategy("0320_MACD_Hidden_Markov_Model/PY/macd_hidden_markov_model_strategy.py");

	[TestMethod]
	public Task IchimokuHurstExponentStrategy()
		=> RunStrategy("0321_Ichimoku_Hurst_Exponent/PY/ichimoku_hurst_exponent_strategy.py");

	[TestMethod]
	public Task SupertrendRsiDivergenceStrategy()
		=> RunStrategy("0322_Supertrend_RSI_Divergence/PY/supertrend_rsi_divergence_strategy.py");

	[TestMethod]
	public Task DonchianSeasonalFilterStrategy()
		=> RunStrategy("0323_Donchian_Seasonal_Filter/PY/donchian_seasonal_filter_strategy.py");

	[TestMethod]
	public Task KeltnerKalmanStrategy()
		=> RunStrategy("0324_Keltner_Kalman_Filter/PY/keltner_kalman_strategy.py");

	[TestMethod]
	public Task HullMaVolatilityContractionStrategy()
		=> RunStrategy("0325_Hull_MA_Volatility_Contraction/PY/hull_ma_volatility_contraction_strategy.py");

	[TestMethod]
	public Task VwapAdxTrendStrategy()
		=> RunStrategy("0326_VWAP_Stochastic_Divergence/PY/vwap_adx_trend_strategy.py");

	[TestMethod]
	public Task ParabolicSarHurstStrategy()
		=> RunStrategy("0327_Parabolic_SAR_Hurst_Filter/PY/parabolic_sar_hurst_strategy.py");

	[TestMethod]
	public Task BollingerKalmanFilterStrategy()
		=> RunStrategy("0328_Bollinger_Kalman_Filter/PY/bollinger_kalman_filter_strategy.py");

	[TestMethod]
	public Task MacdVolumeClusterStrategy()
		=> RunStrategy("0329_MACD_Volume_Cluster/PY/macd_volume_cluster_strategy.py");

	[TestMethod]
	public Task IchimokuVolatilityContractionStrategy()
		=> RunStrategy("0330_Ichimoku_Volatility_Contraction/PY/ichimoku_volatility_contraction_strategy.py");

	[TestMethod]
	public Task DonchianHurstStrategy()
		=> RunStrategy("0332_Donchian_Hurst_Exponent/PY/donchian_hurst_strategy.py");

	[TestMethod]
	public Task KeltnerSeasonalStrategy()
		=> RunStrategy("0333_Keltner_Seasonal_Filter/PY/keltner_seasonal_strategy.py");

	[TestMethod]
	public Task HullKmeansClusterStrategy()
		=> RunStrategy("0334_Hull_MA_K-Means_Cluster/PY/hull_kmeans_cluster_strategy.py");

	[TestMethod]
	public Task VwapHiddenMarkovModelStrategy()
		=> RunStrategy("0335_VWAP_Hidden_Markov_Model/PY/vwap_hidden_markov_model_strategy.py");

	[TestMethod]
	public Task ParabolicSarRsiDivergenceStrategy()
		=> RunStrategy("0336_Parabolic_SAR_RSI_Divergence/PY/parabolic_sar_rsi_divergence_strategy.py");

	[TestMethod]
	public Task AdaptiveRsiVolumeStrategy()
		=> RunStrategy("0337_Adaptive_RSI_Volume_Filter/PY/adaptive_rsi_volume_strategy.py");

	[TestMethod]
	public Task AdaptiveBollingerBreakoutStrategy()
		=> RunStrategy("0338_Adaptive_Bollinger_Breakout/PY/adaptive_bollinger_breakout_strategy.py");

	[TestMethod]
	public Task MacdWithSentimentFilterStrategy()
		=> RunStrategy("0339_MACD_Sentiment_Filter/PY/macd_with_sentiment_filter_strategy.py");

	[TestMethod]
	public Task IchimokuImpliedVolatilityStrategy()
		=> RunStrategy("0340_Ichimoku_Implied_Volatility/PY/ichimoku_implied_volatility_strategy.py");

	[TestMethod]
	public Task SupertrendPutCallRatioStrategy()
		=> RunStrategy("0341_Supertrend_Put_Call_Ratio/PY/supertrend_put_call_ratio_strategy.py");

	[TestMethod]
	public Task DonchianWithSentimentSpikeStrategy()
		=> RunStrategy("0342_Donchian_Sentiment_Spike/PY/donchian_with_sentiment_spike_strategy.py");

	[TestMethod]
	public Task KeltnerWithRlSignalStrategy()
		=> RunStrategy("0343_Keltner_Reinforcement_Learning_Signal/PY/keltner_with_rl_signal_strategy.py");

	[TestMethod]
	public Task HullMaImpliedVolatilityBreakoutStrategy()
		=> RunStrategy("0344_Hull_MA_Implied_Volatility_Breakout/PY/hull_ma_implied_volatility_breakout_strategy.py");

	[TestMethod]
	public Task VwapWithBehavioralBiasFilterStrategy()
		=> RunStrategy("0345_VWAP_Behavioral_Bias_Filter/PY/vwap_with_behavioral_bias_filter_strategy.py");

	[TestMethod]
	public Task ParabolicSarSentimentDivergenceStrategy()
		=> RunStrategy("0346_Parabolic_SAR_Sentiment_Divergence/PY/parabolic_sar_sentiment_divergence_strategy.py");

	[TestMethod]
	public Task RsiWithOptionOpenInterestStrategy()
		=> RunStrategy("0347_RSI_Option_Open_Interest/PY/rsi_with_option_open_interest_strategy.py");

	[TestMethod]
	public Task StochasticImpliedVolatilitySkewStrategy()
		=> RunStrategy("0348_Stochastic_Implied_Volatility_Skew/PY/stochastic_implied_volatility_skew_strategy.py");

	[TestMethod]
	public Task AdxSentimentMomentumStrategy()
		=> RunStrategy("0349_ADX_Sentiment_Momentum/PY/adx_sentiment_momentum_strategy.py");

	[TestMethod]
	public Task CciPutCallRatioDivergenceStrategy()
		=> RunStrategy("0350_CCI_Put_Call_Ratio_Divergence/PY/cci_put_call_ratio_divergence_strategy.py");
}