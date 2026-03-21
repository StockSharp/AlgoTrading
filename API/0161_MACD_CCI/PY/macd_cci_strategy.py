import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class macd_cci_strategy(Strategy):
    """
    MACD + CCI strategy.
    Buy when MACD crosses above Signal and CCI is oversold.
    Sell when MACD crosses below Signal and CCI is overbought.
    """

    def __init__(self):
        super(macd_cci_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD Parameters")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD Parameters")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "MACD Parameters")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")
        self._cci_oversold = self.Param("CciOversold", -100.0) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")
        self._cci_overbought = self.Param("CciOverbought", 100.0) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 320) \
            .SetRange(5, 1000) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._cooldown = 0
        self._has_prev_macd_state = False
        self._prev_macd_above_signal = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(macd_cci_strategy, self).OnStarted(time)
        self._cooldown = 0
        self._has_prev_macd_state = False
        self._prev_macd_above_signal = False

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, cci, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value, cci_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFormed or not cci_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        cci_dec = float(cci_value)
        is_macd_above_signal = macd_line > signal_line

        if not self._has_prev_macd_state:
            self._has_prev_macd_state = True
            self._prev_macd_above_signal = is_macd_above_signal
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_macd_above_signal = is_macd_above_signal
            return

        crossed_up = not self._prev_macd_above_signal and is_macd_above_signal
        crossed_down = self._prev_macd_above_signal and not is_macd_above_signal
        cd = self._cooldown_bars.Value

        if crossed_up and cci_dec < self._cci_oversold.Value and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif crossed_down and cci_dec > self._cci_overbought.Value and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd
        elif crossed_down and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        elif crossed_up and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_macd_above_signal = is_macd_above_signal

    def OnReseted(self):
        super(macd_cci_strategy, self).OnReseted()
        self._cooldown = 0
        self._has_prev_macd_state = False
        self._prev_macd_above_signal = False

    def CreateClone(self):
        return macd_cci_strategy()
