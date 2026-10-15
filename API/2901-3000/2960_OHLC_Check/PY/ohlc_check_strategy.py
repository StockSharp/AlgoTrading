import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ohlc_check_strategy(Strategy):
    def __init__(self):
        super(ohlc_check_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._confirm_bars = self.Param("ConfirmBars", 3) \
            .SetDisplay("Confirm Bars", "Consecutive candles to confirm direction", "Trading")

        self._bull_count = 0
        self._bear_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ConfirmBars(self):
        return self._confirm_bars.Value

    def OnReseted(self):
        super(ohlc_check_strategy, self).OnReseted()
        self._bull_count = 0
        self._bear_count = 0

    def OnStarted2(self, time):
        super(ohlc_check_strategy, self).OnStarted2(time)
        self._bull_count = 0
        self._bear_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        if close > open_price:
            self._bull_count += 1
            self._bear_count = 0
        elif close < open_price:
            self._bear_count += 1
            self._bull_count = 0

        cb = int(self.ConfirmBars)

        # Consecutive bullish candles
        if self._bull_count >= cb and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._bull_count = 0
        # Consecutive bearish candles
        elif self._bear_count >= cb and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._bear_count = 0

    def CreateClone(self):
        return ohlc_check_strategy()
