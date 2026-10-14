import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

RANGE_LENGTH = 50

class fibonacci_swing_trading_bot_strategy(Strategy):
    """
    Fibonacci Swing Trading Bot strategy.
    Retracement levels FiboLevel1 and FiboLevel2 are measured down from the highest high of the last 50 candles towards the
    lowest low. A close breaking above the FiboLevel1 level goes long and a close breaking below the FiboLevel2 level goes short,
    reversing an opposite position. Each position is protected by a percent stop loss and a take profit RiskRewardRatio times
    further away.
    """

    def __init__(self):
        super(fibonacci_swing_trading_bot_strategy, self).__init__()
        self._fibo_level1 = self.Param("FiboLevel1", 0.618).SetDisplay("Fibo Level 1", "Retracement ratio of the long breakout level", "Fibonacci")
        self._fibo_level2 = self.Param("FiboLevel2", 0.786).SetDisplay("Fibo Level 2", "Retracement ratio of the short breakout level", "Fibonacci")
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 2.0).SetNotNegative().SetDisplay("Risk/Reward", "Take profit distance as a multiple of the stop distance", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_level1 = None
        self._prev_level2 = None

    def OnReseted(self):
        super(fibonacci_swing_trading_bot_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(fibonacci_swing_trading_bot_strategy, self).OnStarted2(time)

        self._reset_state()

        highest = Highest()
        highest.Length = RANGE_LENGTH
        lowest = Lowest()
        lowest.Length = RANGE_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        take = stop * float(self._risk_reward_ratio.Value)
        self.StartProtection(Unit(Decimal(take), UnitTypes.Percent), Unit(Decimal(stop), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop and target have to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return

        high = float(high_value)
        low = float(low_value)
        rng = high - low
        level1 = high - rng * float(self._fibo_level1.Value)
        level2 = high - rng * float(self._fibo_level2.Value)
        close = float(candle.ClosePrice)

        last_close = self._prev_close
        last_level1 = self._prev_level1
        last_level2 = self._prev_level2

        self._prev_close = close
        self._prev_level1 = level1
        self._prev_level2 = level2

        if last_close is None or last_level1 is None or last_level2 is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if last_close <= last_level1 and close > level1 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif last_close >= last_level2 and close < level2 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return fibonacci_swing_trading_bot_strategy()
