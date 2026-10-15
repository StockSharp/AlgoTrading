import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, DoubleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_dema_strategy(Strategy):
    def __init__(self):
        super(bollinger_bands_dema_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
        self._dema_period = self.Param("DemaPeriod", 20) \
            .SetDisplay("DEMA Period", "Length of double EMA", "Indicators")
        self._deviation = self.Param("Deviation", 2.0) \
            .SetDisplay("Deviation", "Standard deviation for Bollinger Bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Time frame for Bollinger calculation", "General")
        self._dema0 = None
        self._dema1 = None
        self._dema2 = None

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def dema_period(self):
        return self._dema_period.Value

    @property
    def deviation(self):
        return self._deviation.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_bands_dema_strategy, self).OnReseted()
        self._dema0 = None
        self._dema1 = None
        self._dema2 = None

    def OnStarted2(self, time):
        super(bollinger_bands_dema_strategy, self).OnStarted2(time)
        self._dema0 = None
        self._dema1 = None
        self._dema2 = None
        bollinger = BollingerBands()
        bollinger.Length = int(self.bollinger_period)
        bollinger.Width = float(self.deviation)
        dema = DoubleExponentialMovingAverage()
        dema.Length = int(self.dema_period)
        dema_sub = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        dema_sub.Bind(dema, self._process_dema).Start()
        main_sub = self.SubscribeCandles(self.candle_type)
        main_sub.BindEx(bollinger, self._process_main).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, main_sub)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_dema(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        value = float(value)
        self._dema2 = self._dema1
        self._dema1 = self._dema0
        self._dema0 = value

    def _process_main(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        if not value.IsFormed:
            return
        upper = value.UpBand
        lower = value.LowBand
        middle = value.MovingAverage
        if upper is None or lower is None or middle is None:
            return
        upper = float(upper)
        lower = float(lower)
        if self._dema0 is None or self._dema1 is None or self._dema2 is None:
            return
        dema_up = self._dema0 > self._dema1 and self._dema1 > self._dema2
        dema_down = self._dema0 < self._dema1 and self._dema1 < self._dema2
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        buy_condition = close > lower and open_p < lower and dema_up
        sell_condition = close < upper and open_p > upper and dema_down
        buy_close = close < upper and open_p > upper
        sell_close = close > lower and open_p < lower
        if buy_condition and self.Position <= 0:
            self.BuyMarket()
        if sell_condition and self.Position >= 0:
            self.SellMarket()
        if buy_close and self.Position > 0:
            self.SellMarket()
        if sell_close and self.Position < 0:
            self.BuyMarket()

    def CreateClone(self):
        return bollinger_bands_dema_strategy()
