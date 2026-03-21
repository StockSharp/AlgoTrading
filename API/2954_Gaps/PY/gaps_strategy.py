import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class gaps_strategy(Strategy):
    def __init__(self):
        super(gaps_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._gap_percent = self.Param("GapPercent", 0.05) \
            .SetDisplay("Gap Percent", "Minimum gap size as percentage", "Trading")

        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def GapPercent(self):
        return self._gap_percent.Value

    def OnReseted(self):
        super(gaps_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted(self, time):
        super(gaps_strategy, self).OnStarted(time)
        self._prev_close = None

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

        if self._prev_close is None:
            self._prev_close = close
            return

        prev_close = self._prev_close
        self._prev_close = close

        if prev_close == 0:
            return

        gap_pct = (open_price - prev_close) / prev_close * 100.0
        gp = float(self.GapPercent)

        # Gap up detected - sell expecting gap fill
        if gap_pct > gp and self.Position == 0:
            self.SellMarket()
        # Gap down detected - buy expecting gap fill
        elif gap_pct < -gp and self.Position == 0:
            self.BuyMarket()

    def CreateClone(self):
        return gaps_strategy()
