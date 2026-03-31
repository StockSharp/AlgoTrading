import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_histogram_strategy(Strategy):
    def __init__(self):
        super(cci_histogram_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Period length for the CCI indicator", "General")
        self._upper_level = self.Param("UpperLevel", 100.0) \
            .SetDisplay("Upper Level", "Upper CCI level", "General")
        self._lower_level = self.Param("LowerLevel", -100.0) \
            .SetDisplay("Lower Level", "Lower CCI level", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 100.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
        self._take_profit_points = self.Param("TakeProfitPoints", 200.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk Management")
        self._use_stop_loss = self.Param("UseStopLoss", False) \
            .SetDisplay("Enable Stop Loss", "Use stop loss protection", "Risk Management")
        self._use_take_profit = self.Param("UseTakeProfit", False) \
            .SetDisplay("Enable Take Profit", "Use take profit protection", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_cci = 0.0
        self._initialized = False

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def upper_level(self):
        return self._upper_level.Value

    @property
    def lower_level(self):
        return self._lower_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_histogram_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(cci_histogram_strategy, self).OnStarted2(time)
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return
        cci_val = float(cci_val)
        if not self._initialized:
            self._prev_cci = cci_val
            self._initialized = True
            return
        upper = float(self.upper_level)
        lower = float(self.lower_level)
        if self._prev_cci > upper and cci_val <= upper and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_cci < lower and cci_val >= lower and self.Position >= 0:
            self.SellMarket()
        self._prev_cci = cci_val

    def CreateClone(self):
        return cci_histogram_strategy()
