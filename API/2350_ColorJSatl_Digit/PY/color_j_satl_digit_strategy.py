import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_j_satl_digit_strategy(Strategy):
    def __init__(self):
        super(color_j_satl_digit_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 30) \
            .SetDisplay("JMA Length", "JMA period length", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Indicator timeframe", "Parameters")
        self._direct_mode = self.Param("DirectMode", True) \
            .SetDisplay("Direct Mode", "Trade in direction of signal", "Parameters")
        self._stop_loss = self.Param("StopLoss", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
        self._prev_jma = None
        self._prev_prev_jma = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def direct_mode(self):
        return self._direct_mode.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    def OnReseted(self):
        super(color_j_satl_digit_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_prev_jma = None

    def OnStarted(self, time):
        super(color_j_satl_digit_strategy, self).OnStarted(time)
        self._prev_jma = None
        self._prev_prev_jma = None
        jma = JurikMovingAverage()
        jma.Length = int(self.jma_length)
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(jma, self.process_candle).Start()
        tp_val = float(self.take_profit) * 100.0
        sl_val = float(self.stop_loss) * 100.0
        self.StartProtection(
            takeProfit=Unit(tp_val, UnitTypes.Percent),
            stopLoss=Unit(sl_val, UnitTypes.Percent))

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        if self._prev_jma is not None and self._prev_prev_jma is not None:
            turn_up = self._prev_jma > self._prev_prev_jma and jma_value >= self._prev_jma
            turn_down = self._prev_jma < self._prev_prev_jma and jma_value <= self._prev_jma
            if self.direct_mode:
                if turn_up and self.Position <= 0:
                    self.BuyMarket()
                elif turn_down and self.Position >= 0:
                    self.SellMarket()
            else:
                if turn_down and self.Position <= 0:
                    self.BuyMarket()
                elif turn_up and self.Position >= 0:
                    self.SellMarket()
        self._prev_prev_jma = self._prev_jma
        self._prev_jma = jma_value

    def CreateClone(self):
        return color_j_satl_digit_strategy()
