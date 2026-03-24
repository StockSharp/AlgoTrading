import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage, ZeroLagExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zero_lag_ma_trend_following_strategy(Strategy):
    def __init__(self):
        super(zero_lag_ma_trend_following_strategy, self).__init__()
        self._length_param = self.Param("Length", 34) \
            .SetDisplay("Length", "MA length", "Indicators")
        self._atr_period_param = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR length", "Indicators")
        self._risk_reward_param = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk/Reward", "Take profit ratio", "Risk")
        self._candle_type_param = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_zlma = 0.0
        self._prev_ema = 0.0
        self._long_setup = False
        self._short_setup = False
        self._box_top = 0.0
        self._box_bottom = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_placed = False

    @property
    def length(self):
        return self._length_param.Value

    @property
    def atr_period(self):
        return self._atr_period_param.Value

    @property
    def risk_reward(self):
        return self._risk_reward_param.Value

    @property
    def candle_type(self):
        return self._candle_type_param.Value

    def OnReseted(self):
        super(zero_lag_ma_trend_following_strategy, self).OnReseted()
        self._prev_zlma = 0.0
        self._prev_ema = 0.0
        self._long_setup = False
        self._short_setup = False
        self._box_top = 0.0
        self._box_bottom = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_placed = False

    def OnStarted(self, time):
        super(zero_lag_ma_trend_following_strategy, self).OnStarted(time)
        zlma = ZeroLagExponentialMovingAverage()
        zlma.Length = self.length
        ema = ExponentialMovingAverage()
        ema.Length = self.length
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(zlma, ema, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, zlma_val, ema_val, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_zlma == 0:
            self._prev_zlma = zlma_val
            self._prev_ema = ema_val
            return
        cross_up = self._prev_zlma <= self._prev_ema and zlma_val > ema_val
        cross_down = self._prev_zlma >= self._prev_ema and zlma_val < ema_val
        self._prev_zlma = zlma_val
        self._prev_ema = ema_val
        if cross_up:
            self._box_top = zlma_val
            self._box_bottom = zlma_val - atr_value
            self._long_setup = True
            self._short_setup = False
        elif cross_down:
            self._box_top = zlma_val + atr_value
            self._box_bottom = zlma_val
            self._short_setup = True
            self._long_setup = False
        price = candle.ClosePrice
        if not self._entry_placed:
            if self._long_setup and candle.LowPrice > self._box_top and self.Position <= 0:
                self.BuyMarket()
                self._entry_placed = True
                self._stop_price = self._box_bottom
                self._take_profit_price = price + (price - self._stop_price) * self.risk_reward
                self._long_setup = False
            elif self._short_setup and candle.HighPrice < self._box_bottom and self.Position >= 0:
                self.SellMarket()
                self._entry_placed = True
                self._stop_price = self._box_top
                self._take_profit_price = price - (self._stop_price - price) * self.risk_reward
                self._short_setup = False
        else:
            if self.Position > 0:
                if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_profit_price:
                    self.SellMarket()
                    self._entry_placed = False
            elif self.Position < 0:
                if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_profit_price:
                    self.BuyMarket()
                    self._entry_placed = False

    def CreateClone(self):
        return zero_lag_ma_trend_following_strategy()
