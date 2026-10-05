import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (BollingerBands, AverageDirectionalIndex, SuperTrend,
                                        MovingAverageConvergenceDivergenceSignal, AverageTrueRange)
from StockSharp.Algo.Strategies import Strategy


class nifty_options_trendy_markets_with_tsl_strategy(Strategy):
    """
    Nifty options trendy markets with trailing stop strategy.
    A long opens when the close crosses above the upper Bollinger Band while ADX is above AdxEntryThreshold, volume spikes above
    VolumeSpikeMultiplier times its average and the close is above SuperTrend; a short mirrors this at the lower band.
    A position closes on an opposite MACD/signal cross, when ADX falls below AdxExitThreshold, or at an ATR trailing stop.
    """

    def __init__(self):
        super(nifty_options_trendy_markets_with_tsl_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Bollinger Bands period", "Bollinger")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Multiplier", "Bollinger Bands deviation multiplier", "Bollinger")
        self._adx_length = self.Param("AdxLength", 14).SetGreaterThanZero().SetDisplay("ADX Length", "ADX period", "ADX")
        self._adx_entry_threshold = self.Param("AdxEntryThreshold", 25.0).SetNotNegative().SetDisplay("ADX Entry", "Minimum ADX to enter", "ADX")
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20.0).SetNotNegative().SetDisplay("ADX Exit", "ADX level below which positions close", "ADX")
        self._super_trend_length = self.Param("SuperTrendLength", 10).SetGreaterThanZero().SetDisplay("SuperTrend Length", "SuperTrend ATR period", "SuperTrend")
        self._super_trend_multiplier = self.Param("SuperTrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "SuperTrend")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal period", "MACD")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period for the trailing stop", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Risk")
        self._volume_spike_multiplier = self.Param("VolumeSpikeMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Volume Spike", "Volume must exceed its average by this factor", "Volume")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None
        self._prev_macd = None
        self._prev_signal = None
        self._trail_stop = None

    def OnReseted(self):
        super(nifty_options_trendy_markets_with_tsl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(nifty_options_trendy_markets_with_tsl_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_multiplier.Value)
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        supertrend = SuperTrend()
        supertrend.Length = self._super_trend_length.Value
        supertrend.Multiplier = Decimal(self._super_trend_multiplier.Value)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, adx, supertrend, macd, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, bollinger_value, adx_value, supertrend_value, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        period = self._bollinger_period.Value

        # The volume average covers the BollingerPeriod candles before this one.
        avg_volume = sum(self._volumes, Decimal(0)) / Decimal(len(self._volumes)) if len(self._volumes) >= period else None
        self._volumes.append(candle.TotalVolume)
        while len(self._volumes) > period:
            self._volumes.pop(0)

        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower
        self._prev_close = close

        if not bollinger_value.IsFormed or bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        self._prev_upper = upper
        self._prev_lower = lower

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd_line
        self._prev_signal = signal_line

        if not adx_value.IsFormed or not supertrend_value.IsFormed or not macd_value.IsFormed or not atr_value.IsFormed:
            return

        adx = adx_value.MovingAverage
        if adx is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = atr_value.GetValue[Decimal](None)
        trail_distance = atr * Decimal(self._atr_multiplier.Value)
        has_prev_macd = prev_macd is not None and prev_signal is not None
        macd_cross_down = has_prev_macd and prev_macd >= prev_signal and macd_line < signal_line
        macd_cross_up = has_prev_macd and prev_macd <= prev_signal and macd_line > signal_line
        adx_weak = adx < Decimal(self._adx_exit_threshold.Value)

        if self.Position > 0:
            if (self._trail_stop is not None and candle.LowPrice <= self._trail_stop) or macd_cross_down or adx_weak:
                self.SellMarket(self.Position)
                self._trail_stop = None
                return
            candidate = close - trail_distance
            self._trail_stop = candidate if self._trail_stop is None else max(self._trail_stop, candidate)
        elif self.Position < 0:
            if (self._trail_stop is not None and candle.HighPrice >= self._trail_stop) or macd_cross_up or adx_weak:
                self.BuyMarket(-self.Position)
                self._trail_stop = None
                return
            candidate = close + trail_distance
            self._trail_stop = candidate if self._trail_stop is None else min(self._trail_stop, candidate)

        if prev_close is None or prev_upper is None or prev_lower is None or avg_volume is None:
            return

        st = supertrend_value.GetValue[Decimal](None)
        volume_spike = candle.TotalVolume > avg_volume * Decimal(self._volume_spike_multiplier.Value)
        strong_trend = adx > Decimal(self._adx_entry_threshold.Value)

        if prev_close <= prev_upper and close > upper and strong_trend and volume_spike and close > st and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._trail_stop = close - trail_distance
        elif prev_close >= prev_lower and close < lower and strong_trend and volume_spike and close < st and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._trail_stop = close + trail_distance

    def CreateClone(self):
        return nifty_options_trendy_markets_with_tsl_strategy()
