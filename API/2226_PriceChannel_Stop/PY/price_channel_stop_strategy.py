import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class price_channel_stop_strategy(Strategy):
    def __init__(self):
        super(price_channel_stop_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 5) \
            .SetDisplay("Channel Period", "Period for Price Channel calculation", "Indicators")
        self._risk = self.Param("Risk", 0.10) \
            .SetDisplay("Risk", "Risk factor for stop levels", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Position Open", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Position Open", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Position Close", "Allow closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Position Close", "Allow closing short positions", "Trading")
        self._prev_bsmax = 0.0
        self._prev_bsmin = 0.0
        self._trend = 0
        self._is_first = True

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def risk(self):
        return self._risk.Value

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

    def OnReseted(self):
        super(price_channel_stop_strategy, self).OnReseted()
        self._prev_bsmax = 0.0
        self._prev_bsmin = 0.0
        self._trend = 0
        self._is_first = True

    def OnStarted(self, time):
        super(price_channel_stop_strategy, self).OnStarted(time)
        donchian = DonchianChannels()
        donchian.Length = self.channel_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        if not value.IsFormed:
            return
        upper = value.UpperBand
        lower = value.LowerBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)
        rng = upper - lower
        d_price = rng * float(self.risk)
        bsmax = upper - d_price
        bsmin = lower + d_price
        if self._is_first:
            self._prev_bsmax = bsmax
            self._prev_bsmin = bsmin
            self._is_first = False
            return
        trend = self._trend
        close = float(candle.ClosePrice)
        if close > self._prev_bsmax:
            trend = 1
        elif close < self._prev_bsmin:
            trend = -1
        if trend > 0 and bsmin < self._prev_bsmin:
            bsmin = self._prev_bsmin
        if trend < 0 and bsmax > self._prev_bsmax:
            bsmax = self._prev_bsmax
        is_buy = self._trend <= 0 and trend > 0
        is_sell = self._trend >= 0 and trend < 0
        if is_buy and self.Position <= 0:
            self.BuyMarket()
        elif is_sell and self.Position >= 0:
            self.SellMarket()
        self._trend = trend
        self._prev_bsmax = bsmax
        self._prev_bsmin = bsmin

    def CreateClone(self):
        return price_channel_stop_strategy()
