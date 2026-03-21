import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bb_rsi_strategy(Strategy):
    def __init__(self):
        super(bb_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_deviation = self.Param("BbDeviation", 1.5) \
            .SetDisplay("BB Deviation", "Bollinger Bands deviation", "Bollinger Bands")
        self._rsi_period = self.Param("RsiPeriod", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI calculation period", "RSI")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 48.0) \
            .SetDisplay("RSI Buy Level", "RSI threshold to enter long", "RSI")
        self._rsi_exit_level = self.Param("RsiExitLevel", 52.0) \
            .SetDisplay("RSI Exit Level", "RSI threshold to exit long", "RSI")
        self._trailing_step = self.Param("TrailingStep", 2.0) \
            .SetDisplay("Trailing Step %", "Trailing stop step percent", "Risk")
        self._in_trade = False
        self._peak_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bb_rsi_strategy, self).OnReseted()
        self._in_trade = False
        self._peak_price = 0.0

    def OnStarted(self, time):
        super(bb_rsi_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_deviation.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bb_value
        upper = bb.UpBand
        lower = bb.LowBand
        if upper is None or lower is None:
            return
        if not rsi_value.IsFormed:
            return
        upper_v = float(upper)
        lower_v = float(lower)
        rsi_v = float(rsi_value)
        close = float(candle.ClosePrice)
        buy_level = float(self._rsi_buy_level.Value)
        exit_level = float(self._rsi_exit_level.Value)
        trail_step = float(self._trailing_step.Value)
        if self.Position == 0:
            if close < lower_v and rsi_v < buy_level:
                self.BuyMarket()
                self._peak_price = close
                self._in_trade = True
            elif close > upper_v and rsi_v > exit_level:
                self.SellMarket()
                self._peak_price = close
                self._in_trade = True
        elif self.Position > 0 and self._in_trade:
            if close > self._peak_price:
                self._peak_price = close
            trailing_drop = self._peak_price * (1.0 - trail_step / 100.0)
            if close <= trailing_drop or rsi_v > exit_level:
                self.SellMarket()
                self._in_trade = False
        elif self.Position < 0 and self._in_trade:
            if close < self._peak_price:
                self._peak_price = close
            trailing_rise = self._peak_price * (1.0 + trail_step / 100.0)
            if close >= trailing_rise or rsi_v < buy_level:
                self.BuyMarket()
                self._in_trade = False

    def CreateClone(self):
        return bb_rsi_strategy()
