import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_automated_strategy(Strategy):
    def __init__(self):
        super(rsi_automated_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation length", "RSI")
        self._overbought = self.Param("Overbought", 75.0) \
            .SetDisplay("Overbought", "RSI value to open short", "RSI")
        self._oversold = self.Param("Oversold", 25.0) \
            .SetDisplay("Oversold", "RSI value to open long", "RSI")
        self._exit_level = self.Param("ExitLevel", 50.0) \
            .SetDisplay("Exit Level", "RSI level to close position", "RSI")
        self._stop_loss_points = self.Param("StopLossPoints", 50.0) \
            .SetDisplay("Stop Loss", "Initial stop loss in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 150.0) \
            .SetDisplay("Take Profit", "Take profit distance in points", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 25.0) \
            .SetDisplay("Trailing", "Trailing stop distance in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def oversold(self):
        return self._oversold.Value

    @property
    def exit_level(self):
        return self._exit_level.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    @property
    def trailing_stop_points(self):
        return self._trailing_stop_points.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_automated_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted(self, time):
        super(rsi_automated_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _reset_state(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_val = float(rsi_value)
        close = float(candle.ClosePrice)
        sl = float(self.stop_loss_points)
        tp = float(self.take_profit_points)
        trail = float(self.trailing_stop_points)

        if self.Position == 0:
            if rsi_val < float(self.oversold):
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = close - sl
                self._take_profit_price = close + tp
            elif rsi_val > float(self.overbought):
                self.SellMarket()
                self._entry_price = close
                self._stop_price = close + sl
                self._take_profit_price = close - tp
            return

        if self.Position > 0:
            if rsi_val > float(self.exit_level):
                self.SellMarket()
                self._reset_state()
                return

            low = float(candle.LowPrice)
            high = float(candle.HighPrice)
            if low <= self._stop_price or high >= self._take_profit_price:
                self.SellMarket()
                self._reset_state()
                return

            if trail > 0.0 and close - self._entry_price > trail:
                new_stop = close - trail
                if new_stop > self._stop_price:
                    self._stop_price = new_stop
        else:
            if rsi_val < float(self.exit_level):
                self.BuyMarket()
                self._reset_state()
                return

            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            if high >= self._stop_price or low <= self._take_profit_price:
                self.BuyMarket()
                self._reset_state()
                return

            if trail > 0.0 and self._entry_price - close > trail:
                new_stop = close + trail
                if self._stop_price == 0.0 or new_stop < self._stop_price:
                    self._stop_price = new_stop

    def CreateClone(self):
        return rsi_automated_strategy()
