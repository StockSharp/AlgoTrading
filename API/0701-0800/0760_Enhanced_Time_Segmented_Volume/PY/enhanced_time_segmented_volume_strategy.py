import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class enhanced_time_segmented_volume_strategy(Strategy):
    """
    Enhanced time segmented volume strategy.
    TSV is the sum over TsvLength candles of the close change multiplied by the candle volume, and its SMA over MaLength is the
    signal line. TSV above the signal line and above zero goes long, TSV below the signal line and below zero goes short; an
    opposite signal reverses the position.
    """

    def __init__(self):
        super(enhanced_time_segmented_volume_strategy, self).__init__()
        self._tsv_length = self.Param("TsvLength", 13).SetGreaterThanZero().SetDisplay("TSV Length", "Number of candles summed into TSV", "Indicators")
        self._ma_length = self.Param("MaLength", 7).SetGreaterThanZero().SetDisplay("MA Length", "Length of the TSV moving average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._tsv_sum = None
        self._tsv_average = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(enhanced_time_segmented_volume_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted2(self, time):
        super(enhanced_time_segmented_volume_strategy, self).OnStarted2(time)

        self._prev_close = None

        # The SMA times its length gives the rolling sum.
        self._tsv_sum = SimpleMovingAverage()
        self._tsv_sum.Length = self._tsv_length.Value
        self._tsv_average = SimpleMovingAverage()
        self._tsv_average.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        prev_close = self._prev_close
        self._prev_close = close

        if prev_close is None:
            return

        flow = (close - prev_close) * float(candle.TotalVolume)
        sum_input = DecimalIndicatorValue(self._tsv_sum, Decimal(flow), candle.OpenTime)
        sum_input.IsFinal = True
        sum_value = self._tsv_sum.Process(sum_input)

        if not self._tsv_sum.IsFormed:
            return

        tsv = float(sum_value.GetValue[Decimal](None)) * self._tsv_length.Value
        average_input = DecimalIndicatorValue(self._tsv_average, Decimal(tsv), candle.OpenTime)
        average_input.IsFinal = True
        average_value = self._tsv_average.Process(average_input)

        if not self._tsv_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        average = float(average_value.GetValue[Decimal](None))

        if tsv > average and tsv > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif tsv < average and tsv < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return enhanced_time_segmented_volume_strategy()
