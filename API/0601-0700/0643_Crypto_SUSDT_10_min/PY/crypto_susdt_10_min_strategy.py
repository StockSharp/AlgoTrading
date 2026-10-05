import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class crypto_susdt_10_min_strategy(Strategy):
    """
    Crypto SUSDT 10 min strategy.
    A candle that opens below the EMA and closes above it buys, one that opens above the EMA and closes below it sells short,
    reversing an opposite position. Every trade is closed by a TakeProfitPercent target or a StopLossPercent stop.
    """

    def __init__(self):
        super(crypto_susdt_10_min_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 24).SetGreaterThanZero().SetDisplay("EMA Length", "EMA length", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._order_percent = self.Param("OrderPercent", 30.0).SetGreaterThanZero().SetDisplay("Order %", "Percent of equity per order", "Risk")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(crypto_susdt_10_min_strategy, self).OnStarted2(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        take = float(self._take_profit_percent.Value)
        stop = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(take), UnitTypes.Percent) if take > 0 else Unit(),
            Unit(Decimal(stop), UnitTypes.Percent) if stop > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.ClosePrice > ema and candle.OpenPrice < ema and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif candle.ClosePrice < ema and candle.OpenPrice > ema and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return crypto_susdt_10_min_strategy()
