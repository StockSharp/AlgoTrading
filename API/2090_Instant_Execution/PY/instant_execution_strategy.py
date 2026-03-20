import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class instant_execution_strategy(Strategy):
    def __init__(self):
        super(instant_execution_strategy, self).__init__()
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_ema = None

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def ema_period(self):
        return self._ema_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(instant_execution_strategy, self).OnReseted()
        self._prev_ema = None

    def OnStarted(self, time):
        super(instant_execution_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            isStopTrailing=True,
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        ema_value = float(ema_value)
        if self._prev_ema is not None:
            rising = ema_value > self._prev_ema
            falling = ema_value < self._prev_ema
            if rising and float(candle.ClosePrice) > ema_value and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif falling and float(candle.ClosePrice) < ema_value and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
        self._prev_ema = ema_value

    def CreateClone(self):
        return instant_execution_strategy()
