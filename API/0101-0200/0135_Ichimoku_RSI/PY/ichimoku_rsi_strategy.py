import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Ichimoku, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ichimoku_rsi_strategy(Strategy):
    """
    Ichimoku RSI strategy.
    The cloud gives the trend: Senkou Span A above Senkou Span B is an uptrend, below it a downtrend.
    In an uptrend RSI recovering from oversold, crossing back above RsiOversold, goes long; in a downtrend RSI falling from overbought,
    crossing back below RsiOverbought, goes short. An opposite signal reverses the position, and a percent stop limits the loss.
    """

    def __init__(self):
        super(ichimoku_rsi_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span B Period", "Period of Senkou Span B", "Ichimoku")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level a long recovers from", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level a short falls from", "RSI")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi = None

    def OnReseted(self):
        super(ichimoku_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ichimoku_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_b_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, rsi, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, ichimoku_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if prev_rsi is None:
            return

        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if senkou_a is None or senkou_b is None:
            return

        oversold = Decimal(self._rsi_oversold.Value)
        overbought = Decimal(self._rsi_overbought.Value)

        signal = 0
        if senkou_a > senkou_b and prev_rsi < oversold and rsi >= oversold:
            signal = 1
        elif senkou_a < senkou_b and prev_rsi > overbought and rsi <= overbought:
            signal = -1

        if signal == 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ichimoku_rsi_strategy()
