import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class grim_slash_strategy(Strategy):
    """
    Grim Slash strategy.
    Buys when the candle's low touches or dips below the previous close and exits when the high reaches the previous high.
    A percent take profit and stop loss manage the risk.
    """

    def __init__(self):
        super(grim_slash_strategy, self).__init__()
        self._take_profit_percent = self.Param("TakeProfitPercent", 15.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percent from the entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 5.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percent from the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_close = None
        self._prev_high = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grim_slash_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_high = None

    def OnStarted2(self, time):
        super(grim_slash_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._prev_high = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        tp = float(self._take_profit_percent.Value)
        sl = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        prev_close = self._prev_close
        prev_high = self._prev_high

        self._prev_close = candle.ClosePrice
        self._prev_high = candle.HighPrice

        if prev_close is None or prev_high is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if candle.HighPrice >= prev_high:
                self.SellMarket(self.Position)
        elif self.Position == 0 and candle.LowPrice <= prev_close:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return grim_slash_strategy()
