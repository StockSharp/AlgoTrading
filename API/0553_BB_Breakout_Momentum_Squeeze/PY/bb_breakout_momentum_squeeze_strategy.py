import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, KeltnerChannels, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bb_breakout_momentum_squeeze_strategy(Strategy):
    def __init__(self):
        super(bb_breakout_momentum_squeeze_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Breakout Length", "Length for Bollinger breakout", "BB Breakout")
        self._bb_mult = self.Param("BbMultiplier", 1.0) \
            .SetDisplay("BB Breakout Mult", "Bollinger breakout multiplier", "BB Breakout")
        self._squeeze_length = self.Param("SqueezeLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Squeeze Length", "Length for squeeze calculation", "Squeeze")
        self._squeeze_bb_mult = self.Param("SqueezeBbMultiplier", 2.0) \
            .SetDisplay("Bollinger Mult", "Bollinger Band std multiplier for squeeze", "Squeeze")
        self._kc_mult = self.Param("KcMultiplier", 2.0) \
            .SetDisplay("Keltner Mult", "Keltner Channel multiplier", "Squeeze")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
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
        super(bb_breakout_momentum_squeeze_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bb_breakout_momentum_squeeze_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self._bb_length.Value
        bb.Width = self._bb_mult.Value
        squeeze_bb = BollingerBands()
        squeeze_bb.Length = self._squeeze_length.Value
        squeeze_bb.Width = self._squeeze_bb_mult.Value
        kc = KeltnerChannels()
        kc.Length = self._squeeze_length.Value
        kc.Multiplier = self._kc_mult.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, squeeze_bb, kc, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, squeeze_bb_value, kc_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bb_value
        bb_upper = bb.UpBand
        bb_lower = bb.LowBand
        if bb_upper is None or bb_lower is None:
            return
        sq = squeeze_bb_value
        sq_upper = sq.UpBand
        sq_lower = sq.LowBand
        if sq_upper is None or sq_lower is None:
            return
        kc_typed = kc_value
        kc_upper = kc_typed.Upper
        kc_lower = kc_typed.Lower
        if kc_upper is None or kc_lower is None:
            return
        close = float(candle.ClosePrice)
        upper_v = float(bb_upper)
        lower_v = float(bb_lower)
        squeeze_off = float(sq_lower) < float(kc_lower) or float(sq_upper) > float(kc_upper)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        if close > upper_v and squeeze_off and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif close < lower_v and squeeze_off and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return bb_breakout_momentum_squeeze_strategy()
