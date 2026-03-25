import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class trix_candle_strategy(Strategy):
    def __init__(self):
        super(trix_candle_strategy, self).__init__()
        self._trix_period = self.Param("TrixPeriod", 5) \
            .SetDisplay("TRIX Period", "Period for triple exponential smoothing", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for processing", "General")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Allow Buy Open", "Enable opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Allow Sell Open", "Enable opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Allow Buy Close", "Enable closing long positions", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Allow Sell Close", "Enable closing short positions", "Trading")
        self._open_tema = None
        self._close_tema = None
        self._prev_color = -1

    @property
    def trix_period(self):
        return self._trix_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value
    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value
    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value
    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    def OnReseted(self):
        super(trix_candle_strategy, self).OnReseted()
        self._open_tema = None
        self._close_tema = None
        self._prev_color = -1

    def OnStarted(self, time):
        super(trix_candle_strategy, self).OnStarted(time)
        self._open_tema = TripleExponentialMovingAverage()
        self._open_tema.Length = self.trix_period
        self._close_tema = TripleExponentialMovingAverage()
        self._close_tema.Length = self.trix_period
        self._prev_color = -1
        self.Indicators.Add(self._open_tema)
        self.Indicators.Add(self._close_tema)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        div_o = DecimalIndicatorValue(self._open_tema, candle.OpenPrice, candle.OpenTime)
        div_o.IsFinal = True
        open_result = self._open_tema.Process(div_o)
        div_c = DecimalIndicatorValue(self._close_tema, candle.ClosePrice, candle.OpenTime)
        div_c.IsFinal = True
        close_result = self._close_tema.Process(div_c)
        if not open_result.IsFormed or not close_result.IsFormed:
            return
        open_value = float(open_result)
        close_value = float(close_result)
        if open_value < close_value:
            color = 2
        elif open_value > close_value:
            color = 0
        else:
            color = 1

        if self._prev_color == -1:
            self._prev_color = color
            return

        buy_open = self.buy_pos_open and self._prev_color == 2 and color < 2
        sell_open = self.sell_pos_open and self._prev_color == 0 and color > 0
        buy_close_sig = self.buy_pos_close and self._prev_color == 0
        sell_close_sig = self.sell_pos_close and self._prev_color == 2

        if sell_close_sig and self.Position < 0:
            self.BuyMarket()
        if buy_close_sig and self.Position > 0:
            self.SellMarket()
        if buy_open and self.Position <= 0:
            self.BuyMarket()
        if sell_open and self.Position >= 0:
            self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return trix_candle_strategy()
