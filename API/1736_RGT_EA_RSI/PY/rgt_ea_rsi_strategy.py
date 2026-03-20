import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rgt_ea_rsi_strategy(Strategy):
    def __init__(self):
        super(rgt_ea_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 8) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicator")
        self._rsi_high = self.Param("RsiHigh", 55) \
            .SetDisplay("RSI High", "Overbought threshold", "Indicator")
        self._rsi_low = self.Param("RsiLow", 45) \
            .SetDisplay("RSI Low", "Oversold threshold", "Indicator")
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss size in price units", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 300.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._min_profit = self.Param("MinProfit", 200.0) \
            .SetDisplay("Min Profit", "Minimum profit before trailing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._rsi_value = 0.0
        self._has_rsi = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_high(self):
        return self._rsi_high.Value

    @property
    def rsi_low(self):
        return self._rsi_low.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def min_profit(self):
        return self._min_profit.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rgt_ea_rsi_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._rsi_value = 0.0
        self._has_rsi = False

    def OnStarted(self, time):
        super(rgt_ea_rsi_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        bb = BollingerBands()
        bb.Length = 20
        bb.Width = 2.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.on_rsi).Start()
        subscription.BindEx(bb, self.on_bb).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_rsi(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        self._rsi_value = float(rsi_val)
        self._has_rsi = True

    def on_bb(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_rsi:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        if upper == 0 or lower == 0:
            return
        close = float(candle.ClosePrice)
        rsi_val = self._rsi_value
        if self.Position == 0:
            if rsi_val < self.rsi_low and close < lower:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - self.stop_loss
                return
            if rsi_val > self.rsi_high and close > upper:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price + self.stop_loss
                return
        if self.Position > 0:
            profit = close - self._entry_price
            new_stop = close - self.trailing_stop
            if profit > self.min_profit and new_stop > self._stop_price:
                self._stop_price = new_stop
            if close <= self._stop_price:
                self.SellMarket()
        elif self.Position < 0:
            profit = self._entry_price - close
            new_stop = close + self.trailing_stop
            if profit > self.min_profit and new_stop < self._stop_price:
                self._stop_price = new_stop
            if close >= self._stop_price:
                self.BuyMarket()

    def CreateClone(self):
        return rgt_ea_rsi_strategy()
