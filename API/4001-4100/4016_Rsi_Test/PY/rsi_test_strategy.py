import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_test_strategy(Strategy):
    def __init__(self):
        super(rsi_test_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 7).SetGreaterThanZero().SetDisplay("RSI Period", "Lookback period for RSI", "Indicators")
        self._buy_level = self.Param("BuyLevel", 40.0).SetDisplay("RSI Buy Level", "Oversold threshold for long entries", "Trading")
        self._sell_level = self.Param("SellLevel", 60.0).SetDisplay("RSI Sell Level", "Overbought threshold for short entries", "Trading")
        self._trailing_distance_steps = self.Param("TrailingDistanceSteps", 50).SetDisplay("Trailing Distance Steps", "Steps before activating trailing stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Primary timeframe", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rsi_test_strategy, self).OnReseted()
        self._prev_rsi = None
        self._entry_price = None
        self._stop_price = None
        self._trailing_armed = False

    def OnStarted2(self, time):
        super(rsi_test_strategy, self).OnStarted2(time)
        self._prev_rsi = None
        self._entry_price = None
        self._stop_price = None
        self._trailing_armed = False
        self._price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._price_step = float(self.Security.PriceStep)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        # Manage existing position
        self._manage_position(candle, close)

        if self._prev_rsi is None:
            self._prev_rsi = rsi_val
            return

        # Entry signals
        if rsi_val < self._buy_level.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
                self._reset_state()
            if self.Position == 0:
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = None
                self._trailing_armed = False
        elif rsi_val > self._sell_level.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
                self._reset_state()
            if self.Position == 0:
                self.SellMarket()
                self._entry_price = close
                self._stop_price = None
                self._trailing_armed = False

        self._prev_rsi = rsi_val

    def _manage_position(self, candle, close):
        if self.Position == 0:
            self._reset_state()
            return

        if self.Position > 0:
            self._update_trailing_long(candle)
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_state()
        elif self.Position < 0:
            self._update_trailing_short(candle)
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_state()

    def _update_trailing_long(self, candle):
        if self._trailing_distance_steps.Value <= 0 or self._entry_price is None or self._trailing_armed:
            return
        dist = self._trailing_distance_steps.Value * self._price_step
        if dist <= 0:
            return
        activation = self._entry_price + dist
        if float(candle.HighPrice) >= activation:
            self._stop_price = self._entry_price + dist
            self._trailing_armed = True

    def _update_trailing_short(self, candle):
        if self._trailing_distance_steps.Value <= 0 or self._entry_price is None or self._trailing_armed:
            return
        dist = self._trailing_distance_steps.Value * self._price_step
        if dist <= 0:
            return
        activation = self._entry_price - dist
        if float(candle.LowPrice) <= activation:
            self._stop_price = self._entry_price - dist
            self._trailing_armed = True

    def _reset_state(self):
        self._entry_price = None
        self._stop_price = None
        self._trailing_armed = False

    def CreateClone(self):
        return rsi_test_strategy()
