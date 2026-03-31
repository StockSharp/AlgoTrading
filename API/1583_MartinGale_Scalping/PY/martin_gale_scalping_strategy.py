import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class martin_gale_scalping_strategy(Strategy):
    def __init__(self):
        super(martin_gale_scalping_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast SMA Length", "Length for fast SMA", "General")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow SMA Length", "Length for slow SMA", "General")
        self._take_profit = self.Param("TakeProfit", 1.03) \
            .SetDisplay("Take Profit Mult", "Take profit multiplier", "Risk")
        self._stop_loss = self.Param("StopLoss", 0.95) \
            .SetDisplay("Stop Loss Mult", "Stop loss multiplier", "Risk")
        self._trade_direction = self.Param("TradeDirection", "Long") \
            .SetDisplay("Trade Direction", "Trade direction", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._max_pyramids = self.Param("MaxPyramids", 2) \
            .SetDisplay("Max Pyramids", "Maximum pyramid entries", "General")
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_slow = 0.0
        self._pyramids = 0

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def trade_direction(self):
        return self._trade_direction.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def max_pyramids(self):
        return self._max_pyramids.Value

    def OnReseted(self):
        super(martin_gale_scalping_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_slow = 0.0
        self._pyramids = 0

    def OnStarted2(self, time):
        super(martin_gale_scalping_strategy, self).OnStarted2(time)
        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.fast_length
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.slow_length
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_slow = 0.0
        self._pyramids = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sma, slow_sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        crossover = fast > slow
        crossunder = fast < slow
        if self.Position == 0:
            self._pyramids = 0
            if crossover and self._allow_long():
                self._enter_long(candle, slow)
            elif crossunder and self._allow_short():
                self._enter_short(candle, slow)
        elif self.Position > 0:
            if (candle.ClosePrice > self._take_price or candle.ClosePrice < self._stop_price) and crossunder:
                self.SellMarket()
                self._reset_levels()
            elif crossover and self._allow_long() and self._pyramids < self.max_pyramids:
                self.BuyMarket()
                self._pyramids += 1
                self._update_levels(candle, slow)
        elif self.Position < 0:
            if (candle.ClosePrice > self._take_price or candle.ClosePrice < self._stop_price) and crossover:
                self.BuyMarket()
                self._reset_levels()
            elif crossunder and self._allow_short() and self._pyramids < self.max_pyramids:
                self.SellMarket()
                self._pyramids += 1
                self._update_levels(candle, slow)
        self._prev_slow = slow

    def _enter_long(self, candle, slow):
        self.BuyMarket()
        self._pyramids = 1
        self._update_levels(candle, slow)

    def _enter_short(self, candle, slow):
        self.SellMarket()
        self._pyramids = 1
        self._update_levels(candle, slow)

    def _update_levels(self, candle, slow):
        if self._prev_slow == 0:
            return
        if self.Position > 0:
            self._stop_price = float(candle.ClosePrice) - self.stop_loss * self._prev_slow
            self._take_price = float(candle.ClosePrice) + self.take_profit * self._prev_slow
        else:
            self._stop_price = float(candle.ClosePrice) + self.stop_loss * self._prev_slow
            self._take_price = float(candle.ClosePrice) - self.take_profit * self._prev_slow

    def _reset_levels(self):
        self._stop_price = 0.0
        self._take_price = 0.0

    def _allow_long(self):
        return self.trade_direction != "Short"

    def _allow_short(self):
        return self.trade_direction != "Long"

    def CreateClone(self):
        return martin_gale_scalping_strategy()
