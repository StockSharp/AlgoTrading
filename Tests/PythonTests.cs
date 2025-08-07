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
	public Task MaCrossover()
		=> RunStrategy("0001_MA_CrossOver/PY/ma_crossover_strategy.py");

	[TestMethod]
	public Task NdayBreakout()
		=> RunStrategy("0002_NDay_Breakout/PY/nday_breakout_strategy.py");

	[TestMethod]
	public Task AdxTrend()
		=> RunStrategy("0003_ADX_Trend/PY/adx_trend_strategy.py");

	[TestMethod]
	public Task ParabolicSarTrend()
		=> RunStrategy("0004_Parabolic_SAR_Trend/PY/parabolic_sar_trend_strategy.py");

	[TestMethod]
	public Task DonchianChannel()
		=> RunStrategy("0005_Donchian_Channel/PY/donchian_channel_strategy.py");

	[TestMethod]
	public Task TripleMa()
		=> RunStrategy("0006_Tripple_MA/PY/triple_ma_strategy.py");

	[TestMethod]
	public Task KeltnerChannelBreakout()
		=> RunStrategy("0007_Keltner_Channel_Breakout/PY/keltner_channel_breakout_strategy.py");

	[TestMethod]
	public Task HullMaTrend()
		=> RunStrategy("0008_Hull_MA_Trend/PY/hull_ma_trend_strategy.py");

	[TestMethod]
	public Task MacdTrend()
		=> RunStrategy("0009_MACD_Trend/PY/macd_trend_strategy.py");

	[TestMethod]
	public Task Supertrend()
		=> RunStrategy("0010_Super_Trend/PY/supertrend_strategy.py");

	[TestMethod]
	public Task IchimokuKumoBreakout()
		=> RunStrategy("0011_Ichimoku_Kumo_Breakout/PY/ichimoku_kumo_breakout_strategy.py");

	[TestMethod]
	public Task HeikinAshiConsecutive()
		=> RunStrategy("0012_Heikin_Ashi_Consecutive/PY/heikin_ashi_consecutive_strategy.py");

	[TestMethod]
	public Task DmiPowerMove()
		=> RunStrategy("0013_DMI_Power_Move/PY/dmi_power_move_strategy.py");

	[TestMethod]
	public Task TradingviewSupertrendFlip()
		=> RunStrategy("0014_TradingView_Supertrend_Flip/PY/tradingview_supertrend_flip_strategy.py");

	[TestMethod]
	public Task GannSwingBreakout()
		=> RunStrategy("0015_Gann_Swing_Breakout/PY/gann_swing_breakout_strategy.py");

	[TestMethod]
	public Task RsiDivergence()
		=> RunStrategy("0016_RSI_Divergence/PY/rsi_divergence_strategy.py");

	[TestMethod]
	public Task WilliamsPercentR()
		=> RunStrategy("0017_Williams_R/PY/williams_percent_r_strategy.py");

	[TestMethod]
	public Task RocImpulse()
		=> RunStrategy("0018_ROC_Impulce/PY/roc_impulse_strategy.py");

	[TestMethod]
	public Task CciBreakout()
		=> RunStrategy("0019_CCI_Breakout/PY/cci_breakout_strategy.py");

	[TestMethod]
	public Task MomentumPercentage()
		=> RunStrategy("0020_Momentum_Percentage/PY/momentum_percentage_strategy.py");

	[TestMethod]
	public Task BollingerSqueeze()
		=> RunStrategy("0021_Bollinger_Squeeze/PY/bollinger_squeeze_strategy.py");

	[TestMethod]
	public Task AdxDi()
		=> RunStrategy("0022_ADX_DI/PY/adx_di_strategy.py");

	[TestMethod]
	public Task ElderImpulse()
		=> RunStrategy("0023_Elder_Impulse/PY/elder_impulse_strategy.py");

	[TestMethod]
	public Task LaguerreRsi()
		=> RunStrategy("0024_RSI_Laguerre/PY/laguerre_rsi_strategy.py");

	[TestMethod]
	public Task StochasticRsiCross()
		=> RunStrategy("0025_Stochastic_RSI_Cross/PY/stochastic_rsi_cross_strategy.py");

	[TestMethod]
	public Task RsiReversion()
		=> RunStrategy("0026_RSI_Reversion/PY/rsi_reversion_strategy.py");

	[TestMethod]
	public Task BollingerReversion()
		=> RunStrategy("0027_Bollinger_Reversion/PY/bollinger_reversion_strategy.py");

	[TestMethod]
	public Task ZScore()
		=> RunStrategy("0028_ZScore/PY/z_score_strategy.py");

	[TestMethod]
	public Task MaDeviation()
		=> RunStrategy("0029_MA_Deviation/PY/ma_deviation_strategy.py");

	[TestMethod]
	public Task VwapReversion()
		=> RunStrategy("0030_VWAP_Reversion/PY/vwap_reversion_strategy.py");

	[TestMethod]
	public Task KeltnerReversion()
		=> RunStrategy("0031_Keltner_Reversion/PY/keltner_reversion_strategy.py");

	[TestMethod]
	public Task AtrReversion()
		=> RunStrategy("0032_ATR_Reversion/PY/atr_reversion_strategy.py");

	[TestMethod]
	public Task MacdZero()
		=> RunStrategy("0033_MACD_Zero/PY/macd_zero_strategy.py");

	[TestMethod]
	public Task LowVolReversion()
		=> RunStrategy("0034_Low_Vol_Reversion/PY/low_vol_reversion_strategy.py");

	[TestMethod]
	public Task BollingerPercentB()
		=> RunStrategy("0035_Bollinger_B_Reversion/PY/bollinger_percent_b_strategy.py");

	[TestMethod]
	public Task AtrExpansion()
		=> RunStrategy("0036_ATR_Expansion/PY/atr_expansion_strategy.py");

	[TestMethod]
	public Task VixTrigger()
		=> RunStrategy("0037_VIX_Trigger/PY/vix_trigger_strategy.py");

	[TestMethod]
	public Task BollingerBandWidth()
		=> RunStrategy("0038_BB_Width/PY/bollinger_band_width_strategy.py");

	[TestMethod]
	public Task HvBreakout()
		=> RunStrategy("0039_HV_Breakout/PY/hv_breakout_strategy.py");

	[TestMethod]
	public Task AtrTrailing()
		=> RunStrategy("0040_ATR_Trailing/PY/atr_trailing_strategy.py");

	[TestMethod]
	public Task VolAdjustedMa()
		=> RunStrategy("0041_Vol_Adjusted_MA/PY/vol_adjusted_ma_strategy.py");

	[TestMethod]
	public Task IvSpike()
		=> RunStrategy("0042_IV_Spike/PY/iv_spike_strategy.py");

	[TestMethod]
	public Task Vcp()
		=> RunStrategy("0043_VCP/PY/vcp_strategy.py");

	[TestMethod]
	public Task AtrRange()
		=> RunStrategy("0044_ATR_Range/PY/atr_range_strategy.py");

	[TestMethod]
	public Task ChoppinessIndexBreakout()
		=> RunStrategy("0045_Choppiness_Index_Breakout/PY/choppiness_index_breakout_strategy.py");

	[TestMethod]
	public Task VolumeSpike()
		=> RunStrategy("0046_Volume_Spike/PY/volume_spike_strategy.py");

	[TestMethod]
	public Task ObvBreakout()
		=> RunStrategy("0047_OBV_Breakout/PY/obv_breakout_strategy.py");

	[TestMethod]
	public Task VwapBreakout()
		=> RunStrategy("0048_VWAP_Breakout/PY/vwap_breakout_strategy.py");

	[TestMethod]
	public Task Vwma()
		=> RunStrategy("0049_VWMA/PY/vwma_strategy.py");

	[TestMethod]
	public Task Ad()
		=> RunStrategy("0050_AD/PY/ad_strategy.py");

	[TestMethod]
	public Task VolumeWeightedPriceBreakout()
		=> RunStrategy("0051_Volume_Weighted_Price_Breakout/PY/volume_weighted_price_breakout_strategy.py");

	[TestMethod]
	public Task VolumeDivergence()
		=> RunStrategy("0052_Volume_Divergence/PY/volume_divergence_strategy.py");

	[TestMethod]
	public Task VolumeMaCross()
		=> RunStrategy("0053_Volume_MA_Cross/PY/volume_ma_cross_strategy.py");

	[TestMethod]
	public Task CumulativeDeltaBreakout()
		=> RunStrategy("0054_Cumulative_Delta_Breakout/PY/cumulative_delta_breakout_strategy.py");

	[TestMethod]
	public Task VolumeSurge()
		=> RunStrategy("0055_Volume_Surge/PY/volume_surge_strategy.py");

	[TestMethod]
	public Task DoubleBottom()
		=> RunStrategy("0056_Double_Bottom/PY/double_bottom_strategy.py");

	[TestMethod]
	public Task DoubleTop()
		=> RunStrategy("0057_Double_Top/PY/double_top_strategy.py");

	[TestMethod]
	public Task RsiOverboughtOversold()
		=> RunStrategy("0058_RSI_Overbought_Oversold/PY/rsi_overbought_oversold_strategy.py");

	[TestMethod]
	public Task HammerCandle()
		=> RunStrategy("0059_Hammer_Candle/PY/hammer_candle_strategy.py");

	[TestMethod]
	public Task ShootingStar()
		=> RunStrategy("0060_Shooting_Star/PY/shooting_star_strategy.py");

	[TestMethod]
	public Task MacdDivergence()
		=> RunStrategy("0061_MACD_Divergence/PY/macd_divergence_strategy.py");

	[TestMethod]
	public Task StochasticOverboughtOversold()
		=> RunStrategy("0062_Stochastic_Overbought_Oversold/PY/stochastic_overbought_oversold_strategy.py");

	[TestMethod]
	public Task EngulfingBullish()
		=> RunStrategy("0063_Engulfing_Bullish/PY/engulfing_bullish_strategy.py");

	[TestMethod]
	public Task EngulfingBearish()
		=> RunStrategy("0064_Engulfing_Bearish/PY/engulfing_bearish_strategy.py");

	[TestMethod]
	public Task PinbarReversal()
		=> RunStrategy("0065_Pinbar_Reversal/PY/pinbar_reversal_strategy.py");

	[TestMethod]
	public Task ThreeBarReversalUp()
		=> RunStrategy("0066_Three_Bar_Reversal_Up/PY/three_bar_reversal_up_strategy.py");

	[TestMethod]
	public Task ThreeBarReversalDown()
		=> RunStrategy("0067_Three_Bar_Reversal_Down/PY/three_bar_reversal_down_strategy.py");

	[TestMethod]
	public Task CciDivergence()
		=> RunStrategy("0068_CCI_Divergence/PY/cci_divergence_strategy.py");

	[TestMethod]
	public Task BollingerBandReversal()
		=> RunStrategy("0069_Bollinger_Band_Reversal/PY/bollinger_band_reversal_strategy.py");

	[TestMethod]
	public Task MorningStar()
		=> RunStrategy("0070_Morning_Star/PY/morning_star_strategy.py");

	[TestMethod]
	public Task EveningStar()
		=> RunStrategy("0071_Evening_Star/PY/evening_star_strategy.py");

	[TestMethod]
	public Task DojiReversal()
		=> RunStrategy("0072_Doji_Reversal/PY/doji_reversal_strategy.py");

	[TestMethod]
	public Task KeltnerChannelReversal()
		=> RunStrategy("0073_Keltner_Channel_Reversal/PY/keltner_channel_reversal_strategy.py");

	[TestMethod]
	public Task WilliamsR()
		=> RunStrategy("0074_Williams_R_Divergence/PY/williams_percent_r_divergence_strategy.py");

	[TestMethod]
	public Task Obv()
		=> RunStrategy("0075_OBV_Divergence/PY/obv_divergence_strategy.py");

	[TestMethod]
	public Task Fibonacci()
		=> RunStrategy("0076_Fibonacci_Retracement_Reversal/PY/fibonacci_retracement_reversal_strategy.py");

	[TestMethod]
	public Task InsideBar()
		=> RunStrategy("0077_Inside_Bar_Breakout/PY/inside_bar_breakout_strategy.py");

	[TestMethod]
	public Task OutsideBar()
		=> RunStrategy("0078_Outside_Bar_Reversal/PY/outside_bar_reversal_strategy.py");

	[TestMethod]
	public Task Trendline()
		=> RunStrategy("0079_Trendline_Bounce/PY/trendline_bounce_strategy.py");

	[TestMethod]
	public Task PivotPoint()
		=> RunStrategy("0080_Pivot_Point_Reversal/PY/pivot_point_reversal_strategy.py");

	[TestMethod]
	public Task VwapBounce()
		=> RunStrategy("0081_VWAP_Bounce/PY/vwap_bounce_strategy.py");

	[TestMethod]
	public Task VolumeExhaustion()
		=> RunStrategy("0082_Volume_Exhaustion/PY/volume_exhaustion_strategy.py");

	[TestMethod]
	public Task AdxWeakening()
		=> RunStrategy("0083_ADX_Weakening/PY/adx_weakening_strategy.py");

	[TestMethod]
	public Task AtrExhaustion()
		=> RunStrategy("0084_ATR_Exhaustion/PY/atr_exhaustion_strategy.py");

	[TestMethod]
	public Task IchimokuTenkanKijun()
		=> RunStrategy("0085_Ichimoku_Tenkan/PY/ichimoku_tenkan_kijun_strategy.py");

	[TestMethod]
	public Task HeikinAshiReversal()
		=> RunStrategy("0086_Heikin_Ashi_Reversal/PY/heikin_ashi_reversal_strategy.py");

	[TestMethod]
	public Task ParabolicSarReversal()
		=> RunStrategy("0087_Parabolic_SAR_Reversal/PY/parabolic_sar_reversal_strategy.py");

	[TestMethod]
	public Task SupertrendReversal()
		=> RunStrategy("0088_Supertrend_Reversal/PY/supertrend_reversal_strategy.py");

	[TestMethod]
	public Task HullMaReversal()
		=> RunStrategy("0089_Hull_MA_Reversal/PY/hull_ma_reversal_strategy.py");

	[TestMethod]
	public Task DonchianReversal()
		=> RunStrategy("0090_Donchian_Reversal/PY/donchian_reversal_strategy.py");

	[TestMethod]
	public Task MacdHistogramReversal()
		=> RunStrategy("0091_MACD_Histogram_Reversal/PY/macd_histogram_reversal_strategy.py");

	[TestMethod]
	public Task RsiHookReversal()
		=> RunStrategy("0092_RSI_Hook_Reversal/PY/rsi_hook_reversal_strategy.py");

	[TestMethod]
	public Task StochasticHookReversal()
		=> RunStrategy("0093_Stochastic_Hook_Reversal/PY/stochastic_hook_reversal_strategy.py");

	[TestMethod]
	public Task CciHookReversal()
		=> RunStrategy("0094_CCI_Hook_Reversal/PY/cci_hook_reversal_strategy.py");

	[TestMethod]
	public Task WilliamsRHookReversal()
		=> RunStrategy("0095_Williams_R_Hook_Reversal/PY/williams_r_hook_reversal_strategy.py");

	[TestMethod]
	public Task ThreeWhiteSoldiers()
		=> RunStrategy("0096_Three_White_Soldiers/PY/three_white_soldiers_strategy.py");

	[TestMethod]
	public Task ThreeBlackCrows()
		=> RunStrategy("0097_Three_Black_Crows/PY/three_black_crows_strategy.py");

	[TestMethod]
	public Task GapFillReversal()
		=> RunStrategy("0098_Gap_Fill_Reversal/PY/gap_fill_reversal_strategy.py");

	[TestMethod]
	public Task TweezerBottom()
		=> RunStrategy("0099_Tweezer_Bottom/PY/tweezer_bottom_strategy.py");

	[TestMethod]
	public Task TweezerTop()
		=> RunStrategy("0100_Tweezer_Top/PY/tweezer_top_strategy.py");

	[TestMethod]
	public Task HaramiBullish()
		=> RunStrategy("0101_Harami_Bullish/PY/harami_bullish_strategy.py");

	[TestMethod]
	public Task HaramiBearish()
		=> RunStrategy("0102_Harami_Bearish/PY/harami_bearish_strategy.py");

	[TestMethod]
	public Task DarkPoolPrints()
		=> RunStrategy("0103_Dark_Pool_Prints/PY/dark_pool_prints_strategy.py");

	[TestMethod]
	public Task RejectionCandle()
		=> RunStrategy("0104_Rejection_Candle/PY/rejection_candle_strategy.py");

	[TestMethod]
	public Task FalseBreakoutTrap()
		=> RunStrategy("0105_False_Breakout_Trap/PY/false_breakout_trap_strategy.py");

	[TestMethod]
	public Task SpringReversal()
		=> RunStrategy("0106_Spring_Reversal/PY/spring_reversal_strategy.py");

	[TestMethod]
	public Task UpthrustReversal()
		=> RunStrategy("0107_Upthrust_Reversal/PY/upthrust_reversal_strategy.py");

	[TestMethod]
	public Task WyckoffAccumulation()
		=> RunStrategy("0108_Wyckoff_Accumulation/PY/wyckoff_accumulation_strategy.py");

	[TestMethod]
	public Task WyckoffDistribution()
		=> RunStrategy("0109_Wyckoff_Distribution/PY/wyckoff_distribution_strategy.py");

	[TestMethod]
	public Task RsiFailureSwing()
		=> RunStrategy("0110_RSI_Failure_Swing/PY/rsi_failure_swing_strategy.py");

	[TestMethod]
	public Task StochasticFailureSwing()
		=> RunStrategy("0111_Stochastic_Failure_Swing/PY/stochastic_failure_swing_strategy.py");

	[TestMethod]
	public Task CciFailureSwing()
		=> RunStrategy("0112_CCI_Failure_Swing/PY/cci_failure_swing_strategy.py");

	[TestMethod]
	public Task BullishAbandonedBaby()
		=> RunStrategy("0113_Bullish_Abandoned_Baby/PY/bullish_abandoned_baby_strategy.py");

	[TestMethod]
	public Task BearishAbandonedBaby()
		=> RunStrategy("0114_Bearish_Abandoned_Baby/PY/bearish_abandoned_baby_strategy.py");

	[TestMethod]
	public Task VolumeClimaxReversal()
		=> RunStrategy("0115_Volume_Climax_Reversal/PY/volume_climax_reversal_strategy.py");

	[TestMethod]
	public Task DayOfWeek()
		=> RunStrategy("0116_Day_of_Week/PY/day_of_week_strategy.py");

	[TestMethod]
	public Task MonthOfYear()
		=> RunStrategy("0117_Month_of_Year/PY/month_of_year_strategy.py");

	[TestMethod]
	public Task TurnaroundTuesday()
		=> RunStrategy("0118_Turnaround_Tuesday/PY/turnaround_tuesday_strategy.py");

	[TestMethod]
	public Task EndOfMonthStrength()
		=> RunStrategy("0119_End_of_Month_Strength/PY/end_of_month_strength_strategy.py");

	[TestMethod]
	public Task FirstDayOfMonth()
		=> RunStrategy("0120_First_Day_of_Month/PY/first_day_of_month_strategy.py");

	[TestMethod]
	public Task SantaClausRally()
		=> RunStrategy("0121_Santa_Claus_Rally/PY/santa_claus_rally_strategy.py");

	[TestMethod]
	public Task JanuaryEffect()
		=> RunStrategy("0122_January_Effect/PY/january_effect_strategy.py");

	[TestMethod]
	public Task MondayWeakness()
		=> RunStrategy("0123_Monday_Weakness/PY/monday_weakness_strategy.py");

	[TestMethod]
	public Task PreHolidayStrength()
		=> RunStrategy("0124_Pre-Holiday_Strength/PY/pre_holiday_strength_strategy.py");

	[TestMethod]
	public Task PostHolidayWeakness()
		=> RunStrategy("0125_Post-Holiday_Weakness/PY/post_holiday_weakness_strategy.py");

	[TestMethod]
	public Task QuarterlyExpiry()
		=> RunStrategy("0126_Quarterly_Expiry/PY/quarterly_expiry_strategy.py");

	[TestMethod]
	public Task OpenDrive()
		=> RunStrategy("0127_Open_Drive/PY/open_drive_strategy.py");

	[TestMethod]
	public Task MiddayReversal()
		=> RunStrategy("0128_Midday_Reversal/PY/midday_reversal_strategy.py");

	[TestMethod]
	public Task OvernightGap()
		=> RunStrategy("0129_Overnight_Gap/PY/overnight_gap_strategy.py");

	[TestMethod]
	public Task LunchBreakFade()
		=> RunStrategy("0130_Lunch_Break_Fade/PY/lunch_break_fade_strategy.py");

	[TestMethod]
	public Task MacdRsi()
		=> RunStrategy("0131_MACD_RSI/PY/macd_rsi_strategy.py");

	[TestMethod]
	public Task BollingerStochastic()
		=> RunStrategy("0132_Bollinger_Stochastic/PY/bollinger_stochastic_strategy.py");

	[TestMethod]
	public Task MaVolume()
		=> RunStrategy("0133_MA_Volume/PY/ma_volume_strategy.py");

	[TestMethod]
	public Task AdxMacd()
		=> RunStrategy("0134_ADX_MACD/PY/adx_macd_strategy.py");

	[TestMethod]
	public Task IchimokuRsi()
		=> RunStrategy("0135_Ichimoku_RSI/PY/ichimoku_rsi_strategy.py");

	[TestMethod]
	public Task SupertrendVolume()
		=> RunStrategy("0136_Supertrend_Volume/PY/supertrend_volume_strategy.py");

	[TestMethod]
	public Task BollingerRsi()
		=> RunStrategy("0137_Bollinger_RSI/PY/bollinger_rsi_strategy.py");

	[TestMethod]
	public Task MaStochastic()
		=> RunStrategy("0138_MA_Stochastic/PY/ma_stochastic_strategy.py");

	[TestMethod]
	public Task AtrMacd()
		=> RunStrategy("0139_ATR_MACD/PY/atr_macd_strategy.py");

	[TestMethod]
	public Task VwapRsi()
		=> RunStrategy("0140_VWAP_RSI/PY/vwap_rsi_strategy.py");

	[TestMethod]
	public Task DonchianVolume()
		=> RunStrategy("0141_Donchian_Volume/PY/donchian_volume_strategy.py");

	[TestMethod]
	public Task KeltnerStochastic()
		=> RunStrategy("0142_Keltner_Stochastic/PY/keltner_stochastic_strategy.py");

	[TestMethod]
	public Task ParabolicSarRsi()
		=> RunStrategy("0143_Parabolic_SAR_RSI/PY/parabolic_sar_rsi_strategy.py");

	[TestMethod]
	public Task HullMaVolume()
		=> RunStrategy("0144_Hull_MA_Volume/PY/hull_ma_volume_strategy.py");

	[TestMethod]
	public Task AdxStochastic()
		=> RunStrategy("0145_ADX_Stochastic/PY/adx_stochastic_strategy.py");

	[TestMethod]
	public Task MacdVolume()
		=> RunStrategy("0146_MACD_Volume/PY/macd_volume_strategy.py");

	[TestMethod]
	public Task BollingerVolume()
		=> RunStrategy("0147_Bollinger_Volume/PY/bollinger_volume_strategy.py");

	[TestMethod]
	public Task RsiStochastic()
		=> RunStrategy("0148_RSI_Stochastic/PY/rsi_stochastic_strategy.py");

	[TestMethod]
	public Task MaAdx()
		=> RunStrategy("0149_MA_ADX/PY/ma_adx_strategy.py");

	[TestMethod]
	public Task VwapStochastic()
		=> RunStrategy("0150_VWAP_Stochastic/PY/vwap_stochastic_strategy.py");

	[TestMethod]
	public Task IchimokuVolume()
		=> RunStrategy("0151_Ichimoku_Volume/PY/ichimoku_volume_strategy.py");

	[TestMethod]
	public Task SupertrendRsi()
		=> RunStrategy("0152_Supertrend_RSI/PY/supertrend_rsi_strategy.py");

	[TestMethod]
	public Task BollingerAdx()
		=> RunStrategy("0153_Bollinger_ADX/PY/bollinger_adx_strategy.py");

	[TestMethod]
	public Task MaCci()
		=> RunStrategy("0154_MA_CCI/PY/ma_cci_strategy.py");

	[TestMethod]
	public Task VwapVolume()
		=> RunStrategy("0155_VWAP_Volume/PY/vwap_volume_strategy.py");

	[TestMethod]
	public Task DonchianRsi()
		=> RunStrategy("0156_Donchian_RSI/PY/donchian_rsi_strategy.py");

	[TestMethod]
	public Task KeltnerVolume()
		=> RunStrategy("0157_Keltner_Volume/PY/keltner_volume_strategy.py");

	[TestMethod]
	public Task ParabolicSarStochastic()
		=> RunStrategy("0158_Parabolic_SAR_Stochastic/PY/parabolic_sar_stochastic_strategy.py");

	[TestMethod]
	public Task HullMaRsi()
		=> RunStrategy("0159_Hull_MA_RSI/PY/hull_ma_rsi_strategy.py");

	[TestMethod]
	public Task AdxVolume()
		=> RunStrategy("0160_ADX_Volume/PY/adx_volume_strategy.py");

	[TestMethod]
	public Task MacdCci()
		=> RunStrategy("0161_MACD_CCI/PY/macd_cci_strategy.py");

	[TestMethod]
	public Task BollingerCci()
		=> RunStrategy("0162_Bollinger_CCI/PY/bollinger_cci_strategy.py");

	[TestMethod]
	public Task RsiWilliamsR()
		=> RunStrategy("0163_RSI_Williams_R/PY/rsi_williams_r_strategy.py");

	[TestMethod]
	public Task MaWilliamsR()
		=> RunStrategy("0164_MA_Williams_R/PY/ma_williams_r_strategy.py");

	[TestMethod]
	public Task VwapCci()
		=> RunStrategy("0165_VWAP_CCI/PY/vwap_cci_strategy.py");

	[TestMethod]
	public Task DonchianStochastic()
		=> RunStrategy("0166_Donchian_Stochastic/PY/donchian_stochastic_strategy.py");

	[TestMethod]
	public Task KeltnerRsi()
		=> RunStrategy("0167_Keltner_RSI/PY/keltner_rsi_strategy.py");

	[TestMethod]
	public Task HullMaStochastic()
		=> RunStrategy("0169_Hull_MA_Stochastic/PY/hull_ma_stochastic_strategy.py");

	[TestMethod]
	public Task AdxCci()
		=> RunStrategy("0170_ADX_CCI/PY/adx_cci_strategy.py");

	[TestMethod]
	public Task MacdWilliamsR()
		=> RunStrategy("0171_MACD_Williams_R/PY/macd_williams_r_strategy.py");

	[TestMethod]
	public Task BollingerWilliamsR()
		=> RunStrategy("0172_Bollinger_Williams_R/PY/bollinger_williams_r_strategy.py");

	[TestMethod]
	public Task MacdVwap()
		=> RunStrategy("0174_MACD_VWAP/PY/macd_vwap_strategy.py");

	[TestMethod]
	public Task RsiSupertrend()
		=> RunStrategy("0175_RSI_Supertrend/PY/rsi_supertrend_strategy.py");

	[TestMethod]
	public Task AdxBollinger()
		=> RunStrategy("0176_ADX_Bollinger/PY/adx_bollinger_strategy.py");

	[TestMethod]
	public Task IchimokuStochastic()
		=> RunStrategy("0177_Ichimoku_Stochastic/PY/ichimoku_stochastic_strategy.py");

	[TestMethod]
	public Task SupertrendStochastic()
		=> RunStrategy("0185_Supertrend_Stochastic/PY/supertrend_stochastic_strategy.py");

	[TestMethod]
	public Task DonchianMacd()
		=> RunStrategy("0187_Donchian_MACD/PY/donchian_macd_strategy.py");

	[TestMethod]
	public Task ParabolicSarVolume()
		=> RunStrategy("0188_Parabolic_SAR_Volume/PY/parabolic_sar_volume_strategy.py");

	[TestMethod]
	public Task VwapAdx()
		=> RunStrategy("0190_VWAP_ADX/PY/vwap_adx_strategy.py");

	[TestMethod]
	public Task SupertrendAdx()
		=> RunStrategy("0193_Supertrend_ADX/PY/supertrend_adx_strategy.py");

	[TestMethod]
	public Task KeltnerMacd()
		=> RunStrategy("0194_Keltner_MACD/PY/keltner_macd_strategy.py");

	[TestMethod]
	public Task HullMaAdx()
		=> RunStrategy("0197_Hull_MA_ADX/PY/hull_ma_adx_strategy.py");

	[TestMethod]
	public Task VwapMacd()
		=> RunStrategy("0198_VWAP_MACD/PY/vwap_macd_strategy.py");

	[TestMethod]
	public Task IchimokuAdx()
		=> RunStrategy("0200_Ichimoku_ADX/PY/ichimoku_adx_strategy.py");

	[TestMethod]
	public Task VwapWilliamsR()
		=> RunStrategy("0201_VWAP_Williams_R/PY/vwap_williams_r_strategy.py");

	[TestMethod]
	public Task DonchianCci()
		=> RunStrategy("0202_Donchian_CCI/PY/donchian_cci_strategy.py");

	[TestMethod]
	public Task KeltnerWilliamsR()
		=> RunStrategy("0203_Keltner_Williams_R/PY/keltner_williams_r_strategy.py");

	[TestMethod]
	public Task ParabolicSarCci()
		=> RunStrategy("0204_Parabolic_SAR_CCI/PY/parabolic_sar_cci_strategy.py");

	[TestMethod]
	public Task HullMaCci()
		=> RunStrategy("0205_Hull_MA_CCI/PY/hull_ma_cci_strategy.py");

	[TestMethod]
	public Task MacdBollinger()
		=> RunStrategy("0206_MACD_Bollinger/PY/macd_bollinger_strategy.py");

	[TestMethod]
	public Task RsiHullMa()
		=> RunStrategy("0207_RSI_Hull_MA/PY/rsi_hull_ma_strategy.py");

	[TestMethod]
	public Task StochasticKeltner()
		=> RunStrategy("0208_Stochastic_Keltner/PY/stochastic_keltner_strategy.py");

	[TestMethod]
	public Task VolumeSupertrend()
		=> RunStrategy("0209_Volume_Supertrend/PY/volume_supertrend_strategy.py");

	[TestMethod]
	public Task AdxDonchian()
		=> RunStrategy("0210_ADX_Donchian/PY/adx_donchian_strategy.py");

	[TestMethod]
	public Task CciVwap()
		=> RunStrategy("0211_CCI_VWAP/PY/cci_vwap_strategy.py");

	[TestMethod]
	public Task WilliamsIchimoku()
		=> RunStrategy("0212_Williams_R_Ichimoku/PY/williams_ichimoku_strategy.py");

	[TestMethod]
	public Task MaParabolicSar()
		=> RunStrategy("0213_MA_Parabolic_SAR/PY/ma_parabolic_sar_strategy.py");

	[TestMethod]
	public Task BollingerSupertrend()
		=> RunStrategy("0214_Bollinger_Supertrend/PY/bollinger_supertrend_strategy.py");

	[TestMethod]
	public Task RsiDonchian()
		=> RunStrategy("0215_RSI_Donchian/PY/rsi_donchian_strategy.py");

	[TestMethod]
	public Task MeanReversion()
		=> RunStrategy("0216_Mean_Reversion/PY/mean_reversion_strategy.py");

	[TestMethod]
	public Task PairsTrading()
		=> RunStrategy("0217_Pairs_Trading/PY/pairs_trading_strategy.py", (stra, sec) =>
		{
			stra.Parameters["SecondSecurity"].Value = sec;
		});

	[TestMethod]
	public Task ZScoreReversal()
		=> RunStrategy("0218_ZScore_Reversal/PY/z_score_reversal_strategy.py");

	[TestMethod]
	public Task StatisticalArbitrage()
		=> RunStrategy("0219_Statistical_Arbitrage/PY/statistical_arbitrage_strategy.py", (stra, sec) =>
		{
			stra.Parameters["SecondSecurity"].Value = sec;
		});

	[TestMethod]
	public Task VolatilityBreakout()
		=> RunStrategy("0220_Volatility_Breakout/PY/volatility_breakout_strategy.py");

	[TestMethod]
	public Task BollingerBandSqueeze()
		=> RunStrategy("0221_Bollinger_Band_Squeeze/PY/bollinger_band_squeeze_strategy.py");

	[TestMethod]
	public Task CointegrationPairs()
		=> RunStrategy("0222_Cointegration_Pairs/PY/cointegration_pairs_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Asset2"].Value = sec;
		});

	[TestMethod]
	public Task MomentumDivergence()
		=> RunStrategy("0223_Momentum_Divergence/PY/momentum_divergence_strategy.py");

	[TestMethod]
	public Task AtrMeanReversion()
		=> RunStrategy("0224_ATR_Mean_Reversion/PY/atr_mean_reversion_strategy.py");

	[TestMethod]
	public Task KalmanFilterTrend()
		=> RunStrategy("0225_Kalman_Filter_Trend/PY/kalman_filter_trend_strategy.py");

	[TestMethod]
	public Task VolatilityAdjustedMeanReversion()
		=> RunStrategy("0226_Volatility_Adjusted_Mean_Reversion/PY/volatility_adjusted_mean_reversion_strategy.py");

	[TestMethod]
	public Task HurstExponentTrend()
		=> RunStrategy("0227_Hurst_Exponent_Trend/PY/hurst_exponent_trend_strategy.py");

	[TestMethod]
	public Task HurstExponentReversion()
		=> RunStrategy("0228_Hurst_Exponent_Reversion/PY/hurst_exponent_reversion_strategy.py");

	[TestMethod]
	public Task AutocorrelationReversion()
		=> RunStrategy("0229_Autocorrelation_Reversal/PY/autocorrelation_reversion_strategy.py");

	[TestMethod]
	public Task DeltaNeutralArbitrage()
		=> RunStrategy("0230_Delta_Neutral_Arbitrage/PY/delta_neutral_arbitrage_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Asset2Security"].Value = sec;
			stra.Parameters["Asset2Portfolio"].Value = stra.Portfolio;
		});

	[TestMethod]
	public Task VolatilitySkewArbitrage()
		=> RunStrategy("0231_Volatility_Skew_Arbitrage/PY/volatility_skew_arbitrage_strategy.py");

	[TestMethod]
	public Task CorrelationBreakout()
		=> RunStrategy("0232_Correlation_Breakout/PY/correlation_breakout_strategy.py");

	[TestMethod]
	public Task BetaNeutralArbitrage()
		=> RunStrategy("0233_Beta_Neutral_Arbitrage/PY/beta_neutral_arbitrage_strategy.py");

	[TestMethod]
	public Task VwapMeanReversion()
		=> RunStrategy("0235_VWAP_Mean_Reversion/PY/vwap_mean_reversion_strategy.py");

	[TestMethod]
	public Task RsiMeanReversion()
		=> RunStrategy("0236_RSI_Mean_Reversion/PY/rsi_mean_reversion_strategy.py");

	[TestMethod]
	public Task StochasticMeanReversion()
		=> RunStrategy("0237_Stochastic_Mean_Reversion/PY/stochastic_mean_reversion_strategy.py");

	[TestMethod]
	public Task CciMeanReversion()
		=> RunStrategy("0238_CCI_Mean_Reversion/PY/cci_mean_reversion_strategy.py");

	[TestMethod]
	public Task WilliamsRMeanReversion()
		=> RunStrategy("0239_Williams_R_Mean_Reversion/PY/williams_r_mean_reversion_strategy.py");

	[TestMethod]
	public Task MacdMeanReversion()
		=> RunStrategy("0240_MACD_Mean_Reversion/PY/macd_mean_reversion_strategy.py");

	[TestMethod]
	public Task AdxMeanReversion()
		=> RunStrategy("0241_ADX_Mean_Reversion/PY/adx_mean_reversion_strategy.py");

	[TestMethod]
	public Task VolatilityMeanReversion()
		=> RunStrategy("0242_Volatility_Mean_Reversion/PY/volatility_mean_reversion_strategy.py");

	[TestMethod]
	public Task VolumeMeanReversion()
		=> RunStrategy("0243_Volume_Mean_Reversion/PY/volume_mean_reversion_strategy.py");

	[TestMethod]
	public Task ObvMeanReversion()
		=> RunStrategy("0244_OBV_Mean_Reversion/PY/obv_mean_reversion_strategy.py");

	[TestMethod]
	public Task MomentumBreakout()
		=> RunStrategy("0245_Momentum_Breakout/PY/momentum_breakout_strategy.py");

	[TestMethod]
	public Task RsiBreakout()
		=> RunStrategy("0247_RSI_Breakout/PY/rsi_breakout_strategy.py");

	[TestMethod]
	public Task StochasticBreakout()
		=> RunStrategy("0248_Stochastic_Breakout/PY/stochastic_breakout_strategy.py");

	[TestMethod]
	public Task WilliamsRBreakout()
		=> RunStrategy("0250_Williams_R_Breakout/PY/williams_r_breakout_strategy.py");

	[TestMethod]
	public Task MacdBreakout()
		=> RunStrategy("0251_MACD_Breakout/PY/macd_breakout_strategy.py");

	[TestMethod]
	public Task AdxBreakout()
		=> RunStrategy("0252_ADX_Breakout/PY/adx_breakout_strategy.py");

	[TestMethod]
	public Task VolumeBreakout()
		=> RunStrategy("0254_Volume_Breakout/PY/volume_breakout_strategy.py");

	[TestMethod]
	public Task BollingerBandWidthBreakout()
		=> RunStrategy("0256_Bollinger_Band_Width_Breakout/PY/bollinger_band_width_breakout_strategy.py");

	[TestMethod]
	public Task KeltnerWidthBreakout()
		=> RunStrategy("0257_Keltner_Channel_Width_Breakout/PY/keltner_width_breakout_strategy.py");

	[TestMethod]
	public Task DonchianWidthBreakout()
		=> RunStrategy("0258_Donchian_Channel_Width_Breakout/PY/donchian_width_breakout_strategy.py");

	[TestMethod]
	public Task IchimokuWidthBreakout()
		=> RunStrategy("0259_Ichimoku_Cloud_Width_Breakout/PY/ichimoku_width_breakout_strategy.py");

	[TestMethod]
	public Task SupertrendDistanceBreakout()
		=> RunStrategy("0260_Supertrend_Distance_Breakout/PY/supertrend_distance_breakout_strategy.py");

	[TestMethod]
	public Task ParabolicSarDistanceBreakout()
		=> RunStrategy("0261_Parabolic_SAR_Distance_Breakout/PY/parabolic_sar_distance_breakout_strategy.py");

	[TestMethod]
	public Task HullMaSlopeBreakout()
		=> RunStrategy("0262_Hull_MA_Slope_Breakout/PY/hull_ma_slope_breakout_strategy.py");

	[TestMethod]
	public Task MaSlopeBreakout()
		=> RunStrategy("0263_MA_Slope_Breakout/PY/ma_slope_breakout_strategy.py");

	[TestMethod]
	public Task EmaSlopeBreakout()
		=> RunStrategy("0264_EMA_Slope_Breakout/PY/ema_slope_breakout_strategy.py");

	[TestMethod]
	public Task VolatilityAdjustedMomentum()
		=> RunStrategy("0265_Volatility_Adjusted_Momentum/PY/volatility_adjusted_momentum_strategy.py");

	[TestMethod]
	public Task VwapSlopeBreakout()
		=> RunStrategy("0266_VWAP_Slope_Breakout/PY/vwap_slope_breakout_strategy.py");

	[TestMethod]
	public Task RsiSlopeBreakout()
		=> RunStrategy("0267_RSI_Slope_Breakout/PY/rsi_slope_breakout_strategy.py");

	[TestMethod]
	public Task StochasticSlopeBreakout()
		=> RunStrategy("0268_Stochastic_Slope_Breakout/PY/stochastic_slope_breakout_strategy.py");

	[TestMethod]
	public Task CciSlopeBreakout()
		=> RunStrategy("0269_CCI_Slope_Breakout/PY/cci_slope_breakout_strategy.py");

	[TestMethod]
	public Task WilliamsRSlopeBreakout()
		=> RunStrategy("0270_Williams_R_Slope_Breakout/PY/williams_r_slope_breakout_strategy.py");

	[TestMethod]
	public Task MacdSlopeBreakout()
		=> RunStrategy("0271_MACD_Slope_Breakout/PY/macd_slope_breakout_strategy.py");

	[TestMethod]
	public Task AdxSlopeBreakout()
		=> RunStrategy("0272_ADX_Slope_Breakout/PY/adx_slope_breakout_strategy.py");

	[TestMethod]
	public Task AtrSlopeBreakout()
		=> RunStrategy("0273_ATR_Slope_Breakout/PY/atr_slope_breakout_strategy.py");

	[TestMethod]
	public Task VolumeSlopeBreakout()
		=> RunStrategy("0274_Volume_Slope_Breakout/PY/volume_slope_breakout_strategy.py");

	[TestMethod]
	public Task ObvSlopeBreakout()
		=> RunStrategy("0275_OBV_Slope_Breakout/PY/obv_slope_breakout_strategy.py");

	[TestMethod]
	public Task BollingerWidthMeanReversion()
		=> RunStrategy("0276_Bollinger_Width_Mean_Reversion/PY/bollinger_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task KeltnerWidthMeanReversion()
		=> RunStrategy("0277_Keltner_Width_Mean_Reversion/PY/keltner_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task DonchianWidthMeanReversion()
		=> RunStrategy("0278_Donchian_Width_Mean_Reversion/PY/donchian_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task IchimokuCloudWidthMeanReversion()
		=> RunStrategy("0279_Ichimoku_Cloud_Width_Mean_Reversion/PY/ichimoku_cloud_width_mean_reversion_strategy.py");

	[TestMethod]
	public Task SupertrendDistanceMeanReversion()
		=> RunStrategy("0280_Supertrend_Distance_Mean_Reversion/PY/supertrend_distance_mean_reversion_strategy.py");

	[TestMethod]
	public Task ParabolicSarDistanceMeanReversion()
		=> RunStrategy("0281_Parabolic_SAR_Distance_Mean_Reversion/PY/parabolic_sar_distance_mean_reversion_strategy.py");

	[TestMethod]
	public Task HullMaSlopeMeanReversion()
		=> RunStrategy("0282_Hull_MA_Slope_Mean_Reversion/PY/hull_ma_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task MaSlopeMeanReversion()
		=> RunStrategy("0283_MA_Slope_Mean_Reversion/PY/ma_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task EmaSlopeMeanReversion()
		=> RunStrategy("0284_EMA_Slope_Mean_Reversion/PY/ema_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task VwapSlopeMeanReversion()
		=> RunStrategy("0285_VWAP_Slope_Mean_Reversion/PY/vwap_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task RsiSlopeMeanReversion()
		=> RunStrategy("0286_RSI_Slope_Mean_Reversion/PY/rsi_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task StochasticSlopeMeanReversion()
		=> RunStrategy("0287_Stochastic_Slope_Mean_Reversion/PY/stochastic_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task CciSlopeMeanReversion()
		=> RunStrategy("0288_CCI_Slope_Mean_Reversion/PY/cci_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task WilliamsRSlopeMeanReversion()
		=> RunStrategy("0289_Williams_R_Slope_Mean_Reversion/PY/williams_r_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task MacdSlopeMeanReversion()
		=> RunStrategy("0290_MACD_Slope_Mean_Reversion/PY/macd_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task AdxSlopeMeanReversion()
		=> RunStrategy("0291_ADX_Slope_Mean_Reversion/PY/adx_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task AtrSlopeMeanReversion()
		=> RunStrategy("0292_ATR_Slope_Mean_Reversion/PY/atr_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task VolumeSlopeMeanReversion()
		=> RunStrategy("0293_Volume_Slope_Mean_Reversion/PY/volume_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task ObvSlopeMeanReversion()
		=> RunStrategy("0294_OBV_Slope_Mean_Reversion/PY/obv_slope_mean_reversion_strategy.py");

	[TestMethod]
	public Task PairsTradingVolatilityFilter()
		=> RunStrategy("0295_Pairs_Trading_Volatility_Filter/PY/pairs_trading_volatility_filter_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Security2"].Value = sec;
		});

	[TestMethod]
	public Task ZscoreVolumeFilter()
		=> RunStrategy("0296_Z-Score_Volume_Filter/PY/zscore_volume_filter_strategy.py");

	[TestMethod]
	public Task CorrelationMeanReversion()
		=> RunStrategy("0298_Correlation_Mean_Reversion/PY/correlation_mean_reversion_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Security2"].Value = sec;
		});

	[TestMethod]
	public Task BetaAdjustedPairs()
		=> RunStrategy("0299_Beta_Adjusted_Pairs_Trading/PY/beta_adjusted_pairs_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Asset2"].Value = sec;
			stra.Parameters["Asset2Portfolio"].Value = stra.Portfolio;
		});

	[TestMethod]
	public Task HurstVolatilityFilter()
		=> RunStrategy("0300_Hurst_Exponent_Volatility_Filter/PY/hurst_volatility_filter_strategy.py");

	[TestMethod]
	public Task AdaptiveEmaBreakout()
		=> RunStrategy("0301_Adaptive_EMA_Breakout/PY/adaptive_ema_breakout_strategy.py");

	[TestMethod]
	public Task VolatilityClusterBreakout()
		=> RunStrategy("0302_Volatility_Cluster_Breakout/PY/volatility_cluster_breakout_strategy.py");

	[TestMethod]
	public Task SeasonalityAdjustedMomentum()
		=> RunStrategy("0303_Seasonality_Adjusted_Momentum/PY/seasonality_adjusted_momentum_strategy.py");

	[TestMethod]
	public Task RsiDynamicOverboughtOversold()
		=> RunStrategy("0305_RSI_Dynamic_Overbought_Oversold/PY/rsi_dynamic_overbought_oversold_strategy.py");

	[TestMethod]
	public Task BollingerVolatilityBreakout()
		=> RunStrategy("0306_Bollinger_Volatility_Breakout/PY/bollinger_volatility_breakout_strategy.py");

	[TestMethod]
	public Task MacdAdaptiveHistogram()
		=> RunStrategy("0307_MACD_Adaptive_Histogram/PY/macd_adaptive_histogram_strategy.py");

	[TestMethod]
	public Task IchimokuVolumeCluster()
		=> RunStrategy("0308_Ichimoku_Volume_Cluster/PY/ichimoku_volume_cluster_strategy.py");

	[TestMethod]
	public Task SupertrendMomentumFilter()
		=> RunStrategy("0309_Supertrend_Momentum_Filter/PY/supertrend_momentum_filter_strategy.py");

	[TestMethod]
	public Task DonchianVolatilityContraction()
		=> RunStrategy("0310_Donchian_Volatility_Contraction/PY/donchian_volatility_contraction_strategy.py");

	[TestMethod]
	public Task KeltnerRsiDivergence()
		=> RunStrategy("0311_Keltner_RSI_Divergence/PY/keltner_rsi_divergence_strategy.py");

	[TestMethod]
	public Task HullMaVolumeSpike()
		=> RunStrategy("0312_Hull_MA_Volume_Spike/PY/hull_ma_volume_spike_strategy.py");

	[TestMethod]
	public Task VwapAdxTrendStrength()
		=> RunStrategy("0313_VWAP_ADX_Trend_Strength/PY/vwap_adx_trend_strength_strategy.py");

	[TestMethod]
	public Task ParabolicSarVolatilityExpansion()
		=> RunStrategy("0314_Parabolic_SAR_Volatility_Expansion/PY/parabolic_sar_volatility_expansion_strategy.py");

	[TestMethod]
	public Task StochasticWithDynamicZones()
		=> RunStrategy("0315_Stochastic_Dynamic_Zones/PY/stochastic_with_dynamic_zones_strategy.py");

	[TestMethod]
	public Task AdxWithVolumeBreakout()
		=> RunStrategy("0316_ADX_Volume_Breakout/PY/adx_with_volume_breakout_strategy.py");

	[TestMethod]
	public Task CciWithVolatilityFilter()
		=> RunStrategy("0317_CCI_Volatility_Filter/PY/cci_with_volatility_filter_strategy.py");

	[TestMethod]
	public Task WilliamsPercentRWithMomentum()
		=> RunStrategy("0318_Williams_R_Momentum/PY/williams_percent_r_with_momentum_strategy.py");

	[TestMethod]
	public Task BollingerKmeans()
		=> RunStrategy("0319_Bollinger_K-Means_Cluster/PY/bollinger_kmeans_strategy.py");

	[TestMethod]
	public Task MacdHiddenMarkovModel()
		=> RunStrategy("0320_MACD_Hidden_Markov_Model/PY/macd_hidden_markov_model_strategy.py");

	[TestMethod]
	public Task IchimokuHurstExponent()
		=> RunStrategy("0321_Ichimoku_Hurst_Exponent/PY/ichimoku_hurst_exponent_strategy.py");

	[TestMethod]
	public Task SupertrendRsiDivergence()
		=> RunStrategy("0322_Supertrend_RSI_Divergence/PY/supertrend_rsi_divergence_strategy.py");

	[TestMethod]
	public Task DonchianSeasonalFilter()
		=> RunStrategy("0323_Donchian_Seasonal_Filter/PY/donchian_seasonal_filter_strategy.py");

	[TestMethod]
	public Task KeltnerKalman()
		=> RunStrategy("0324_Keltner_Kalman_Filter/PY/keltner_kalman_strategy.py");

	[TestMethod]
	public Task HullMaVolatilityContraction()
		=> RunStrategy("0325_Hull_MA_Volatility_Contraction/PY/hull_ma_volatility_contraction_strategy.py");

	[TestMethod]
	public Task VwapAdxTrend()
		=> RunStrategy("0326_VWAP_Stochastic_Divergence/PY/vwap_adx_trend_strategy.py");

	[TestMethod]
	public Task ParabolicSarHurst()
		=> RunStrategy("0327_Parabolic_SAR_Hurst_Filter/PY/parabolic_sar_hurst_strategy.py");

	[TestMethod]
	public Task BollingerKalmanFilter()
		=> RunStrategy("0328_Bollinger_Kalman_Filter/PY/bollinger_kalman_filter_strategy.py");

	[TestMethod]
	public Task MacdVolumeCluster()
		=> RunStrategy("0329_MACD_Volume_Cluster/PY/macd_volume_cluster_strategy.py");

	[TestMethod]
	public Task IchimokuVolatilityContraction()
		=> RunStrategy("0330_Ichimoku_Volatility_Contraction/PY/ichimoku_volatility_contraction_strategy.py");

	[TestMethod]
	public Task DonchianHurst()
		=> RunStrategy("0332_Donchian_Hurst_Exponent/PY/donchian_hurst_strategy.py");

	[TestMethod]
	public Task KeltnerSeasonal()
		=> RunStrategy("0333_Keltner_Seasonal_Filter/PY/keltner_seasonal_strategy.py");

	[TestMethod]
	public Task HullKmeansCluster()
		=> RunStrategy("0334_Hull_MA_K-Means_Cluster/PY/hull_kmeans_cluster_strategy.py");

	[TestMethod]
	public Task VwapHiddenMarkovModel()
		=> RunStrategy("0335_VWAP_Hidden_Markov_Model/PY/vwap_hidden_markov_model_strategy.py");

	[TestMethod]
	public Task ParabolicSarRsiDivergence()
		=> RunStrategy("0336_Parabolic_SAR_RSI_Divergence/PY/parabolic_sar_rsi_divergence_strategy.py");

	[TestMethod]
	public Task AdaptiveRsiVolume()
		=> RunStrategy("0337_Adaptive_RSI_Volume_Filter/PY/adaptive_rsi_volume_strategy.py");

	[TestMethod]
	public Task AdaptiveBollingerBreakout()
		=> RunStrategy("0338_Adaptive_Bollinger_Breakout/PY/adaptive_bollinger_breakout_strategy.py");

	[TestMethod]
	public Task MacdWithSentimentFilter()
		=> RunStrategy("0339_MACD_Sentiment_Filter/PY/macd_with_sentiment_filter_strategy.py");

	[TestMethod]
	public Task IchimokuImpliedVolatility()
		=> RunStrategy("0340_Ichimoku_Implied_Volatility/PY/ichimoku_implied_volatility_strategy.py");

	[TestMethod]
	public Task SupertrendPutCallRatio()
		=> RunStrategy("0341_Supertrend_Put_Call_Ratio/PY/supertrend_put_call_ratio_strategy.py");

	[TestMethod]
	public Task DonchianWithSentimentSpike()
		=> RunStrategy("0342_Donchian_Sentiment_Spike/PY/donchian_with_sentiment_spike_strategy.py");

	[TestMethod]
	public Task KeltnerWithRlSignal()
		=> RunStrategy("0343_Keltner_Reinforcement_Learning_Signal/PY/keltner_with_rl_signal_strategy.py");

	[TestMethod]
	public Task HullMaImpliedVolatilityBreakout()
		=> RunStrategy("0344_Hull_MA_Implied_Volatility_Breakout/PY/hull_ma_implied_volatility_breakout_strategy.py");

	[TestMethod]
	public Task VwapWithBehavioralBiasFilter()
		=> RunStrategy("0345_VWAP_Behavioral_Bias_Filter/PY/vwap_with_behavioral_bias_filter_strategy.py");

	[TestMethod]
	public Task ParabolicSarSentimentDivergence()
		=> RunStrategy("0346_Parabolic_SAR_Sentiment_Divergence/PY/parabolic_sar_sentiment_divergence_strategy.py");

	[TestMethod]
	public Task RsiWithOptionOpenInterest()
		=> RunStrategy("0347_RSI_Option_Open_Interest/PY/rsi_with_option_open_interest_strategy.py");

	[TestMethod]
	public Task StochasticImpliedVolatilitySkew()
		=> RunStrategy("0348_Stochastic_Implied_Volatility_Skew/PY/stochastic_implied_volatility_skew_strategy.py");

	[TestMethod]
	public Task AdxSentimentMomentum()
		=> RunStrategy("0349_ADX_Sentiment_Momentum/PY/adx_sentiment_momentum_strategy.py");

	[TestMethod]
	public Task CciPutCallRatioDivergence()
		=> RunStrategy("0350_CCI_Put_Call_Ratio_Divergence/PY/cci_put_call_ratio_divergence_strategy.py");

	[TestMethod]
	public Task AccrualAnomaly()
		=> RunStrategy("0351_Accrual_Anomaly/PY/accrual_anomaly_strategy.py", (stra, sec) =>
		{
			stra.Parameters["Universe"].Value = new[] { sec };
		});

	[TestMethod]
	public Task AssetClassTrendFollowing()
		=> RunStrategy("0352_Asset_Class_Trend_Following/PY/asset_class_trend_following_strategy.py");

	[TestMethod]
	public Task AssetGrowthEffect()
		=> RunStrategy("0353_Asset_Growth_Effect/PY/asset_growth_effect_strategy.py");

	[TestMethod]
	public Task BettingAgainstBetaStocks()
		=> RunStrategy("0354_Betting_Against_Beta_Stocks/PY/betting_against_beta_stocks_strategy.py");

	[TestMethod]
	public Task BettingAgainstBeta()
		=> RunStrategy("0355_Betting_Against_Beta/PY/betting_against_beta_strategy.py");

	[TestMethod]
	public Task BitcoinIntradaySeasonality()
		=> RunStrategy("0356_Bitcoin_Intraday_Seasonality/PY/bitcoin_intraday_seasonality_strategy.py");

	[TestMethod]
	public Task BookToMarketValue()
		=> RunStrategy("0357_Book_To_Market_Value/PY/book_to_market_value_strategy.py");

	[TestMethod]
	public Task CommodityMomentum()
		=> RunStrategy("0358_Commodity_Momentum/PY/commodity_momentum_strategy.py");

	[TestMethod]
	public Task ConsistentMomentum()
		=> RunStrategy("0359_Consistent_Momentum/PY/consistent_momentum_strategy.py");

	[TestMethod]
	public Task CountryValueFactor()
		=> RunStrategy("0360_Country_Value_Factor/PY/country_value_factor_strategy.py");

	[TestMethod]
	public Task CrudeOilPredictsEquity()
		=> RunStrategy("0361_Crude_Oil_Predicts_Equity/PY/crude_oil_predicts_equity_strategy.py");

	[TestMethod]
	public Task CryptoRebalancingPremium()
		=> RunStrategy("0362_Crypto_Rebalancing_Premium/PY/crypto_rebalancing_premium_strategy.py");

	[TestMethod]
	public Task CurrencyMomentumFactor()
		=> RunStrategy("0363_Currency_Momentum_Factor/PY/currency_momentum_factor_strategy.py");

	[TestMethod]
	public Task CurrencyPppValue()
		=> RunStrategy("0364_Currency_PPPValue/PY/currency_ppp_value_strategy.py");

	[TestMethod]
	public Task DispersionTrading()
		=> RunStrategy("0365_Dispersion_Trading/PY/dispersion_trading_strategy.py");

	[TestMethod]
	public Task DollarCarryTrade()
		=> RunStrategy("0366_Dollar_Carry_Trade/PY/dollar_carry_trade_strategy.py");

	[TestMethod]
	public Task EarningsAnnouncementPremium()
		=> RunStrategy("0367_Earnings_Announcement_Premium/PY/earnings_announcement_premium_strategy.py");

	[TestMethod]
	public Task EarningsAnnouncementReversal()
		=> RunStrategy("0368_Earnings_Announcement_Reversal/PY/earnings_announcement_reversal_strategy.py");

	[TestMethod]
	public Task EarningsAnnouncementsWithBuybacks()
		=> RunStrategy("0369_Earnings_Announcements_With_Buybacks/PY/earnings_announcements_with_buybacks_strategy.py");

	[TestMethod]
	public Task EarningsQualityFactor()
		=> RunStrategy("0370_Earnings_Quality_Factor/PY/earnings_quality_factor_strategy.py");

	[TestMethod]
	public Task EsgFactorMomentum()
		=> RunStrategy("0371_ESGFactor_Momentum/PY/esg_factor_momentum_strategy.py");

	[TestMethod]
	public Task FedModel()
		=> RunStrategy("0372_Fed_Model/PY/fed_model_strategy.py");

	[TestMethod]
	public Task FscoreReversal()
		=> RunStrategy("0373_FScore_Reversal/PY/fscore_reversal_strategy.py");

	[TestMethod]
	public Task FxCarryTrade()
		=> RunStrategy("0374_FXCarry_Trade/PY/fx_carry_trade_strategy.py");

	[TestMethod]
	public Task JanuaryBarometer()
		=> RunStrategy("0375_January_Barometer/PY/january_barometer_strategy.py");

	[TestMethod]
	public Task LexicalDensityFilings()
		=> RunStrategy("0376_Lexical_Density_Filings/PY/lexical_density_filings_strategy.py");

	[TestMethod]
	public Task LowVolatilityStocks()
		=> RunStrategy("0377_Low_Volatility_Stocks/PY/low_volatility_stocks_strategy.py");

	[TestMethod]
	public Task MomentumAssetGrowth()
		=> RunStrategy("0378_Momentum_Asset_Growth/PY/momentum_asset_growth_strategy.py");

	[TestMethod]
	public Task MomentumFactorStocks()
		=> RunStrategy("0379_Momentum_Factor_Stocks/PY/momentum_factor_stocks_strategy.py");

	[TestMethod]
	public Task MomentumRevVol()
		=> RunStrategy("0380_Momentum_Rev_Vol/PY/momentum_rev_vol_strategy.py");

	[TestMethod]
	public Task MomentumStyleRotation()
		=> RunStrategy("0381_Momentum_Style_Rotation/PY/momentum_style_rotation_strategy.py");

	[TestMethod]
	public Task Month12Cycle()
		=> RunStrategy("0382_Month12Cycle/PY/month12_cycle_strategy.py");

	[TestMethod]
	public Task MutualFundMomentum()
		=> RunStrategy("0383_Mutual_Fund_Momentum/PY/mutual_fund_momentum_strategy.py");

	[TestMethod]
	public Task OptionExpirationWeek()
		=> RunStrategy("0384_Option_Expiration_Week/PY/option_expiration_week_strategy.py");

	[TestMethod]
	public Task OvernightSentimentAnomaly()
		=> RunStrategy("0385_Overnight_Sentiment_Anomaly/PY/overnight_sentiment_anomaly_strategy.py");

	[TestMethod]
	public Task PairedSwitching()
		=> RunStrategy("0386_Paired_Switching/PY/paired_switching_strategy.py");

	[TestMethod]
	public Task PairsTradingCountryEtfs()
		=> RunStrategy("0387_Pairs_Trading_Country_ETFs/PY/pairs_trading_country_etfs_strategy.py");

	[TestMethod]
	public Task PairsTradingStocks()
		=> RunStrategy("0388_Pairs_Trading_Stocks/PY/pairs_trading_stocks_strategy.py");

	[TestMethod]
	public Task PaydayAnomaly()
		=> RunStrategy("0389_Payday_Anomaly/PY/payday_anomaly_strategy.py");

	[TestMethod]
	public Task RdExpenditures()
		=> RunStrategy("0390_RDExpenditures/PY/rd_expenditures_strategy.py");

	[TestMethod]
	public Task ResidualMomentumFactor()
		=> RunStrategy("0391_Residual_Momentum_Factor/PY/residual_momentum_factor_strategy.py");

	[TestMethod]
	public Task ReturnAsymmetryCommodity()
		=> RunStrategy("0392_Return_Asymmetry_Commodity/PY/return_asymmetry_commodity_strategy.py");

	[TestMethod]
	public Task RoaEffectStocks()
		=> RunStrategy("0393_ROAEffect_Stocks/PY/roa_effect_stocks_strategy.py");

	[TestMethod]
	public Task SectorMomentumRotation()
		=> RunStrategy("0394_Sector_Momentum_Rotation/PY/sector_momentum_rotation_strategy.py");

	[TestMethod]
	public Task ShortInterestEffect()
		=> RunStrategy("0395_Short_Interest_Effect/PY/short_interest_effect_strategy.py");

	[TestMethod]
	public Task ShortTermReversalFutures()
		=> RunStrategy("0396_Short_Term_Reversal_Futures/PY/short_term_reversal_futures_strategy.py");

	[TestMethod]
	public Task ShortTermReversalStocks()
		=> RunStrategy("0397_Short_Term_Reversal_Stocks/PY/short_term_reversal_stocks_strategy.py");

	[TestMethod]
	public Task SkewnessCommodity()
		=> RunStrategy("0398_Skewness_Commodity/PY/skewness_commodity_strategy.py");

	[TestMethod]
	public Task SmallCapPremium()
		=> RunStrategy("0399_Small_Cap_Premium/PY/small_cap_premium_strategy.py");

	[TestMethod]
	public Task SmartFactorsMomentumMarket()
		=> RunStrategy("0400_Smart_Factors_Momentum_Market/PY/smart_factors_momentum_market_strategy.py");

	[TestMethod]
	public Task SoccerClubsArbitrage()
		=> RunStrategy("0401_Soccer_Clubs_Arbitrage/PY/soccer_clubs_arbitrage_strategy.py");

	[TestMethod]
	public Task SyntheticLendingRates()
		=> RunStrategy("0402_Synthetic_Lending_Rates/PY/synthetic_lending_rates_strategy.py");

	[TestMethod]
	public Task TermStructureCommodities()
		=> RunStrategy("0403_Term_Structure_Commodities/PY/term_structure_commodities_strategy.py");

	[TestMethod]
	public Task TimeSeriesMomentum()
		=> RunStrategy("0404_Time_Series_Momentum/PY/time_series_momentum_strategy.py");

	[TestMethod]
	public Task TrendFollowingStocks()
		=> RunStrategy("0405_Trend_Following_Stocks/PY/trend_following_stocks_strategy.py");

	[TestMethod]
	public Task TurnOfMonth()
		=> RunStrategy("0406_Turn_Of_Month/PY/turn_of_month_strategy.py");

	[TestMethod]
	public Task ValueMomentumAcrossAssets()
		=> RunStrategy("0407_Value_Momentum_Across_Assets/PY/value_momentum_across_assets_strategy.py");

	[TestMethod]
	public Task VolatilityRiskPremium()
		=> RunStrategy("0408_Volatility_Risk_Premium/PY/volatility_risk_premium_strategy.py");

	[TestMethod]
	public Task Weeks52High()
		=> RunStrategy("0409_Weeks52High/PY/weeks52_high_strategy.py");

	[TestMethod]
	public Task WtiBrentSpread()
		=> RunStrategy("0410_WTIBrent_Spread/PY/wti_brent_spread_strategy.py");

	[TestMethod]
	public Task AssetClassMomentumRotational()
		=> RunStrategy("0411_Asset_Class_Momentum_Rotational/PY/asset_class_momentum_rotational_strategy.py");

	[TestMethod]
	public Task BollingerAroon()
		=> RunStrategy("0412_Bollinger_Aroon/PY/bollinger_aroon_strategy.py");

	[TestMethod]
	public Task BollingerDivergence()
		=> RunStrategy("0413_Bollinger_Divergence/PY/bollinger_divergence_strategy.py");

	[TestMethod]
	public Task BollingerWinnerLite()
		=> RunStrategy("0414_Bollinger_Winner_Lite/PY/bollinger_winner_lite_strategy.py");

	[TestMethod]
	public Task BollingerWinnerPro()
		=> RunStrategy("0415_Bollinger_Winner_Pro/PY/bollinger_winner_pro_strategy.py");

	[TestMethod]
	public Task BollingerBreakout()
		=> RunStrategy("0416_Bollinger_Breakout/PY/bollinger_breakout_strategy.py");

	[TestMethod]
	public Task DmiWinner()
		=> RunStrategy("0417_Dmi_Winner/PY/dmi_winner_strategy.py");

	[TestMethod]
	public Task DoubleRsi()
		=> RunStrategy("0418_Double_Rsi/PY/double_rsi_strategy.py");

	[TestMethod]
	public Task DoubleSupertrend()
		=> RunStrategy("0419_Double_Supertrend/PY/double_supertrend_strategy.py");

	[TestMethod]
	public Task EmaMovingAway()
		=> RunStrategy("0420_Ema_Moving_Away/PY/ema_moving_away_strategy.py");

	[TestMethod]
	public Task EmaSmaRsi()
		=> RunStrategy("0421_Ema_Sma_Rsi/PY/ema_sma_rsi_strategy.py");

	[TestMethod]
	public Task ExceededCandle()
		=> RunStrategy("0422_Exceeded_Candle/PY/exceeded_candle_strategy.py");

	[TestMethod]
	public Task FlawlessVictory()
		=> RunStrategy("0423_Flawless_Victory/PY/flawless_victory_strategy.py");

	[TestMethod]
	public Task FullCandle()
		=> RunStrategy("0424_Full_Candle/PY/full_candle_strategy.py");

	[TestMethod]
	public Task GridBot()
		=> RunStrategy("0425_Grid_Bot/PY/grid_bot_strategy.py");

	[TestMethod]
	public Task HaUniversal()
		=> RunStrategy("0426_Ha_Universal/PY/ha_universal_strategy.py");

	[TestMethod]
	public Task HeikinAshiV2()
		=> RunStrategy("0427_Heikin_Ashi_V2/PY/heikin_ashi_v2_strategy.py");

	[TestMethod]
	public Task Improvisando()
		=> RunStrategy("0428_Improvisando/PY/improvisando_strategy.py");

	[TestMethod]
	public Task JavoV1()
		=> RunStrategy("0429_Javo_V1/PY/javo_v1_strategy.py");

	[TestMethod]
	public Task MacdBbRsi()
		=> RunStrategy("0430_Macd_Bb_Rsi/PY/macd_bb_rsi_strategy.py");

	[TestMethod]
	public Task MacdDmi()
		=> RunStrategy("0432_Macd_Dmi/PY/macd_dmi_strategy.py");

	[TestMethod]
	public Task MacdLong()
		=> RunStrategy("0433_Macd_Long/PY/macd_long_strategy.py");

	[TestMethod]
	public Task MaCrossDmi()
		=> RunStrategy("0435_Ma_Cross_Dmi/PY/ma_cross_dmi_strategy.py");

	[TestMethod]
	public Task MemaBbRsi()
		=> RunStrategy("0436_Mema_Bb_Rsi/PY/mema_bb_rsi_strategy.py");

	[TestMethod]
	public Task MtfBb()
		=> RunStrategy("0437_Mtf_Bb/PY/mtf_bb_strategy.py");

	[TestMethod]
	public Task OmarMmr()
		=> RunStrategy("0438_Omar_Mmr/PY/omar_mmr_strategy.py");

	[TestMethod]
	public Task PinBarMagic()
		=> RunStrategy("0439_Pin_Bar_Magic/PY/pin_bar_magic_strategy.py");

	[TestMethod]
	public Task QqeSignals()
		=> RunStrategy("0440_Qqe_Signals/PY/qqe_signals_strategy.py");

	[TestMethod]
	public Task RsiPlus1200()
		=> RunStrategy("0441_Rsi_Plus_1200/PY/rsi_plus_1200_strategy.py");

	[TestMethod]
	public Task RsiEma()
		=> RunStrategy("0442_Rsi_Ema/PY/rsi_ema_strategy.py");

	[TestMethod]
	public Task StochRsiCrossover()
		=> RunStrategy("0443_Stoch_Rsi_Crossover/PY/stoch_rsi_crossover_strategy.py");

	[TestMethod]
	public Task StochRsiSupertrend()
		=> RunStrategy("0444_Stoch_Rsi_Supertrend/PY/stoch_rsi_supertrend_strategy.py");

	[TestMethod]
	public Task StrategyTester()
		=> RunStrategy("0445_Strategy_Tester/PY/strategy_tester_strategy.py");

	[TestMethod]
	public Task StratBase()
		=> RunStrategy("0446_Strat_Base/PY/strat_base_strategy.py");

	[TestMethod]
	public Task SupertrendEmaRebound()
		=> RunStrategy("0447_Supertrend_Ema_Rebound/PY/supertrend_ema_rebound_strategy.py");

	[TestMethod]
	public Task TendencyEmaRsi()
		=> RunStrategy("0450_Tendency_Ema_Rsi/PY/tendency_ema_rsi_strategy.py");

	[TestMethod]
	public Task ThreeEmaCross()
		=> RunStrategy("0451_Three_Ema_Cross/PY/three_ema_cross_strategy.py");

	[TestMethod]
	public Task TtmSqueeze()
		=> RunStrategy("0452_Ttm_Squeeze/PY/ttm_squeeze_strategy.py");

	[TestMethod]
	public Task VelaSuperada()
		=> RunStrategy("0453_Vela_Superada/PY/vela_superada_strategy.py");

	[TestMethod]
	public Task WilliamsVixFix()
		=> RunStrategy("0454_Williams_Vix_Fix/PY/williams_vix_fix_strategy.py");
}