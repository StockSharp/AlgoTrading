import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, WilliamsR
from StockSharp.Algo.Strategies import Strategy

class macd_williams_r_strategy(Strategy):
    """
    MACD Williams %R strategy.
    MACD above its signal line with Williams %R below WilliamsROversold goes long and MACD below the signal line with %R above
    WilliamsROverbought goes short,
    reversing an opposite position. A long closes when MACD crosses below the signal line and a short when it crosses above it,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(macd_williams_r_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period of Williams %R", "Williams %R")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0).SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Williams %R")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0).SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Williams %R")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(macd_williams_r_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        williams = WilliamsR()
        williams.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, williams, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)
                self.DrawIndicator(oscillators, williams)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, macd_value, williams_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or not williams_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        williams = williams_value.GetValue[Decimal](None)

        if macd > signal and williams < Decimal(self._williams_r_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif macd < signal and williams > Decimal(self._williams_r_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and macd < signal:
            self.SellMarket(self.Position)
        elif self.Position < 0 and macd > signal:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return macd_williams_r_strategy()
