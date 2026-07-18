import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy


class hawaiian_tsunami_surfer_strategy(Strategy):
    def __init__(self):
        super(hawaiian_tsunami_surfer_strategy, self).__init__()
        self._momentum_period = self.Param("MomentumPeriod", 1) \
            .SetDisplay("Momentum Period", "Period of the momentum indicator", "Indicators")
        self._tsunami_strength = self.Param("TsunamiStrength", 0.24) \
            .SetDisplay("Threshold", "Momentum percentage deviation from 0%", "Parameters")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk Management")
        self._stop_loss_points = self.Param("StopLossPoints", 700) \
            .SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def momentum_period(self):
        return self._momentum_period.Value

    @property
    def tsunami_strength(self):
        return self._tsunami_strength.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(hawaiian_tsunami_surfer_strategy, self).OnStarted2(time)

        momentum = Momentum()
        momentum.Length = self.momentum_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(momentum, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        mom = float(momentum_value)
        prev_price = close - mom
        if prev_price == 0.0:
            return

        pct_change = (mom / prev_price) * 100.0
        threshold = float(self.tsunami_strength)

        if pct_change > threshold and self.Position >= 0:
            self.SellMarket()
        elif pct_change < -threshold and self.Position <= 0:
            self.BuyMarket()

    def CreateClone(self):
        return hawaiian_tsunami_surfer_strategy()
