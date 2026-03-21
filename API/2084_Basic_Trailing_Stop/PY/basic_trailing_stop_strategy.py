import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class basic_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(basic_trailing_stop_strategy, self).__init__()
        self._stop_loss_pct = self.Param("StopLossPct", 1.5) \
            .SetDisplay("Stop Loss %", "Trailing stop distance as percentage", "Risk Management")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Relative Strength Index period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_price = 0.0

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(basic_trailing_stop_strategy, self).OnReseted()
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(basic_trailing_stop_strategy, self).OnStarted(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, rsi, self.on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, cci_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not cci_val.IsFormed or not rsi_val.IsFormed:
            return
        self.process_candle(candle, float(cci_val.ToDecimal()), float(rsi_val.ToDecimal()))

    def process_candle(self, candle, cci_value, rsi_value):
        close = float(candle.ClosePrice)
        stop_offset = close * float(self.stop_loss_pct) / 100.0

        if self.Position > 0:
            new_stop = close - stop_offset
            if new_stop > self._stop_price:
                self._stop_price = new_stop
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._stop_price = 0.0
            return

        if self.Position < 0:
            new_stop = close + stop_offset
            if self._stop_price == 0.0 or new_stop < self._stop_price:
                self._stop_price = new_stop
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._stop_price = 0.0
            return

        long_signal = cci_value < -50.0 and rsi_value < 40.0
        short_signal = cci_value > 50.0 and rsi_value > 60.0

        if long_signal:
            self.BuyMarket()
            self._stop_price = close - stop_offset
        elif short_signal:
            self.SellMarket()
            self._stop_price = close + stop_offset

    def CreateClone(self):
        return basic_trailing_stop_strategy()
