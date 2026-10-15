import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

MIDDLE_LEVEL = 50.0

class graph_style_4th_dimension_rsi_strategy(Strategy):
    """
    Graph Style 4th Dimension RSI strategy.
    Goes long when RSI leaves the oversold zone upward on a candle that closed higher than the previous one, and short when RSI
    leaves the overbought zone downward on a candle that closed lower, reversing an opposite position. A long closes when RSI
    returns to the middle level 50 from below and a short when it returns from above; a percent stop limits the loss.
    """

    def __init__(self):
        super(graph_style_4th_dimension_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 70.0).SetDisplay("Overbought", "RSI overbought level", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 30.0).SetDisplay("Oversold", "RSI oversold level", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi = None
        self._prev_close = None

    def OnReseted(self):
        super(graph_style_4th_dimension_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(graph_style_4th_dimension_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_value)
        close = float(candle.ClosePrice)

        last_rsi = self._prev_rsi
        last_close = self._prev_close

        self._prev_rsi = rsi
        self._prev_close = close

        if last_rsi is None or last_close is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        oversold = float(self._oversold_level.Value)
        overbought = float(self._overbought_level.Value)

        long_signal = last_rsi < oversold and rsi >= oversold and close > last_close
        short_signal = last_rsi > overbought and rsi <= overbought and close < last_close

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and rsi >= MIDDLE_LEVEL:
            self.SellMarket(self.Position)
        elif self.Position < 0 and rsi <= MIDDLE_LEVEL:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return graph_style_4th_dimension_rsi_strategy()
