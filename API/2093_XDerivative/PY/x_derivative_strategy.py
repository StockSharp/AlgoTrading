import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import JurikMovingAverage, RateOfChange
from StockSharp.Algo.Strategies import Strategy


class x_derivative_strategy(Strategy):
    def __init__(self):
        super(x_derivative_strategy, self).__init__()
        self._roc_period = self.Param("RocPeriod", 14) \
            .SetDisplay("ROC Period", "Period for rate of change", "Parameters")
        self._ma_length = self.Param("MaLength", 7) \
            .SetDisplay("JMA Length", "Period for Jurik MA smoothing", "Parameters")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._jma = None
        self._prev_value = None
        self._prev_prev_value = None

    @property
    def roc_period(self):
        return self._roc_period.Value
    @property
    def ma_length(self):
        return self._ma_length.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x_derivative_strategy, self).OnReseted()
        self._jma = None
        self._prev_value = None
        self._prev_prev_value = None

    def OnStarted(self, time):
        super(x_derivative_strategy, self).OnStarted(time)
        self._jma = JurikMovingAverage()
        self._jma.Length = self.ma_length
        roc = RateOfChange()
        roc.Length = self.roc_period
        self.Indicators.Add(self._jma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, roc)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, roc_value):
        if candle.State != CandleStates.Finished:
            return
        jma_result = self._jma.Process(float(roc_value), candle.OpenTime, True)
        if not jma_result.IsFormed:
            return
        value = float(jma_result)

        if self._prev_value is not None and self._prev_prev_value is not None:
            turn_up = self._prev_value < self._prev_prev_value and value > self._prev_value
            turn_down = self._prev_value > self._prev_prev_value and value < self._prev_value
            if turn_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif turn_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_prev_value = self._prev_value
        self._prev_value = value

    def CreateClone(self):
        return x_derivative_strategy()
