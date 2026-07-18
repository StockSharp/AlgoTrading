import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vwap_williams_r_strategy(Strategy):
    """
    Strategy based on VWAP and Williams %R indicators.
    Long when price below VWAP and Williams %R crosses into oversold.
    Short when price above VWAP and Williams %R crosses into overbought.
    """

    def __init__(self):
        super(vwap_williams_r_strategy, self).__init__()

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetRange(5, 50) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 60) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_williams_r = 0.0
        self._cooldown = 0
        self._vwap_date = None
        self._vwap_cum_pv = 0.0
        self._vwap_cum_vol = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_williams_r_strategy, self).OnReseted()
        self._previous_williams_r = 0.0
        self._cooldown = 0
        self._vwap_date = None
        self._vwap_cum_pv = 0.0
        self._vwap_cum_vol = 0.0

    def OnStarted2(self, time):
        super(vwap_williams_r_strategy, self).OnStarted2(time)
        self._previous_williams_r = 0.0
        self._cooldown = 0
        self._vwap_date = None
        self._vwap_cum_pv = 0.0
        self._vwap_cum_vol = 0.0

        williams_r = WilliamsR()
        williams_r.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(williams_r, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        date = candle.ServerTime.Date
        if self._vwap_date is None or self._vwap_date != date:
            self._vwap_date = date
            self._vwap_cum_pv = 0.0
            self._vwap_cum_vol = 0.0

        self._vwap_cum_pv += float(candle.ClosePrice) * float(candle.TotalVolume)
        self._vwap_cum_vol += float(candle.TotalVolume)
        if self._vwap_cum_vol <= 0:
            return

        vwap_value = self._vwap_cum_pv / self._vwap_cum_vol

        previous_wr = self._previous_williams_r
        self._previous_williams_r = float(williams_r_value)
        wr = float(williams_r_value)

        price = float(candle.ClosePrice)
        crossed_into_oversold = previous_wr > -80 and wr <= -80
        crossed_into_overbought = previous_wr < -20 and wr >= -20

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and price < vwap_value * 0.999 and crossed_into_oversold and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and price > vwap_value * 1.001 and crossed_into_overbought and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val

    def CreateClone(self):
        return vwap_williams_r_strategy()
