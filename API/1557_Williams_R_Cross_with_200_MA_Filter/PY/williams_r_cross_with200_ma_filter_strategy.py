import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class williams_r_cross_with200_ma_filter_strategy(Strategy):
    def __init__(self):
        super(williams_r_cross_with200_ma_filter_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._ma_length = self.Param("MaLength", 50) \
            .SetDisplay("MA Length", "SMA filter period", "General")
        self._oversold = self.Param("Oversold", 30) \
            .SetDisplay("Oversold", "Oversold level", "General")
        self._overbought = self.Param("Overbought", 70) \
            .SetDisplay("Overbought", "Overbought level", "General")
        self._stop_pct = self.Param("StopPct", 0.5) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp_pct = self.Param("TpPct", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("TP %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def tp_pct(self):
        return self._tp_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_r_cross_with200_ma_filter_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0

    def OnStarted2(self, time):
        super(williams_r_cross_with200_ma_filter_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        sma = SimpleMovingAverage()
        sma.Length = self.ma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        # TP/SL management
        if self.Position > 0 and self._entry_price > 0 and self._stop_dist > 0:
            if candle.ClosePrice <= self._entry_price - self._stop_dist or candle.ClosePrice >= self._entry_price + self._stop_dist * (self.tp_pct / self.stop_pct):
                self.SellMarket()
                self._entry_price = 0
                self._stop_dist = 0
        elif self.Position < 0 and self._entry_price > 0 and self._stop_dist > 0:
            if candle.ClosePrice >= self._entry_price + self._stop_dist or candle.ClosePrice <= self._entry_price - self._stop_dist * (self.tp_pct / self.stop_pct):
                self.BuyMarket()
                self._entry_price = 0
                self._stop_dist = 0
        if self._prev_rsi == 0:
            self._prev_rsi = rsi_val
            return
        # Crossover signals with MA filter
        enter_long = self._prev_rsi < self.oversold and rsi_val >= self.oversold and candle.ClosePrice > sma_val
        enter_short = self._prev_rsi > self.overbought and rsi_val <= self.overbought and candle.ClosePrice < sma_val
        if enter_long and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._stop_dist = candle.ClosePrice * self.stop_pct / 100
        elif enter_short and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._stop_dist = candle.ClosePrice * self.stop_pct / 100
        self._prev_rsi = rsi_val

    def CreateClone(self):
        return williams_r_cross_with200_ma_filter_strategy()
