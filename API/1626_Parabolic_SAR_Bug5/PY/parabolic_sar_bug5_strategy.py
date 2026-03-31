import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_bug5_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_bug5_strategy, self).__init__()
        self._step = self.Param("Step", 0.001) \
            .SetDisplay("Step", "Initial acceleration factor", "Indicators")
        self._maximum = self.Param("Maximum", 0.2) \
            .SetDisplay("Maximum", "Maximum acceleration factor", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 90.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 20.0) \
            .SetDisplay("Take Profit", "Take profit distance in points", "Risk")
        self._trailing = self.Param("Trailing", False) \
            .SetDisplay("Use Trailing", "Enable trailing stop", "Risk")
        self._trail_points = self.Param("TrailPoints", 10.0) \
            .SetDisplay("Trail Points", "Trailing distance in points", "Risk")
        self._reverse = self.Param("Reverse", False) \
            .SetDisplay("Reverse", "Reverse trading direction", "General")
        self._sar_close = self.Param("SarClose", True) \
            .SetDisplay("SAR Close", "Close position on SAR switch", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_sar = 0.0
        self._prev_above = False
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0

    @property
    def step(self):
        return self._step.Value

    @property
    def maximum(self):
        return self._maximum.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    @property
    def trailing(self):
        return self._trailing.Value

    @property
    def trail_points(self):
        return self._trail_points.Value

    @property
    def reverse(self):
        return self._reverse.Value

    @property
    def sar_close(self):
        return self._sar_close.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_bug5_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_above = False
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0

    def OnStarted2(self, time):
        super(parabolic_sar_bug5_strategy, self).OnStarted2(time)
        psar = ParabolicSar()
        psar.Acceleration = self.step
        psar.AccelerationMax = self.maximum
        self._prev_sar = 0.0
        self._prev_above = False
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(psar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, psar)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sar):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        sar_f = float(sar)
        price_above = close > sar_f
        crossing = self._prev_sar > 0 and price_above != self._prev_above
        if crossing:
            is_buy_signal = price_above
            if self.reverse:
                is_buy_signal = not is_buy_signal
            if is_buy_signal and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
                self._take_price = close + self.take_profit_points
                self._stop_price = close - self.stop_loss_points
                self._highest_price = float(candle.HighPrice)
            elif not is_buy_signal and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
                self._take_price = close - self.take_profit_points
                self._stop_price = close + self.stop_loss_points
                self._lowest_price = float(candle.LowPrice)
        if self.Position > 0 and self._entry_price is not None:
            self._highest_price = max(self._highest_price, float(candle.HighPrice))
            if self.trailing:
                self._stop_price = max(self._stop_price if self._stop_price is not None else 0, self._highest_price - self.trail_points)
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_state()
            elif self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket()
                self._reset_state()
        elif self.Position < 0 and self._entry_price is not None:
            self._lowest_price = min(self._lowest_price, float(candle.LowPrice))
            if self.trailing:
                self._stop_price = min(self._stop_price if self._stop_price is not None else 1e18, self._lowest_price + self.trail_points)
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_state()
            elif self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket()
                self._reset_state()
        self._prev_sar = sar_f
        self._prev_above = price_above

    def _reset_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._highest_price = 0.0
        self._lowest_price = 0.0

    def CreateClone(self):
        return parabolic_sar_bug5_strategy()
