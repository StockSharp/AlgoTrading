import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security


class crude_oil_predicts_equity_strategy(Strategy):
    """Strategy that holds equity when oil momentum is positive and equity is above trend."""

    def __init__(self):
        super(crude_oil_predicts_equity_strategy, self).__init__()

        self._oil_security_id = self.Param("OilSecurityId", "TONUSDT@BNBFT") \
            .SetDisplay("Oil Security Id", "Identifier of the crude-oil benchmark security", "General")

        self._lookback = self.Param("Lookback", 20) \
            .SetRange(5, 120) \
            .SetDisplay("Lookback", "Number of candles used to compute oil momentum", "Indicators")

        self._trend_length = self.Param("TrendLength", 20) \
            .SetRange(5, 120) \
            .SetDisplay("Trend Length", "Equity trend filter length", "Indicators")

        self._oil_threshold = self.Param("OilThreshold", 0.0) \
            .SetRange(-20.0, 20.0) \
            .SetDisplay("Oil Threshold", "Minimum oil momentum required to hold equity exposure", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetRange(0, 100) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series for both instruments", "General")

        self._oil_security = None
        self._oil_momentum = None
        self._equity_trend = None
        self._latest_equity_price = 0.0
        self._latest_equity_trend = 0.0
        self._latest_oil_momentum = 0.0
        self._equity_updated = False
        self._oil_updated = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        sec2_id = str(self._oil_security_id.Value)
        if sec2_id:
            s = Security()
            s.Id = sec2_id
            result.append((s, self.candle_type))
        return result

    def OnReseted(self):
        super(crude_oil_predicts_equity_strategy, self).OnReseted()
        self._oil_security = None
        self._oil_momentum = None
        self._equity_trend = None
        self._latest_equity_price = 0.0
        self._latest_equity_trend = 0.0
        self._latest_oil_momentum = 0.0
        self._equity_updated = False
        self._oil_updated = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(crude_oil_predicts_equity_strategy, self).OnStarted2(time)

        sec2_id = str(self._oil_security_id.Value)
        if not sec2_id:
            raise Exception("Oil security identifier is not specified.")

        s = Security()
        s.Id = sec2_id
        self._oil_security = s

        self._oil_momentum = RateOfChange()
        self._oil_momentum.Length = int(self._lookback.Value)
        self._equity_trend = SimpleMovingAverage()
        self._equity_trend.Length = int(self._trend_length.Value)

        equity_subscription = self.SubscribeCandles(self.candle_type, True, self.Security)
        oil_subscription = self.SubscribeCandles(self.candle_type, True, self._oil_security)

        equity_subscription.Bind(self.ProcessEquityCandle).Start()
        oil_subscription.Bind(self.ProcessOilCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, equity_subscription)
            self.DrawCandles(area, oil_subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessEquityCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._latest_equity_price = float(candle.ClosePrice)

        civ = CandleIndicatorValue(self._equity_trend, candle)
        civ.IsFinal = True
        trend_result = self._equity_trend.Process(civ)
        self._latest_equity_trend = float(trend_result)
        self._equity_updated = self._equity_trend.IsFormed
        self.TryProcessSignal()

    def ProcessOilCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ = CandleIndicatorValue(self._oil_momentum, candle)
        civ.IsFinal = True
        oil_result = self._oil_momentum.Process(civ)

        if not oil_result.IsEmpty and self._oil_momentum.IsFormed:
            self._latest_oil_momentum = float(oil_result)
            self._oil_updated = True
            self.TryProcessSignal()

    def TryProcessSignal(self):
        if not self._equity_updated or not self._oil_updated:
            return

        self._equity_updated = False
        self._oil_updated = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        oil_threshold = float(self._oil_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        bullish_signal = self._latest_oil_momentum > oil_threshold and self._latest_equity_price >= self._latest_equity_trend
        exit_signal = self._latest_oil_momentum <= oil_threshold or self._latest_equity_price < self._latest_equity_trend

        if self._cooldown_remaining == 0 and self.Position == 0 and bullish_signal:
            self.BuyMarket()
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and exit_signal:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return crude_oil_predicts_equity_strategy()
