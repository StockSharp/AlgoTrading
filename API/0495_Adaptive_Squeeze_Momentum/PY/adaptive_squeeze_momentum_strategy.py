import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, KeltnerChannels, Momentum, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class adaptive_squeeze_momentum_strategy(Strategy):
    def __init__(self):
        super(adaptive_squeeze_momentum_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Periods for Bollinger Bands", "Indicators")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0) \
            .SetDisplay("Bollinger Multiplier", "Deviation multiplier", "Indicators")
        self._keltner_period = self.Param("KeltnerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner Period", "EMA period for Keltner Channels", "Indicators")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner Multiplier", "ATR multiplier for Keltner Channels", "Indicators")
        self._momentum_length = self.Param("MomentumLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Length", "Periods for momentum", "Indicators")
        self._trend_ma_length = self.Param("TrendMaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Trend EMA Length", "EMA period for trend filter", "Indicators")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSl", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Mult", "ATR multiplier for stop-loss", "Risk")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTp", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Take Mult", "ATR multiplier for take-profit", "Risk")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "Period for ATR", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._squeeze_off_prev = False
        self._stop_price = 0.0
        self._profit_target = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adaptive_squeeze_momentum_strategy, self).OnReseted()
        self._squeeze_off_prev = False
        self._stop_price = 0.0
        self._profit_target = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_squeeze_momentum_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self._bollinger_period.Value
        bb.Width = self._bollinger_multiplier.Value
        kc = KeltnerChannels()
        kc.Length = self._keltner_period.Value
        kc.Multiplier = self._keltner_multiplier.Value
        mom = Momentum()
        mom.Length = self._momentum_length.Value
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self._trend_ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, kc, mom, trend_ema, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, kc_value, mom_value, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if bb_value.IsEmpty or kc_value.IsEmpty or mom_value.IsEmpty or ema_value.IsEmpty or atr_value.IsEmpty:
            return
        bb = bb_value
        kc = kc_value
        bb_upper = bb.UpBand
        bb_lower = bb.LowBand
        kc_upper = kc.Upper
        kc_lower = kc.Lower
        if bb_upper is None or bb_lower is None or kc_upper is None or kc_lower is None:
            return
        mom_v = float(mom_value.GetValue[float]())
        trend = float(ema_value.GetValue[float]())
        atr_v = float(atr_value.GetValue[float]())
        close = float(candle.ClosePrice)
        squeeze_off = float(bb_lower) < float(kc_lower) and float(bb_upper) > float(kc_upper)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._check_stops(candle, atr_v)
            self._squeeze_off_prev = squeeze_off
            return
        if self._check_stops(candle, atr_v):
            self._squeeze_off_prev = squeeze_off
            return
        bullish_trend = close > trend
        bearish_trend = close < trend
        buy_signal = self._squeeze_off_prev and mom_v > 0 and bullish_trend
        sell_signal = self._squeeze_off_prev and mom_v < 0 and bearish_trend
        self._squeeze_off_prev = squeeze_off
        sl_mult = float(self._atr_multiplier_sl.Value)
        tp_mult = float(self._atr_multiplier_tp.Value)
        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._stop_price = close - atr_v * sl_mult
            self._profit_target = close + atr_v * tp_mult
            self._cooldown_remaining = self.cooldown_bars
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._stop_price = close + atr_v * sl_mult
            self._profit_target = close - atr_v * tp_mult
            self._cooldown_remaining = self.cooldown_bars

    def _check_stops(self, candle, atr_v):
        close = float(candle.ClosePrice)
        if self.Position > 0 and self._stop_price > 0:
            if close <= self._stop_price or close >= self._profit_target:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
                self._stop_price = 0.0
                return True
        elif self.Position < 0 and self._stop_price > 0:
            if close >= self._stop_price or close <= self._profit_target:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
                self._stop_price = 0.0
                return True
        return False

    def CreateClone(self):
        return adaptive_squeeze_momentum_strategy()
