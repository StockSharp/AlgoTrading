import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bedo_osaimi_istr_strategy(Strategy):
    def __init__(self):
        super(bedo_osaimi_istr_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma_length = self.Param("MaLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Moving average length", "Parameters")
        self._prev_close = None
        self._prev_open = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bedo_osaimi_istr_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_open = None

    def OnStarted2(self, time):
        super(bedo_osaimi_istr_strategy, self).OnStarted2(time)
        self._close_ma = SimpleMovingAverage()
        self._close_ma.Length = self._ma_length.Value
        self._open_ma = SimpleMovingAverage()
        self._open_ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._close_ma, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._close_ma)
            self.DrawIndicator(area, self._open_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, close_ma_value):
        if candle.State != CandleStates.Finished:
            return

        iv = DecimalIndicatorValue(self._open_ma, float(candle.OpenPrice), candle.ServerTime)
        iv.IsFinal = True
        open_ma_result = self._open_ma.Process(iv)
        if not open_ma_result.IsFormed:
            self._prev_close = float(close_ma_value)
            self._prev_open = None
            return

        open_ma_value = float(open_ma_result)
        close_ma_v = float(close_ma_value)

        if self._prev_close is None or self._prev_open is None:
            self._prev_close = close_ma_v
            self._prev_open = open_ma_value
            if self.Position == 0:
                self.BuyMarket()
            return

        prev_close = self._prev_close
        prev_open = self._prev_open

        if close_ma_v > open_ma_value and prev_close <= prev_open and self.Position == 0:
            self.BuyMarket()
        elif close_ma_v < open_ma_value and prev_close >= prev_open and self.Position == 0:
            self.SellMarket()

        self._prev_close = close_ma_v
        self._prev_open = open_ma_value

    def CreateClone(self):
        return bedo_osaimi_istr_strategy()
