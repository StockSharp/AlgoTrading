import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class j_satl_candle_strategy(Strategy):
    def __init__(self):
        super(j_satl_candle_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "Period for Jurik Moving Average", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
        self._enable_stop_loss = self.Param("EnableStopLoss", True) \
            .SetDisplay("Enable Stop Loss", "Use stop loss", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
        self._prev_jma = None
        self._prev_direction = 0

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def enable_stop_loss(self):
        return self._enable_stop_loss.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    def OnReseted(self):
        super(j_satl_candle_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_direction = 0

    def OnStarted(self, time):
        super(j_satl_candle_strategy, self).OnStarted(time)
        self._prev_jma = None
        self._prev_direction = 0
        jma = JurikMovingAverage()
        jma.Length = int(self.jma_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()
        tp_val = float(self.take_profit_percent) * 100.0
        sl = Unit(float(self.stop_loss_percent) * 100.0, UnitTypes.Percent) if self.enable_stop_loss else None
        self.StartProtection(
            takeProfit=Unit(tp_val, UnitTypes.Percent),
            stopLoss=sl)

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        if self._prev_jma is not None:
            diff = jma_value - self._prev_jma
            if diff > 0:
                direction = 1
            elif diff < 0:
                direction = -1
            else:
                direction = 0
        else:
            direction = 0
        if self._prev_direction <= 0 and direction > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_direction >= 0 and direction < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_direction = direction
        self._prev_jma = jma_value

    def CreateClone(self):
        return j_satl_candle_strategy()
