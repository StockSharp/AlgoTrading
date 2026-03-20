import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class anchored_momentum_breakout_strategy(Strategy):

    def __init__(self):
        super(anchored_momentum_breakout_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 34) \
            .SetDisplay("SMA Period", "Period for simple moving average", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for exponential moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")

        self._prev = 0.0
        self._prev_prev = 0.0
        self._initialized = False

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @SmaPeriod.setter
    def SmaPeriod(self, value):
        self._sma_period.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    def OnStarted(self, time):
        super(anchored_momentum_breakout_strategy, self).OnStarted(time)

        self._initialized = False

        sma = ExponentialMovingAverage()
        sma.Length = self.SmaPeriod
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(sma, ema, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfitPercent), UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLossPercent), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, sma_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        sma_f = float(sma_val)
        ema_f = float(ema_val)

        if sma_f == 0:
            return

        mom = 100.0 * (ema_f / sma_f - 1.0)

        if not self._initialized:
            self._prev = mom
            self._prev_prev = mom
            self._initialized = True
            return

        if self._prev < self._prev_prev and mom >= self._prev and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev > self._prev_prev and mom <= self._prev and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev = self._prev
        self._prev = mom

    def OnReseted(self):
        super(anchored_momentum_breakout_strategy, self).OnReseted()
        self._prev = 0.0
        self._prev_prev = 0.0
        self._initialized = False

    def CreateClone(self):
        return anchored_momentum_breakout_strategy()
