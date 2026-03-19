import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class machine_learning_super_trend_strategy(Strategy):
    """
    SuperTrend strategy with trailing take profit and stop loss.
    """

    def __init__(self):
        super(machine_learning_super_trend_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 4).SetDisplay("ATR Period", "ATR length for SuperTrend", "SuperTrend")
        self._atr_factor = self.Param("AtrFactor", 2.94).SetDisplay("Multiplier", "ATR multiplier", "SuperTrend")
        self._stop_loss_mult = self.Param("StopLossMultiplier", 0.01).SetDisplay("Stop Loss Mult", "Pct from SuperTrend", "Risk")
        self._take_profit_mult = self.Param("TakeProfitMultiplier", 0.03).SetDisplay("Take Profit Mult", "Pct from SuperTrend", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 8).SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))).SetDisplay("Candle Type", "Candles", "General")

        self._super_trend = None
        self._prev_direction = 0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._bars_from_signal = 8

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(machine_learning_super_trend_strategy, self).OnReseted()
        self._super_trend = None
        self._prev_direction = 0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._bars_from_signal = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(machine_learning_super_trend_strategy, self).OnStarted(time)
        self._bars_from_signal = self._cooldown_bars.Value
        self._super_trend = SuperTrend()
        self._super_trend.Length = self._atr_period.Value
        self._super_trend.Multiplier = self._atr_factor.Value
        dummy1 = ExponentialMovingAverage()
        dummy1.Length = 10
        dummy2 = ExponentialMovingAverage()
        dummy2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(dummy1, dummy2, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._super_trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        from StockSharp.Algo.Indicators import CandleIndicatorValue
        st_result = self._super_trend.Process(CandleIndicatorValue(self._super_trend, candle))
        if not self._super_trend.IsFormed or st_result.IsEmpty:
            return
        st_val = float(st_result.GetValue[float]())
        close = float(candle.ClosePrice)
        direction = 1 if close > st_val else -1
        direction_changed = self._prev_direction != 0 and direction != self._prev_direction
        self._bars_from_signal += 1
        can_trade = self._bars_from_signal >= self._cooldown_bars.Value
        sl_m = float(self._stop_loss_mult.Value)
        tp_m = float(self._take_profit_mult.Value)
        self._stop_loss = st_val - st_val * sl_m if direction == 1 else st_val + st_val * sl_m
        self._take_profit = st_val + st_val * tp_m if direction == 1 else st_val - st_val * tp_m
        if can_trade and direction_changed:
            if direction == 1 and self.Position <= 0:
                self.BuyMarket()
            elif direction == -1 and self.Position >= 0:
                self.SellMarket()
            self._bars_from_signal = 0
        if can_trade and self.Position > 0:
            if close <= self._stop_loss or close >= self._take_profit:
                self.SellMarket()
                self._bars_from_signal = 0
        elif can_trade and self.Position < 0:
            if close >= self._stop_loss or close <= self._take_profit:
                self.BuyMarket()
                self._bars_from_signal = 0
        self._prev_direction = direction

    def CreateClone(self):
        return machine_learning_super_trend_strategy()
