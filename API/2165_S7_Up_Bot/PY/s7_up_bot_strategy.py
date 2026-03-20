import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class s7_up_bot_strategy(Strategy):
    def __init__(self):
        super(s7_up_bot_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Absolute take profit", "Risk")
        self._stop_loss = self.Param("StopLoss", 300.0) \
            .SetDisplay("Stop Loss", "Absolute stop loss", "Risk")
        self._hl_divergence = self.Param("HlDivergence", 100.0) \
            .SetDisplay("HL Divergence", "Max difference between highs or lows", "General")
        self._span_price = self.Param("SpanPrice", 50.0) \
            .SetDisplay("Span Price", "Distance from extreme to price", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")
        self._prev_low = 0.0
        self._prev_high = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._is_long = False
        self._in_position = False

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def hl_divergence(self):
        return self._hl_divergence.Value

    @property
    def span_price(self):
        return self._span_price.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(s7_up_bot_strategy, self).OnReseted()
        self._prev_low = 0.0
        self._prev_high = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._is_long = False
        self._in_position = False

    def OnStarted(self, time):
        super(s7_up_bot_strategy, self).OnStarted(time)
        self._prev_low = 0.0
        self._prev_high = 0.0
        self._in_position = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._in_position:
            self._manage_position(candle, price)

        if not self._in_position and self._prev_low != 0.0 and self._prev_high != 0.0:
            self._check_entry(candle, price, high, low)

        self._prev_low = low
        self._prev_high = high

    def _check_entry(self, candle, price, high, low):
        hl_div = float(self.hl_divergence)
        span = float(self.span_price)
        tp = float(self.take_profit)
        sl = float(self.stop_loss)

        # Double bottom
        if abs(low - self._prev_low) < hl_div and price - low > span:
            if self.Position <= 0:
                self.BuyMarket()
            self._in_position = True
            self._is_long = True
            self._entry_price = price
            self._stop_price = price - sl
            self._take_profit_price = price + tp
        # Double top
        elif abs(high - self._prev_high) < hl_div and high - price > span:
            if self.Position >= 0:
                self.SellMarket()
            self._in_position = True
            self._is_long = False
            self._entry_price = price
            self._stop_price = price + sl
            self._take_profit_price = price - tp

    def _manage_position(self, candle, price):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._is_long:
            if high >= self._take_profit_price or low <= self._stop_price:
                self.SellMarket()
                self._in_position = False
        else:
            if low <= self._take_profit_price or high >= self._stop_price:
                self.BuyMarket()
                self._in_position = False

    def CreateClone(self):
        return s7_up_bot_strategy()
