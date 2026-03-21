import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bb_rsi_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(bb_rsi_trailing_stop_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 40) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.5) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 3000.0) \
            .SetDisplay("Stop Loss Points", "Initial stop loss in points", "Risk Management")
        self._trail_offset_points = self.Param("TrailOffsetPoints", 2000.0) \
            .SetDisplay("Trail Offset Points", "Profit to activate trailing stop", "Risk Management")
        self._trail_stop_points = self.Param("TrailStopPoints", 1500.0) \
            .SetDisplay("Trail Stop Points", "Trailing stop distance", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._trailing_price = 0.0
        self._trailing_active = False
        self._cooldown = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value
    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value
    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value
    @property
    def trail_offset_points(self):
        return self._trail_offset_points.Value
    @property
    def trail_stop_points(self):
        return self._trail_stop_points.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bb_rsi_trailing_stop_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._trailing_price = 0.0
        self._trailing_active = False
        self._cooldown = 0

    def _reset_stops(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._trailing_price = 0.0
        self._trailing_active = False
        self._cooldown = 100

    def OnStarted(self, time):
        super(bb_rsi_trailing_stop_strategy, self).OnStarted(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = self.bollinger_deviation
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, bb_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        upper = bb_value.UpBand
        lower = bb_value.LowBand
        if upper is None or lower is None:
            return
        if not rsi_value.IsFormed:
            return

        rsi = float(rsi_value)

        if self._cooldown > 0:
            self._cooldown -= 1

        if self.Position == 0 and self._cooldown == 0:
            if candle.LowPrice < lower and rsi < self.rsi_oversold:
                self.BuyMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = self._entry_price - self.stop_loss_points
                self._cooldown = 100
            elif candle.HighPrice > upper and rsi > self.rsi_overbought:
                self.SellMarket()
                self._entry_price = float(candle.ClosePrice)
                self._stop_price = self._entry_price + self.stop_loss_points
                self._cooldown = 100
        elif self.Position > 0:
            if not self._trailing_active and float(candle.ClosePrice) - self._entry_price >= self.trail_offset_points:
                self._trailing_active = True
                self._trailing_price = float(candle.ClosePrice) - self.trail_stop_points
            if self._trailing_active:
                new_level = float(candle.ClosePrice) - self.trail_stop_points
                if new_level > self._trailing_price:
                    self._trailing_price = new_level
            if candle.LowPrice <= self._stop_price or (self._trailing_active and candle.LowPrice <= self._trailing_price):
                self.SellMarket()
                self._reset_stops()
        else:
            if not self._trailing_active and self._entry_price - float(candle.ClosePrice) >= self.trail_offset_points:
                self._trailing_active = True
                self._trailing_price = float(candle.ClosePrice) + self.trail_stop_points
            if self._trailing_active:
                new_level = float(candle.ClosePrice) + self.trail_stop_points
                if new_level < self._trailing_price or self._trailing_price == 0:
                    self._trailing_price = new_level
            if candle.HighPrice >= self._stop_price or (self._trailing_active and candle.HighPrice >= self._trailing_price):
                self.BuyMarket()
                self._reset_stops()

    def CreateClone(self):
        return bb_rsi_trailing_stop_strategy()
