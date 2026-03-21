import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rsi_williams_r_strategy(Strategy):
    """
    RSI + Williams %R strategy.
    Buy when both RSI and Williams %R cross into oversold.
    Sell when both cross into overbought.
    """

    def __init__(self):
        super(rsi_williams_r_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI", "RSI Parameters")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI Parameters")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI Parameters")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R", "Williams %R Parameters")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0) \
            .SetRange(-100, 0) \
            .SetDisplay("Williams %R Oversold", "Williams %R oversold level", "Williams %R Parameters")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0) \
            .SetRange(-100, 0) \
            .SetDisplay("Williams %R Overbought", "Williams %R overbought level", "Williams %R Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 180) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._cooldown = 0
        self._prev_rsi = 0.0
        self._prev_williams = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(rsi_williams_r_strategy, self).OnStarted(time)
        self._cooldown = 0
        self._prev_rsi = 0.0
        self._prev_williams = 0.0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        williams_r = WilliamsR()
        williams_r.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, williams_r, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            osc_area = self.CreateChartArea()
            if osc_area is not None:
                self.DrawIndicator(osc_area, rsi)
                self.DrawIndicator(osc_area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_value)
        wv = float(williams_r_value)

        if self._prev_rsi == 0 and self._prev_williams == 0:
            self._prev_rsi = rv
            self._prev_williams = wv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rv
            self._prev_williams = wv
            return

        cd = self._cooldown_bars.Value
        rsi_os = self._rsi_oversold.Value
        rsi_ob = self._rsi_overbought.Value
        wr_os = self._williams_r_oversold.Value
        wr_ob = self._williams_r_overbought.Value

        oversold_cross = (self._prev_rsi >= rsi_os and rv < rsi_os
                          and self._prev_williams >= wr_os and wv < wr_os)
        overbought_cross = (self._prev_rsi <= rsi_ob and rv > rsi_ob
                            and self._prev_williams <= wr_ob and wv > wr_ob)

        if oversold_cross and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        elif overbought_cross and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd
        elif rv > 50 and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        elif rv < 50 and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_rsi = rv
        self._prev_williams = wv

    def OnReseted(self):
        super(rsi_williams_r_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_rsi = 0.0
        self._prev_williams = 0.0

    def CreateClone(self):
        return rsi_williams_r_strategy()
