import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class b_bands_stop_strategy(Strategy):
    def __init__(self):
        super(b_bands_stop_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._length = self.Param("Length", 20) \
            .SetDisplay("Length", "Bollinger period", "Indicator")
        self._deviation = self.Param("Deviation", 1.5) \
            .SetDisplay("Deviation", "Bollinger deviation", "Indicator")
        self._money_risk = self.Param("MoneyRisk", 1.0) \
            .SetDisplay("Money Risk", "Offset factor", "Indicator")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Allow to enter long", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Allow to enter short", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Close", "Allow to exit long", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Close", "Allow to exit short", "Trading")
        self._trend = 0
        self._smax1 = 0.0
        self._smin1 = 0.0
        self._bsmax1 = 0.0
        self._bsmin1 = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def length(self):
        return self._length.Value

    @property
    def deviation(self):
        return self._deviation.Value

    @property
    def money_risk(self):
        return self._money_risk.Value

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
        super(b_bands_stop_strategy, self).OnReseted()
        self._trend = 0
        self._smax1 = 0.0
        self._smin1 = 0.0
        self._bsmax1 = 0.0
        self._bsmin1 = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(b_bands_stop_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self.length
        bb.Width = self.deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_process(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        upper = float(value.UpBand)
        lower = float(value.LowBand)
        middle = float(value.MovingAverage)
        if upper == 0 or lower == 0:
            return
        m_risk = 0.5 * (self.money_risk - 1.0)
        smax0 = upper
        smin0 = lower
        if not self._initialized:
            self._initialized = True
            self._smax1 = smax0
            self._smin1 = smin0
            first_offset = m_risk * (smax0 - smin0)
            self._bsmax1 = smax0 + first_offset
            self._bsmin1 = smin0 - first_offset
            return
        prev_trend = self._trend
        close = float(candle.ClosePrice)
        if close > self._smax1:
            self._trend = 1
        elif close < self._smin1:
            self._trend = -1
        if self._trend > 0 and smin0 < self._smin1:
            smin0 = self._smin1
        elif self._trend < 0 and smax0 > self._smax1:
            smax0 = self._smax1
        dsize = m_risk * (smax0 - smin0)
        bsmax0 = smax0 + dsize
        bsmin0 = smin0 - dsize
        if self._trend > 0 and bsmin0 < self._bsmin1:
            bsmin0 = self._bsmin1
        elif self._trend < 0 and bsmax0 > self._bsmax1:
            bsmax0 = self._bsmax1
        if self._trend > 0 and prev_trend <= 0:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
        elif self._trend < 0 and prev_trend >= 0:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()
        self._smax1 = smax0
        self._smin1 = smin0
        self._bsmax1 = bsmax0
        self._bsmin1 = bsmin0

    def CreateClone(self):
        return b_bands_stop_strategy()
