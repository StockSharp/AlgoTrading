import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zap_team_pro_v6_ema_strategy(Strategy):
    def __init__(self):
        super(zap_team_pro_v6_ema_strategy, self).__init__()
        self._ema21_length = self.Param("Ema21Length", 21) \
            .SetDisplay("EMA 21", "Fast EMA length", "Indicators")
        self._ema50_length = self.Param("Ema50Length", 50) \
            .SetDisplay("EMA 50", "Slow EMA length", "Indicators")
        self._ema200_length = self.Param("Ema200Length", 200) \
            .SetDisplay("EMA 200", "Trend EMA length", "Indicators")
        self._enable_shorts = self.Param("EnableShorts", False) \
            .SetDisplay("Enable Shorts", "Allow short trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev21 = None
        self._prev50 = None

    @property
    def ema21_length(self):
        return self._ema21_length.Value

    @property
    def ema50_length(self):
        return self._ema50_length.Value

    @property
    def ema200_length(self):
        return self._ema200_length.Value

    @property
    def enable_shorts(self):
        return self._enable_shorts.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zap_team_pro_v6_ema_strategy, self).OnReseted()
        self._prev21 = None
        self._prev50 = None

    def OnStarted(self, time):
        super(zap_team_pro_v6_ema_strategy, self).OnStarted(time)
        ema21 = ExponentialMovingAverage()
        ema21.Length = self.ema21_length
        ema50 = ExponentialMovingAverage()
        ema50.Length = self.ema50_length
        ema200 = ExponentialMovingAverage()
        ema200.Length = self.ema200_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema21, ema50, ema200, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema21, ema50, ema200):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._prev21 is None:
            self._prev21 = ema21
            self._prev50 = ema50
            return
        cross_up = self._prev21 <= self._prev50 and ema21 > ema50
        cross_down = self._prev21 >= self._prev50 and ema21 < ema50
        trend_long = candle.ClosePrice > ema200
        trend_short = candle.ClosePrice < ema200
        if cross_up and trend_long and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and trend_short and self.enable_shorts and self.Position >= 0:
            self.SellMarket()
        self._prev21 = ema21
        self._prev50 = ema50

    def CreateClone(self):
        return zap_team_pro_v6_ema_strategy()
