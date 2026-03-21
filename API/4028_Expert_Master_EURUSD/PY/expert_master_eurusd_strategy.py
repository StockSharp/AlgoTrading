import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal

class expert_master_eurusd_strategy(Strategy):
    def __init__(self):
        super(expert_master_eurusd_strategy, self).__init__()

        self._trailing_points = self.Param("TrailingPoints", 25) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._fixed_volume = self.Param("FixedVolume", 1.0) \
            .SetDisplay("Fixed Volume", "Fallback trade volume", "Risk")
        self._risk_percent = self.Param("RiskPercent", 0.01) \
            .SetDisplay("Risk Percent", "Portfolio percentage used to size positions", "Risk")
        self._macd_fast_period = self.Param("MacdFastPeriod", 5) \
            .SetDisplay("MACD Fast", "Fast EMA period", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 15) \
            .SetDisplay("MACD Slow", "Slow EMA period", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 3) \
            .SetDisplay("MACD Signal", "Signal EMA period", "Indicators")
        self._upper_macd_threshold = self.Param("UpperMacdThreshold", 10.0) \
            .SetDisplay("Upper MACD", "Positive MACD threshold", "Logic")
        self._lower_macd_threshold = self.Param("LowerMacdThreshold", -10.0) \
            .SetDisplay("Lower MACD", "Negative MACD threshold for longs", "Logic")
        self._short_current_threshold = self.Param("ShortCurrentThreshold", -20.0) \
            .SetDisplay("Short MACD", "Negative MACD threshold for shorts", "Logic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Candle type for MACD", "Data")

        self._macd_main0 = None
        self._macd_main1 = None
        self._macd_main2 = None
        self._macd_main3 = None
        self._signal0 = None
        self._signal1 = None
        self._signal2 = None
        self._signal3 = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._price_step = 1.0

    @property
    def TrailingPoints(self):
        return self._trailing_points.Value

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @property
    def UpperMacdThreshold(self):
        return self._upper_macd_threshold.Value

    @property
    def LowerMacdThreshold(self):
        return self._lower_macd_threshold.Value

    @property
    def ShortCurrentThreshold(self):
        return self._short_current_threshold.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(expert_master_eurusd_strategy, self).OnStarted(time)

        self._price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._price_step = ps

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFastPeriod
        self._macd.Macd.LongMa.Length = self.MacdSlowPeriod
        self._macd.SignalMa.Length = self.MacdSignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, indicator_value):
        if candle.State != CandleStates.Finished:
            return

        if not indicator_value.IsFinal:
            return

        macd_main = None
        macd_signal = None
        if hasattr(indicator_value, 'Macd') and hasattr(indicator_value, 'Signal'):
            macd_main = float(indicator_value.Macd)
            macd_signal = float(indicator_value.Signal)

        if macd_main is None or macd_signal is None:
            return

        # Shift buffer: oldest <- older <- previous <- current <- new
        self._macd_main3 = self._macd_main2
        self._macd_main2 = self._macd_main1
        self._macd_main1 = self._macd_main0
        self._macd_main0 = macd_main

        self._signal3 = self._signal2
        self._signal2 = self._signal1
        self._signal1 = self._signal0
        self._signal0 = macd_signal

        if self._manage_trailing(candle):
            return

        if (self._macd_main3 is None or self._macd_main2 is None or
                self._macd_main1 is None or self._macd_main0 is None or
                self._signal3 is None or self._signal2 is None or
                self._signal1 is None or self._signal0 is None):
            return

        mac1 = self._macd_main0
        mac2 = self._macd_main1
        mac3 = self._macd_main2
        mac4 = self._macd_main3
        sig1 = self._signal0
        sig2 = self._signal1
        sig3 = self._signal2
        sig4 = self._signal3

        upper_thresh = float(self.UpperMacdThreshold)
        lower_thresh = float(self.LowerMacdThreshold)
        short_thresh = float(self.ShortCurrentThreshold)

        long_signal = (sig4 > sig3 and sig3 > sig2 and sig2 < sig1 and
                       mac4 > mac3 and mac3 < mac2 and mac2 < mac1 and
                       mac2 < lower_thresh and mac4 < 0 and mac1 > upper_thresh)

        short_signal = (sig4 < sig3 and sig3 < sig2 and sig2 > sig1 and
                        mac4 < mac3 and mac3 > mac2 and mac2 > mac1 and
                        mac2 > upper_thresh and mac4 > 0 and mac1 < short_thresh)

        if self.Position == 0:
            if long_signal:
                volume = self._get_trade_volume()
                if volume > 0:
                    self.BuyMarket(volume)
                    self._long_entry_price = float(candle.ClosePrice)
                    self._long_trailing_stop = None
                    self._reset_short()
            elif short_signal:
                volume = self._get_trade_volume()
                if volume > 0:
                    self.SellMarket(volume)
                    self._short_entry_price = float(candle.ClosePrice)
                    self._short_trailing_stop = None
                    self._reset_long()
        elif self.Position > 0:
            if mac1 < mac2:
                self.SellMarket(abs(self.Position))
                self._reset_long()
        elif self.Position < 0:
            if mac1 > mac2:
                self.BuyMarket(abs(self.Position))
                self._reset_short()

    def _manage_trailing(self, candle):
        trailing_pts = int(self.TrailingPoints)
        if trailing_pts <= 0:
            return False

        trailing_distance = trailing_pts * self._price_step
        if trailing_distance <= 0:
            return False

        if self.Position > 0 and self._long_entry_price > 0:
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            close = float(candle.ClosePrice)

            if high >= self._long_entry_price + trailing_distance:
                new_stop = close - trailing_distance
                if self._long_trailing_stop is None or new_stop > self._long_trailing_stop:
                    self._long_trailing_stop = new_stop

            if self._long_trailing_stop is not None and low <= self._long_trailing_stop:
                self.SellMarket(abs(self.Position))
                self._reset_long()
                return True

        elif self.Position < 0 and self._short_entry_price > 0:
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            close = float(candle.ClosePrice)

            if low <= self._short_entry_price - trailing_distance:
                new_stop = close + trailing_distance
                if self._short_trailing_stop is None or new_stop < self._short_trailing_stop:
                    self._short_trailing_stop = new_stop

            if self._short_trailing_stop is not None and high >= self._short_trailing_stop:
                self.BuyMarket(abs(self.Position))
                self._reset_short()
                return True

        return False

    def _get_trade_volume(self):
        volume = float(self.FixedVolume)
        risk_pct = float(self.RiskPercent)

        if risk_pct > 0 and self.Portfolio is not None:
            cv = self.Portfolio.CurrentValue
            equity = float(cv) if cv is not None and float(cv) > 0 else 0.0
            if equity > 0:
                risk_volume = equity * (risk_pct / 100.0)
                volume = round(risk_volume, 1)

        if volume <= 0:
            volume = float(self.FixedVolume)

        return max(volume, 0.0)

    def _reset_long(self):
        self._long_entry_price = 0.0
        self._long_trailing_stop = None

    def _reset_short(self):
        self._short_entry_price = 0.0
        self._short_trailing_stop = None

    def OnReseted(self):
        super(expert_master_eurusd_strategy, self).OnReseted()
        self._macd_main0 = None
        self._macd_main1 = None
        self._macd_main2 = None
        self._macd_main3 = None
        self._signal0 = None
        self._signal1 = None
        self._signal2 = None
        self._signal3 = None
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_trailing_stop = None
        self._short_trailing_stop = None
        self._price_step = 1.0

    def CreateClone(self):
        return expert_master_eurusd_strategy()
