import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class knux_martingale_strategy(Strategy):

    def __init__(self):
        super(knux_martingale_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX filter", "Indicators")
        self._lots_multiplier = self.Param("LotsMultiplier", 1.5) \
            .SetDisplay("Lots Multiplier", "Multiplier for losing trades", "Risk")
        self._stop_loss = self.Param("StopLoss", 150.0) \
            .SetDisplay("Stop Loss", "Absolute stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 300.0) \
            .SetDisplay("Take Profit", "Absolute take profit in price units", "Risk")
        self._trend_threshold = self.Param("TrendThreshold", 0.008) \
            .SetDisplay("Trend Threshold", "Minimum distance from trend average", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed position", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for strategy", "General")

        self._current_volume = 0.0
        self._prev_sma = 0.0
        self._has_prev_sma = False
        self._bars_since_exit = 0

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def LotsMultiplier(self):
        return self._lots_multiplier.Value

    @LotsMultiplier.setter
    def LotsMultiplier(self, value):
        self._lots_multiplier.Value = value

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
    def TrendThreshold(self):
        return self._trend_threshold.Value

    @TrendThreshold.setter
    def TrendThreshold(self, value):
        self._trend_threshold.Value = value

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

    def OnStarted2(self, time):
        super(knux_martingale_strategy, self).OnStarted2(time)

        self._current_volume = float(self.Volume)

        sma = SimpleMovingAverage()
        sma.Length = self.AdxPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(sma, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)

        if self._bars_since_exit < self.CooldownBars:
            self._bars_since_exit += 1

        if not self._has_prev_sma:
            self._prev_sma = sma_val
            self._has_prev_sma = True
            return

        close = float(candle.ClosePrice)
        distance = abs(close - sma_val) / sma_val if sma_val != 0.0 else 0.0
        is_trend_up = close > sma_val and sma_val > self._prev_sma
        is_trend_down = close < sma_val and sma_val < self._prev_sma

        if distance < float(self.TrendThreshold):
            self._prev_sma = sma_val
            return

        if self._bars_since_exit < self.CooldownBars and self.Position == 0:
            self._prev_sma = sma_val
            return

        volume = max(float(self.Volume), self._current_volume)
        pos = self.Position

        if is_trend_up and float(candle.ClosePrice) > float(candle.OpenPrice) and pos <= 0:
            self.BuyMarket(volume)
        elif is_trend_down and float(candle.ClosePrice) < float(candle.OpenPrice) and pos >= 0:
            self.SellMarket(volume)

        self._prev_sma = sma_val

    def OnOwnTradeReceived(self, my_trade):
        super(knux_martingale_strategy, self).OnOwnTradeReceived(my_trade)

        if self.Position != 0:
            return

        if my_trade.PnL < 0:
            self._current_volume *= float(self.LotsMultiplier)
        else:
            self._current_volume = float(self.Volume)

        self._bars_since_exit = 0

    def OnReseted(self):
        super(knux_martingale_strategy, self).OnReseted()
        self._current_volume = float(self.Volume)
        self._prev_sma = 0.0
        self._has_prev_sma = False
        self._bars_since_exit = self.CooldownBars

    def CreateClone(self):
        return knux_martingale_strategy()
