import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_candles_x_smoothed_strategy(Strategy):
    def __init__(self):
        super(exp_candles_x_smoothed_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Smoothing period", "Indicators")
        self._level = self.Param("Level", 30.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Level", "Breakout level in points", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Close", "Allow closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Close", "Allow closing short positions", "Trading")
        self._high_ma = None
        self._low_ma = None

    @property
    def ma_length(self):
        return self._ma_length.Value
    @property
    def level(self):
        return self._level.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value
    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value
    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value
    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    def OnStarted(self, time):
        super(exp_candles_x_smoothed_strategy, self).OnStarted(time)
        self._high_ma = WeightedMovingAverage()
        self._high_ma.Length = self.ma_length
        self._low_ma = WeightedMovingAverage()
        self._low_ma.Length = self.ma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._high_ma)
            self.DrawIndicator(area, self._low_ma)
            self.DrawOwnTrades(area)
        self.StartProtection(None, None)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high_val = self._high_ma.Process(candle.HighPrice, candle.OpenTime, True)
        low_val = self._low_ma.Process(candle.LowPrice, candle.OpenTime, True)
        if not high_val.IsFormed or not low_val.IsFormed:
            return
        step = self.Security.PriceStep if self.Security.PriceStep is not None else 1.0
        lvl = float(self.level) * float(step)
        smoothed_high = float(high_val)
        smoothed_low = float(low_val)
        close = float(candle.ClosePrice)
        break_up = close > smoothed_high + lvl
        break_down = close < smoothed_low - lvl
        if break_up:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
        elif break_down:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return exp_candles_x_smoothed_strategy()
