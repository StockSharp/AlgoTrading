import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class exp_candlesticks_bw_time_strategy(Strategy):
    """
    Bill Williams Candlesticks BW strategy (simplified).
    Uses candle body direction with SMA trend filter.
    Buys after 3 consecutive bullish candles above SMA, sells after 3 bearish below SMA.
    """

    def __init__(self):
        super(exp_candlesticks_bw_time_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._sma_length = self.Param("SmaLength", 34) \
            .SetDisplay("SMA Length", "Bill Williams median line proxy", "Indicators")

        self._bull_count = 0
        self._bear_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_candlesticks_bw_time_strategy, self).OnReseted()
        self._bull_count = 0
        self._bear_count = 0

    def OnStarted(self, time):
        super(exp_candlesticks_bw_time_strategy, self).OnStarted(time)

        self._bull_count = 0
        self._bear_count = 0

        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        sma_val = float(sma_val)

        is_bullish = close > open_p
        is_bearish = close < open_p

        if is_bullish:
            self._bull_count += 1
            self._bear_count = 0
        elif is_bearish:
            self._bear_count += 1
            self._bull_count = 0

        if self._bull_count >= 3 and close > sma_val and self.Position <= 0:
            self.BuyMarket()
            self._bull_count = 0
        elif self._bear_count >= 3 and close < sma_val and self.Position >= 0:
            self.SellMarket()
            self._bear_count = 0

    def CreateClone(self):
        return exp_candlesticks_bw_time_strategy()
