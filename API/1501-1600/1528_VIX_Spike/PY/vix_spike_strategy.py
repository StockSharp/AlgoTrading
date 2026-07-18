import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class vix_spike_strategy(Strategy):
    def __init__(self):
        super(vix_spike_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands length", "Parameters")
        self._bb_width = self.Param("BbWidth", 2.0) \
            .SetDisplay("BB Width", "Bollinger Bands width multiplier", "Parameters")
        self._exit_periods = self.Param("ExitPeriods", 15) \
            .SetDisplay("Exit Bars", "Bars to hold position", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._cooldown = 0

    @property
    def bb_length(self):
        return self._bb_length.Value

    @property
    def bb_width(self):
        return self._bb_width.Value

    @property
    def exit_periods(self):
        return self._exit_periods.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vix_spike_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vix_spike_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bb_length
        bb.Width = self.bb_width
        self._cooldown = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_process(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        upper = float(value.UpBand)
        lower = float(value.LowBand)
        if upper == 0 or lower == 0:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        close = float(candle.ClosePrice)
        if close < lower and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 60
        elif close > upper and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 60

    def CreateClone(self):
        return vix_spike_strategy()
