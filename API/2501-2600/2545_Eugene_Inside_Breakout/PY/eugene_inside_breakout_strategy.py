import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class eugene_inside_breakout_strategy(Strategy):
    """
    Breakout strategy derived from the Eugene expert advisor.
    Detects inside bars and trades breakouts with zigzag confirmation.
    """

    def __init__(self):
        super(eugene_inside_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._activation_hour = self.Param("ActivationHour", 8) \
            .SetDisplay("Activation Hour", "Hour when confirmations become unconditional", "Filters")

        self._prev_open1 = 0.0
        self._prev_close1 = 0.0
        self._prev_high1 = 0.0
        self._prev_low1 = 0.0
        self._prev_open2 = 0.0
        self._prev_close2 = 0.0
        self._prev_high2 = 0.0
        self._prev_low2 = 0.0
        self._has_prev1 = False
        self._has_prev2 = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(eugene_inside_breakout_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(eugene_inside_breakout_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev2:
            self._update_history(candle)
            return

        high0 = float(candle.HighPrice)
        low0 = float(candle.LowPrice)
        close0 = float(candle.ClosePrice)

        open1 = self._prev_open1
        close1 = self._prev_close1
        high1 = self._prev_high1
        low1 = self._prev_low1
        open2 = self._prev_open2
        close2 = self._prev_close2
        high2 = self._prev_high2
        low2 = self._prev_low2

        black_insider = high1 <= high2 and low1 >= low2 and close1 <= open1
        white_insider = high1 <= high2 and low1 >= low2 and close1 > open1
        white_bird = white_insider and close2 > open2
        black_bird = black_insider and close2 < open2

        if close1 < open1:
            zig_level_buy = open1 - (close1 - open1) / 3.0
        else:
            zig_level_buy = open1 - (open1 - low1) / 3.0

        if close1 > open1:
            zig_level_sell = open1 + (close1 - open1) / 3.0
        else:
            zig_level_sell = open1 + (high1 - open1) / 3.0

        hour = candle.CloseTime.Hour
        confirm_buy = (low0 <= zig_level_buy or hour >= self._activation_hour.Value) and not black_bird and not white_insider
        confirm_sell = (high0 >= zig_level_sell or hour >= self._activation_hour.Value) and not white_bird and not black_insider

        buy_signal = high0 > high1
        sell_signal = low0 < low1

        if self.Position == 0:
            if buy_signal and confirm_buy and low0 > low1 and low1 < high2:
                self.BuyMarket()
            elif sell_signal and confirm_sell and high0 < high1:
                self.SellMarket()
        elif self.Position > 0:
            if sell_signal and confirm_sell and high0 < high1:
                self.SellMarket()
        elif self.Position < 0:
            if buy_signal and confirm_buy and low0 > low1 and low1 < high2:
                self.BuyMarket()

        self._update_history(candle)

    def _update_history(self, candle):
        self._prev_open2 = self._prev_open1
        self._prev_close2 = self._prev_close1
        self._prev_high2 = self._prev_high1
        self._prev_low2 = self._prev_low1
        self._has_prev2 = self._has_prev1

        self._prev_open1 = float(candle.OpenPrice)
        self._prev_close1 = float(candle.ClosePrice)
        self._prev_high1 = float(candle.HighPrice)
        self._prev_low1 = float(candle.LowPrice)
        self._has_prev1 = True

    def _reset_history(self):
        self._prev_open1 = 0.0
        self._prev_close1 = 0.0
        self._prev_high1 = 0.0
        self._prev_low1 = 0.0
        self._prev_open2 = 0.0
        self._prev_close2 = 0.0
        self._prev_high2 = 0.0
        self._prev_low2 = 0.0
        self._has_prev1 = False
        self._has_prev2 = False

    def CreateClone(self):
        return eugene_inside_breakout_strategy()
