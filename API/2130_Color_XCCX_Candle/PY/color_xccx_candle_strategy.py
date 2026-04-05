import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class color_xccx_candle_strategy(Strategy):
    def __init__(self):
        super(color_xccx_candle_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", 5) \
            .SetDisplay("SMA Length", "Length of the moving averages", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for the strategy", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percent of entry price", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetDisplay("Take Profit %", "Take profit as percent of entry price", "Risk Management")
        self._open_ema = None
        self._close_ema = None
        self._prev_diff = 0.0
        self._has_prev = False

    @property
    def sma_length(self):
        return self._sma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    def OnReseted(self):
        super(color_xccx_candle_strategy, self).OnReseted()
        self._open_ema = None
        self._close_ema = None
        self._prev_diff = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(color_xccx_candle_strategy, self).OnStarted2(time)
        self._open_ema = ExponentialMovingAverage()
        self._open_ema.Length = self.sma_length
        self._close_ema = ExponentialMovingAverage()
        self._close_ema.Length = self.sma_length
        self._prev_diff = 0.0
        self._has_prev = False
        self.Indicators.Add(self._open_ema)
        self.Indicators.Add(self._close_ema)
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_percent), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_percent), UnitTypes.Percent))
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_result = process_float(self._open_ema, candle.OpenPrice, candle.OpenTime, True)
        close_result = process_float(self._close_ema, candle.ClosePrice, candle.OpenTime, True)
        if not open_result.IsFormed or not close_result.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        open_val = float(open_result)
        close_val = float(close_result)
        diff = close_val - open_val
        if not self._has_prev:
            self._prev_diff = diff
            self._has_prev = True
            return
        if self._prev_diff <= 0 and diff > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_diff >= 0 and diff < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_diff = diff

    def CreateClone(self):
        return color_xccx_candle_strategy()
