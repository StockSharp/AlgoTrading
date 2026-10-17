import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class color_bears_gap_strategy(Strategy):
    """Same-bar smoothed high/open versus high/close gap, with independent side permissions."""

    def __init__(self):
        super(color_bears_gap_strategy, self).__init__()
        self._length1 = self.Param("Length1", 12).SetDisplay("Length 1", "First smoothing", "Indicator")
        self._length2 = self.Param("Length2", 5).SetDisplay("Length 2", "Second smoothing", "Indicator")
        self._buy_open = self.Param("BuyOpen", True).SetDisplay("Buy Open", "Allow long entries", "Permissions")
        self._buy_close = self.Param("BuyClose", True).SetDisplay("Buy Close", "Allow closing longs", "Permissions")
        self._sell_open = self.Param("SellOpen", True).SetDisplay("Sell Open", "Allow short entries", "Permissions")
        self._sell_close = self.Param("SellClose", True).SetDisplay("Sell Close", "Allow closing shorts", "Permissions")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))).SetDisplay(
            "Candle Type", "Timeframe", "General")
        self._reset_state()

    @property
    def length1(self): return self._length1.Value
    @property
    def length2(self): return self._length2.Value
    @property
    def candle_type(self): return self._candle_type.Value
    @property
    def buy_open(self): return self._buy_open.Value
    @property
    def buy_close(self): return self._buy_close.Value
    @property
    def sell_open(self): return self._sell_open.Value
    @property
    def sell_close(self): return self._sell_close.Value

    def _reset_state(self):
        self._ema_close = self._ema_open = None
        self._gap_close = self._gap_open = None
        self._previous_gap = None
        self._pending_order = None

    def OnReseted(self):
        super(color_bears_gap_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(color_bears_gap_strategy, self).OnStarted2(time)
        self._reset_state()
        self._ema_close = ExponentialMovingAverage()
        self._ema_close.Length = self.length1
        self._ema_open = ExponentialMovingAverage()
        self._ema_open.Length = self.length1
        self._gap_close = ExponentialMovingAverage()
        self._gap_close.Length = self.length2
        self._gap_open = ExponentialMovingAverage()
        self._gap_open.Length = self.length2
        for indicator in (self._ema_close, self._ema_open, self._gap_close, self._gap_open):
            self.Indicators.Add(indicator)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        time = candle.OpenTime
        close = process_float(self._ema_close, candle.ClosePrice, time, True)
        open_price = process_float(self._ema_open, candle.OpenPrice, time, True)
        gap_c = process_float(self._gap_close, float(candle.HighPrice) - float(close), time, True)
        gap_o = process_float(self._gap_open, float(candle.HighPrice) - float(open_price), time, True)
        if not (self._ema_close.IsFormed and self._ema_open.IsFormed and self._gap_close.IsFormed and self._gap_open.IsFormed):
            return
        diff = float(gap_o) - float(gap_c)
        previous = self._previous_gap
        self._previous_gap = diff
        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if previous > 0 and diff < 0:
            if self.Position < 0:
                if self.sell_close:
                    self._pending_order = self.BuyMarket(abs(self.Position) + (self.Volume if self.buy_open else 0))
            elif self.Position == 0 and self.buy_open:
                self._pending_order = self.BuyMarket(self.Volume)
        elif previous < 0 and diff > 0:
            if self.Position > 0:
                if self.buy_close:
                    self._pending_order = self.SellMarket(abs(self.Position) + (self.Volume if self.sell_open else 0))
            elif self.Position == 0 and self.sell_open:
                self._pending_order = self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_bears_gap_strategy()
