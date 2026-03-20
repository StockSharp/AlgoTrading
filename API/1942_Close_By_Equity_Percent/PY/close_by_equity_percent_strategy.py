import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class close_by_equity_percent_strategy(Strategy):

    def __init__(self):
        super(close_by_equity_percent_strategy, self).__init__()

        self._equity_percent = self.Param("EquityPercentFromBalance", 1.2) \
            .SetDisplay("Equity/Bal Multiplier", "Threshold multiplier for equity relative to balance", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for periodic checks", "General")

        self._current_balance = 0.0

    @property
    def EquityPercentFromBalance(self):
        return self._equity_percent.Value

    @EquityPercentFromBalance.setter
    def EquityPercentFromBalance(self, value):
        self._equity_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(close_by_equity_percent_strategy, self).OnStarted(time)

        portfolio = self.Portfolio
        if portfolio is not None:
            self._current_balance = float(portfolio.CurrentValue)
        else:
            self._current_balance = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        portfolio = self.Portfolio
        equity = float(portfolio.CurrentValue) if portfolio is not None else 0.0

        if equity > self._current_balance * float(self.EquityPercentFromBalance):
            pos = self.Position
            if pos > 0:
                self.SellMarket(pos)
            elif pos < 0:
                self.BuyMarket(-pos)

            self._current_balance = equity
            return

        if self.Position == 0:
            self._current_balance = equity
            if candle.ClosePrice > candle.OpenPrice:
                self.BuyMarket()
            elif candle.ClosePrice < candle.OpenPrice:
                self.SellMarket()

    def OnReseted(self):
        super(close_by_equity_percent_strategy, self).OnReseted()
        self._current_balance = 0.0

    def CreateClone(self):
        return close_by_equity_percent_strategy()
