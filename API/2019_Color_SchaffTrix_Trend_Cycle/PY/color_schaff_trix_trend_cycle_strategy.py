import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_schaff_trix_trend_cycle_strategy(Strategy):

    def __init__(self):
        super(color_schaff_trix_trend_cycle_strategy, self).__init__()

        self._fast_trix_length = self.Param("FastTrixLength", 5) \
            .SetDisplay("Fast TRIX", "Fast TRIX length", "Indicator")
        self._slow_trix_length = self.Param("SlowTrixLength", 12) \
            .SetDisplay("Slow TRIX", "Slow TRIX length", "Indicator")
        self._cycle = self.Param("Cycle", 10) \
            .SetDisplay("Cycle", "Cycle length", "Indicator")
        self._high_level = self.Param("HighLevel", 20) \
            .SetDisplay("High Level", "Upper threshold", "Indicator")
        self._low_level = self.Param("LowLevel", -20) \
            .SetDisplay("Low Level", "Lower threshold", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._stop_loss = self.Param("StopLoss", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit = self.Param("TakeProfit", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Buy Open", "Allow buy entries", "Trading")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Sell Open", "Allow sell entries", "Trading")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Buy Close", "Allow closing long", "Trading")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Sell Close", "Allow closing short", "Trading")
        self._factor = self.Param("Factor", 0.5) \
            .SetDisplay("Factor", "Smoothing factor for STC calculations", "Indicator")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetDisplay("Signal Cooldown", "Bars to wait between reversals", "Trading")

        self._prev_stc_val = None
        self._cooldown_remaining = 0
        # TRIX internals (fast)
        self._fast_ema1 = None
        self._fast_ema2 = None
        self._fast_ema3 = None
        self._fast_prev = None
        # TRIX internals (slow)
        self._slow_ema1 = None
        self._slow_ema2 = None
        self._slow_ema3 = None
        self._slow_prev = None
        # STC internals
        self._macd_high = None
        self._macd_low = None
        self._st_high = None
        self._st_low = None
        self._st_prev = 0.0
        self._stc_prev = 0.0
        self._st_pass = False
        self._stc_pass = False

    @property
    def FastTrixLength(self):
        return self._fast_trix_length.Value

    @FastTrixLength.setter
    def FastTrixLength(self, value):
        self._fast_trix_length.Value = value

    @property
    def SlowTrixLength(self):
        return self._slow_trix_length.Value

    @SlowTrixLength.setter
    def SlowTrixLength(self, value):
        self._slow_trix_length.Value = value

    @property
    def Cycle(self):
        return self._cycle.Value

    @Cycle.setter
    def Cycle(self, value):
        self._cycle.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    @property
    def Factor(self):
        return self._factor.Value

    @Factor.setter
    def Factor(self, value):
        self._factor.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def OnStarted(self, time):
        super(color_schaff_trix_trend_cycle_strategy, self).OnStarted(time)

        fast_len = self.FastTrixLength
        slow_len = self.SlowTrixLength
        cyc = self.Cycle

        self._fast_ema1 = ExponentialMovingAverage()
        self._fast_ema1.Length = fast_len
        self._fast_ema2 = ExponentialMovingAverage()
        self._fast_ema2.Length = fast_len
        self._fast_ema3 = ExponentialMovingAverage()
        self._fast_ema3.Length = fast_len
        self._fast_prev = None

        self._slow_ema1 = ExponentialMovingAverage()
        self._slow_ema1.Length = slow_len
        self._slow_ema2 = ExponentialMovingAverage()
        self._slow_ema2.Length = slow_len
        self._slow_ema3 = ExponentialMovingAverage()
        self._slow_ema3.Length = slow_len
        self._slow_prev = None

        self._macd_high = Highest()
        self._macd_high.Length = cyc
        self._macd_low = Lowest()
        self._macd_low.Length = cyc
        self._st_high = Highest()
        self._st_high.Length = cyc
        self._st_low = Lowest()
        self._st_low.Length = cyc
        self._st_prev = 0.0
        self._stc_prev = 0.0
        self._st_pass = False
        self._stc_pass = False
        self._prev_stc_val = None
        self._cooldown_remaining = 0

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Percent)
        )

    def _make_input(self, indicator, value, time):
        div = DecimalIndicatorValue(indicator, value, time)
        div.IsFinal = True
        return div

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        close = float(candle.ClosePrice)
        t = candle.OpenTime
        factor = float(self.Factor)

        # Calculate fast TRIX
        inp = self._make_input(self._fast_ema1, close, t)
        e1 = self._fast_ema1.Process(inp)
        e2 = self._fast_ema2.Process(e1)
        e3 = self._fast_ema3.Process(e2)
        fast_val = float(e3)
        fast_trix = 0.0
        if self._fast_prev is not None and self._fast_prev != 0:
            fast_trix = (fast_val - self._fast_prev) / self._fast_prev
        self._fast_prev = fast_val

        # Calculate slow TRIX
        inp2 = self._make_input(self._slow_ema1, close, t)
        s1 = self._slow_ema1.Process(inp2)
        s2 = self._slow_ema2.Process(s1)
        s3 = self._slow_ema3.Process(s2)
        slow_val = float(s3)
        slow_trix = 0.0
        if self._slow_prev is not None and self._slow_prev != 0:
            slow_trix = (slow_val - self._slow_prev) / self._slow_prev
        self._slow_prev = slow_val

        macd = fast_trix - slow_trix

        # STC calculation
        mh_result = self._macd_high.Process(self._make_input(self._macd_high, macd, t))
        ml_result = self._macd_low.Process(self._make_input(self._macd_low, macd, t))
        macd_high = float(mh_result)
        macd_low = float(ml_result)

        if macd_high - macd_low != 0:
            st = (macd - macd_low) / (macd_high - macd_low) * 100.0
        else:
            st = self._st_prev

        if self._st_pass:
            st = factor * (st - self._st_prev) + self._st_prev
        self._st_prev = st
        self._st_pass = True

        sh_result = self._st_high.Process(self._make_input(self._st_high, st, t))
        sl_result = self._st_low.Process(self._make_input(self._st_low, st, t))
        st_high = float(sh_result)
        st_low = float(sl_result)

        if st_high - st_low != 0:
            stc = (st - st_low) / (st_high - st_low) * 200.0 - 100.0
        else:
            stc = self._stc_prev

        if self._stc_pass:
            stc = factor * (stc - self._stc_prev) + self._stc_prev
        self._stc_prev = stc
        self._stc_pass = True

        if self._prev_stc_val is None:
            self._prev_stc_val = stc
            return

        prev = self._prev_stc_val
        high = float(self.HighLevel)
        low = float(self.LowLevel)

        crossed_up = stc > high and prev <= high
        crossed_down = stc < low and prev >= low
        long_exit = self.BuyClose and self.Position > 0 and stc < 0
        short_exit = self.SellClose and self.Position < 0 and stc > 0

        if long_exit:
            self.SellMarket(self.Position)
            self._cooldown_remaining = self.SignalCooldownBars
        elif short_exit:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and crossed_up and self.BuyOpen and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and crossed_down and self.SellOpen and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

        self._prev_stc_val = stc

    def OnReseted(self):
        super(color_schaff_trix_trend_cycle_strategy, self).OnReseted()
        self._prev_stc_val = None
        self._cooldown_remaining = 0

    def CreateClone(self):
        return color_schaff_trix_trend_cycle_strategy()
