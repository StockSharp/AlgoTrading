import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class macd_williams_r_strategy(Strategy):
    """
    MACD + Williams %R strategy.
    Enters on MACD crossover with Williams %R confirmation.
    """

    def __init__(self):
        super(macd_williams_r_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

        self._cooldown = 0
        self._has_prev_state = False
        self._prev_bull = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(macd_williams_r_strategy, self).OnStarted2(time)
        self._cooldown = 0
        self._has_prev_state = False
        self._prev_bull = False

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value

        williams_r = WilliamsR()
        williams_r.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, williams_r, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            wr_area = self.CreateChartArea()
            if wr_area is not None:
                self.DrawIndicator(wr_area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        williams_r = float(williams_r_value)
        bull = macd_line > signal_line

        if not self._has_prev_state:
            self._has_prev_state = True
            self._prev_bull = bull
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_bull = bull
            return

        crossed_up = not self._prev_bull and bull
        crossed_down = self._prev_bull and not bull
        cd = self._cooldown_bars.Value

        if crossed_up and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif crossed_down and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd
        elif macd_line < signal_line and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        elif macd_line > signal_line and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_bull = bull

    def OnReseted(self):
        super(macd_williams_r_strategy, self).OnReseted()
        self._cooldown = 0
        self._has_prev_state = False
        self._prev_bull = False

    def CreateClone(self):
        return macd_williams_r_strategy()
