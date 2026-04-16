import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, BollingerBandsValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from datatype_extensions import *
from indicator_extensions import *


class ratio_pairs_trading_strategy(Strategy):
    """
    Ratio-based pairs trading strategy.
    Trades the price ratio (Asset1 / Asset2) of two correlated instruments.
    Enters when the z-score of the ratio exceeds the entry threshold and
    exits when the ratio reverts toward its rolling mean.
    """

    def __init__(self):
        super(ratio_pairs_trading_strategy, self).__init__()

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Rolling window for ratio statistics", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 60, 5)

        self._entry_z_score = self.Param("EntryZScore", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Z-Score", "Z-score threshold to open a pair position", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.25)

        self._exit_z_score = self.Param("ExitZScore", 0.5) \
            .SetNotNegative() \
            .SetDisplay("Exit Z-Score", "Z-score threshold to close the pair", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 1.0, 0.1)

        self._hedge_ratio = self.Param("HedgeRatio", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Hedge Ratio", "Volume multiplier for the second leg", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.1)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Protective stop per leg, in percent", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 1.0)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe used for both securities", "General")

        self._second_security = self.Param[Security]("SecondSecurity", None) \
            .SetDisplay("Second Security", "Second security in the pair", "General") \
            .SetRequired()

        # Internal state
        self._ratio_bands = None
        self._first_price = 0.0
        self._second_price = 0.0
        self._first_time = None
        self._second_time = None

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def EntryZScore(self):
        return self._entry_z_score.Value

    @EntryZScore.setter
    def EntryZScore(self, value):
        self._entry_z_score.Value = value

    @property
    def ExitZScore(self):
        return self._exit_z_score.Value

    @ExitZScore.setter
    def ExitZScore(self, value):
        self._exit_z_score.Value = value

    @property
    def HedgeRatio(self):
        return self._hedge_ratio.Value

    @HedgeRatio.setter
    def HedgeRatio(self, value):
        self._hedge_ratio.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SecondSecurity(self):
        return self._second_security.Value

    @SecondSecurity.setter
    def SecondSecurity(self, value):
        self._second_security.Value = value

    def GetWorkingSecurities(self):
        return [
            (self.Security, self.CandleType),
            (self.SecondSecurity, self.CandleType),
        ]

    def OnReseted(self):
        super(ratio_pairs_trading_strategy, self).OnReseted()
        self._ratio_bands = None
        self._first_price = 0.0
        self._second_price = 0.0
        self._first_time = None
        self._second_time = None

    def OnStarted2(self, time):
        super(ratio_pairs_trading_strategy, self).OnStarted2(time)

        if self.SecondSecurity is None:
            raise Exception("Second security is not specified.")

        self._ratio_bands = BollingerBands()
        self._ratio_bands.Length = self.LookbackPeriod
        self._ratio_bands.Width = self.EntryZScore

        first_subscription = self.SubscribeCandles(self.CandleType)
        second_subscription = self.SubscribeCandles(self.CandleType, self.SecondSecurity)

        first_subscription.Bind(self.ProcessFirstCandle).Start()
        second_subscription.Bind(self.ProcessSecondCandle).Start()

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, first_subscription)
            self.DrawOwnTrades(area)

    def ProcessFirstCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._first_price = float(candle.ClosePrice)
        self._first_time = candle.OpenTime
        self.TryProcessPair()

    def ProcessSecondCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._second_price = float(candle.ClosePrice)
        self._second_time = candle.OpenTime
        self.TryProcessPair()

    def TryProcessPair(self):
        if self._first_price <= 0 or self._second_price <= 0:
            return

        # Wait until both legs align on the same bar to avoid stale pricing.
        if self._first_time != self._second_time:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ratio = self._first_price / self._second_price

        value = process_float(self._ratio_bands, ratio, self._first_time, True)

        if not self._ratio_bands.IsFormed or value.IsEmpty:
            return

        bands = value
        if bands.MovingAverage is None or bands.UpBand is None or bands.LowBand is None:
            return

        mean = float(bands.MovingAverage)
        upper = float(bands.UpBand)
        lower = float(bands.LowBand)

        # Half the band width equals one standard deviation times EntryZScore.
        denominator = 2.0 * float(self.EntryZScore)
        if denominator == 0:
            return
        std_dev = (upper - lower) / denominator
        if std_dev <= 0:
            return

        z_score = (ratio - mean) / std_dev
        abs_z = Math.Abs(z_score)

        # Exit zone: z-score inside reversion band and pair position is open.
        if abs_z <= float(self.ExitZScore) and self.Position != 0:
            self.ClosePair()
            return

        # Ratio too high => first leg overpriced vs. second: short first, long second.
        if z_score >= float(self.EntryZScore) and self.Position >= 0:
            self.EnterShortFirst()
        # Ratio too low => first leg underpriced vs. second: long first, short second.
        elif z_score <= -float(self.EntryZScore) and self.Position <= 0:
            self.EnterLongFirst()

    def _get_second_position(self):
        value = self.GetPositionValue(self.SecondSecurity, self.Portfolio)
        return float(value) if value is not None else 0.0

    def EnterLongFirst(self):
        # Flip first leg from any short / flat to long of size Volume.
        first_volume = self.Volume + Math.Abs(self.Position)
        self.BuyMarket(first_volume)

        # Flip second leg to short of size Volume * HedgeRatio.
        target_second = self.Volume * self.HedgeRatio
        second_volume = target_second + max(0.0, self._get_second_position())
        if second_volume > 0:
            self.SellMarket(second_volume, self.SecondSecurity)

        self.LogInfo("Long pair: +{0} {1} / -{2} {3}".format(
            self.Volume, self.Security.Id if self.Security else "",
            target_second, self.SecondSecurity.Id if self.SecondSecurity else ""))

    def EnterShortFirst(self):
        # Flip first leg from any long / flat to short of size Volume.
        first_volume = self.Volume + Math.Abs(self.Position)
        self.SellMarket(first_volume)

        # Flip second leg to long of size Volume * HedgeRatio.
        target_second = self.Volume * self.HedgeRatio
        second_volume = target_second + max(0.0, -self._get_second_position())
        if second_volume > 0:
            self.BuyMarket(second_volume, self.SecondSecurity)

        self.LogInfo("Short pair: -{0} {1} / +{2} {3}".format(
            self.Volume, self.Security.Id if self.Security else "",
            target_second, self.SecondSecurity.Id if self.SecondSecurity else ""))

    def ClosePair(self):
        first_pos = self.Position
        if first_pos > 0:
            self.SellMarket(first_pos)
        elif first_pos < 0:
            self.BuyMarket(-first_pos)

        second_pos = self._get_second_position()
        if second_pos > 0:
            self.SellMarket(second_pos, self.SecondSecurity)
        elif second_pos < 0:
            self.BuyMarket(-second_pos, self.SecondSecurity)

        self.LogInfo("Pair closed: ratio returned to mean.")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ratio_pairs_trading_strategy()
