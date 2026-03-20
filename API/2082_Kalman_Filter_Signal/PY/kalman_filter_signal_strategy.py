import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import KalmanFilter
from StockSharp.Algo.Strategies import Strategy


class kalman_filter_signal_strategy(Strategy):
    def __init__(self):
        super(kalman_filter_signal_strategy, self).__init__()
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._prev_filter = None
        self._prev_signal = None

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kalman_filter_signal_strategy, self).OnReseted()
        self._prev_filter = None
        self._prev_signal = None

    def OnStarted(self, time):
        super(kalman_filter_signal_strategy, self).OnStarted(time)
        kalman = KalmanFilter()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(kalman, self.process_candle).Start()
        self.StartProtection(
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kalman)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, filter_value):
        if candle.State != CandleStates.Finished:
            return
        filter_value = float(filter_value)
        signal = 1.0 if float(candle.ClosePrice) > filter_value else 0.0

        if self._prev_signal is not None and signal != self._prev_signal:
            if signal > 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif signal == 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_filter = filter_value
        self._prev_signal = signal

    def CreateClone(self):
        return kalman_filter_signal_strategy()
