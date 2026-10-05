import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

ADX_LENGTH = 14


class adx_for_btc_strategy(Strategy):
    """
    ADX for BTC strategy.
    Long only: buys when ADX(14) crosses above EntryLevel while, with SmaFilter enabled, the close is above SMA(SmaLength), and closes
    the long when ADX crosses below ExitLevel.
    """

    def __init__(self):
        super(adx_for_btc_strategy, self).__init__()
        self._entry_level = self.Param("EntryLevel", 14.0).SetDisplay("Entry Level", "ADX level whose upward cross opens a long", "ADX")
        self._exit_level = self.Param("ExitLevel", 45.0).SetDisplay("Exit Level", "ADX level whose downward cross closes the long", "ADX")
        self._sma_filter = self.Param("SmaFilter", True).SetDisplay("SMA Filter", "Require the close above the SMA", "Filters")
        self._sma_length = self.Param("SmaLength", 200).SetGreaterThanZero().SetDisplay("SMA Length", "SMA period of the trend filter", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_adx = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_for_btc_strategy, self).OnReseted()
        self._prev_adx = None

    def OnStarted2(self, time):
        super(adx_for_btc_strategy, self).OnStarted2(time)

        self._prev_adx = None

        adx = AverageDirectionalIndex()
        adx.Length = ADX_LENGTH
        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, adx_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFormed or adx_value.MovingAverage is None:
            return

        adx = adx_value.MovingAverage
        prev = self._prev_adx
        self._prev_adx = adx

        if not sma_value.IsFormed or prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        entry = Decimal(self._entry_level.Value)
        exit_level = Decimal(self._exit_level.Value)
        trend_ok = not self._sma_filter.Value or candle.ClosePrice > sma_value.GetValue[Decimal](None)

        if self.Position == 0 and prev <= entry and adx > entry and trend_ok:
            self.BuyMarket(self.Volume)
        elif self.Position > 0 and prev >= exit_level and adx < exit_level:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return adx_for_btc_strategy()
