import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, DecimalIndicatorValue
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_ma_adx_ea_strategy(Strategy):

    def __init__(self):
        super(simple_ma_adx_ea_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 21) \
            .SetDisplay("Trend Period", "Period for trend confirmation", "Indicators")
        self._ma_period = self.Param("MaPeriod", 8) \
            .SetDisplay("MA Period", "EMA calculation period", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 0.05) \
            .SetDisplay("Trend Threshold", "Minimum average distance in percent", "Indicators")
        self._stop_loss = self.Param("StopLoss", 400.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 1200.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 200) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._ema = None
        self._trend_ma = None
        self._ema_prev1 = 0.0
        self._ema_prev2 = 0.0
        self._trend_prev = 0.0
        self._prev_close = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(simple_ma_adx_ea_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.MaPeriod
        self._trend_ma = ExponentialMovingAverage()
        self._trend_ma.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute),
            useMarketOrders=True)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        ema_val = float(self._ema.Process(
            DecimalIndicatorValue(self._ema, close, candle.OpenTime, True)))
        trend_ma_val = float(self._trend_ma.Process(
            DecimalIndicatorValue(self._trend_ma, close, candle.OpenTime, True)))

        if not self._ema.IsFormed or not self._trend_ma.IsFormed or trend_ma_val == 0.0:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if not self._is_initialized:
            self._ema_prev2 = ema_val
            self._ema_prev1 = ema_val
            self._trend_prev = trend_ma_val
            self._prev_close = float(close)
            self._is_initialized = True
            return

        distance_percent = abs(ema_val - trend_ma_val) / trend_ma_val * 100.0
        buy_cond1 = ema_val > self._ema_prev1 and self._ema_prev1 >= self._ema_prev2
        buy_cond2 = self._prev_close > self._ema_prev1 and float(close) > trend_ma_val
        buy_cond3 = trend_ma_val >= self._trend_prev and distance_percent >= float(self.AdxThreshold)
        sell_cond1 = ema_val < self._ema_prev1 and self._ema_prev1 <= self._ema_prev2
        sell_cond2 = self._prev_close < self._ema_prev1 and float(close) < trend_ma_val
        sell_cond3 = trend_ma_val <= self._trend_prev and distance_percent >= float(self.AdxThreshold)

        if self._bars_since_trade >= self.CooldownBars and self.Position == 0:
            if buy_cond1 and buy_cond2 and buy_cond3:
                self.BuyMarket()
                self._bars_since_trade = 0
            elif sell_cond1 and sell_cond2 and sell_cond3:
                self.SellMarket()
                self._bars_since_trade = 0

        self._ema_prev2 = self._ema_prev1
        self._ema_prev1 = ema_val
        self._trend_prev = trend_ma_val
        self._prev_close = float(close)

    def OnReseted(self):
        super(simple_ma_adx_ea_strategy, self).OnReseted()
        self._ema = None
        self._trend_ma = None
        self._ema_prev1 = 0.0
        self._ema_prev2 = 0.0
        self._trend_prev = 0.0
        self._prev_close = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return simple_ma_adx_ea_strategy()
