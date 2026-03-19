import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class ichimoku_kumo_breakout_strategy(Strategy):
    """
    Ichimoku Kumo breakout. Tenkan/Kijun cross with cloud confirmation.
    """

    def __init__(self):
        super(ichimoku_kumo_breakout_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetDisplay("Tenkan Period", "Tenkan-sen period", "Indicators")
        self._kijun_period = self.Param("KijunPeriod", 26).SetDisplay("Kijun Period", "Kijun-sen period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_tenkan_above = False
        self._has_prev = False
        self._candles_since_trade = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_kumo_breakout_strategy, self).OnReseted()
        self._prev_tenkan_above = False
        self._has_prev = False
        self._candles_since_trade = 0

    def OnStarted(self, time):
        super(ichimoku_kumo_breakout_strategy, self).OnStarted(time)
        tenkan_h = Highest()
        tenkan_h.Length = self._tenkan_period.Value
        tenkan_l = Lowest()
        tenkan_l.Length = self._tenkan_period.Value
        kijun_h = Highest()
        kijun_h.Length = self._kijun_period.Value
        kijun_l = Lowest()
        kijun_l.Length = self._kijun_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(tenkan_h, tenkan_l, kijun_h, kijun_l, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, th, tl, kh, kl):
        if candle.State != CandleStates.Finished:
            return
        tenkan = (float(th) + float(tl)) / 2.0
        kijun = (float(kh) + float(kl)) / 2.0
        if tenkan == 0 or kijun == 0:
            return
        tenkan_above = tenkan > kijun
        self._candles_since_trade += 1
        if not self._has_prev:
            self._has_prev = True
            self._prev_tenkan_above = tenkan_above
            return
        is_cross = tenkan_above != self._prev_tenkan_above
        self._prev_tenkan_above = tenkan_above
        if not is_cross:
            return
        if self._candles_since_trade < 4:
            return
        if tenkan_above and self.Position <= 0:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif not tenkan_above and self.Position >= 0:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return ichimoku_kumo_breakout_strategy()
