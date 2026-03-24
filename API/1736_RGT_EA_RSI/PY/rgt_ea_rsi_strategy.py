import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from System import Array
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, IIndicator, IndicatorHelper
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

    def OnStarted(self, time):
        super(rgt_ea_rsi_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        bb = BollingerBands()
        bb.Length = 20
        bb.Width = 2.0
        indicators = Array[IIndicator]([rsi, bb])
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(indicators, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_process(self, candle, values):
        if candle.State != CandleStates.Finished:
            return
        if values[0].IsEmpty or values[1].IsEmpty:
            return
        rsi_val = IndicatorHelper.ToDecimal(values[0])
        bb_val = values[1]
        up = bb_val.UpBand
        lo = bb_val.LowBand
        if up is None or lo is None:
            return
        close = candle.ClosePrice
        if self.Position == 0:
            if rsi_val < self.rsi_low and close < lo:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price - self.stop_loss
                return
            if rsi_val > self.rsi_high and close > up:
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
