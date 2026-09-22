import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import SmoothedMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security
from StockSharp.Messages import CandleStates, DataType
from System import TimeSpan

# The bundled sample history ships this instrument next to the primary one, so the hedge leg
# has a tradable default.
DEFAULT_HEDGE_SECURITY_ID = "TONUSDT@BNBFT"


class improve_ma_rsi_hedge_strategy(Strategy):
    """Dual smoothed moving average and RSI hedge strategy converted from Improve.mq5."""

    def __init__(self):
        super(improve_ma_rsi_hedge_strategy, self).__init__()

        default_hedge = Security()
        default_hedge.Id = DEFAULT_HEDGE_SECURITY_ID

        self._profit_target = self.Param("ProfitTarget", 50.0)
        self._hedge_security = self.Param[Security]("HedgeSecurity", default_hedge)
        self._fast_period = self.Param("FastMaPeriod", 8)
        self._slow_period = self.Param("SlowMaPeriod", 21)
        self._rsi_period = self.Param("RsiPeriod", 21)
        self._oversold_level = self.Param("OversoldLevel", 30.0)
        self._overbought_level = self.Param("OverboughtLevel", 70.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._fast_ma = None
        self._slow_ma = None
        self._rsi = None
        self._base_last_close = 0.0
        self._hedge_last_close = 0.0
        self._base_entry_price = 0.0
        self._hedge_entry_price = 0.0
        self._has_base_close = False
        self._has_hedge_close = False
        self._pair_direction = 0

    @property
    def ProfitTarget(self):
        return self._profit_target.Value

    @property
    def HedgeSecurity(self):
        return self._hedge_security.Value

    @HedgeSecurity.setter
    def HedgeSecurity(self, value):
        self._hedge_security.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        securities = []

        if self.Security is not None:
            securities.append((self.Security, self.CandleType))

        if self.HedgeSecurity is not None:
            securities.append((self.HedgeSecurity, self.CandleType))

        return securities

    def OnStarted2(self, time):
        super(improve_ma_rsi_hedge_strategy, self).OnStarted2(time)

        if self.Security is None:
            raise Exception("Primary security must be specified.")

        if self.HedgeSecurity is None:
            raise Exception("Hedge security must be specified.")

        if self.FastMaPeriod >= self.SlowMaPeriod:
            raise Exception("Fast MA period must be less than slow MA period.")

        self._fast_ma = SmoothedMovingAverage()
        self._fast_ma.Length = self.FastMaPeriod
        self._slow_ma = SmoothedMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        base_subscription = self.SubscribeCandles(self.CandleType)
        base_subscription.Bind(self._fast_ma, self._slow_ma, self._rsi, self._process_base_candle).Start()

        # The hedge leg only contributes its closing price, so it needs no indicators.
        hedge_subscription = self.SubscribeCandles(self.CandleType, False, self.HedgeSecurity)
        hedge_subscription.Bind(self._process_hedge_candle).Start()

    def _process_base_candle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        self._base_last_close = float(candle.ClosePrice)
        self._has_base_close = True

        self._check_profit_target()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed or not self._rsi.IsFormed:
            return

        # Both legs are opened together, so a new pair starts only from a flat state.
        if self._pair_direction != 0:
            return

        # The hedge entry price comes from its own candle, so wait until one arrived.
        if not self._has_hedge_close:
            return

        fast_value = float(fast_val)
        slow_value = float(slow_val)
        rsi_value = float(rsi_val)

        if slow_value > fast_value and rsi_value <= self.OversoldLevel:
            self._open_pair(1)
        elif slow_value < fast_value and rsi_value >= self.OverboughtLevel:
            self._open_pair(-1)

    def _process_hedge_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._hedge_last_close = float(candle.ClosePrice)
        self._has_hedge_close = True

        self._check_profit_target()

    def _open_pair(self, direction):
        if direction == 0:
            return

        if self._position_of(self.Security) != 0 or self._position_of(self.HedgeSecurity) != 0:
            return

        volume = self.Volume

        # The hedge leg mirrors the primary direction instead of offsetting it.
        if direction > 0:
            self.BuyMarket(volume, self.Security)
            self.BuyMarket(volume, self.HedgeSecurity)
        else:
            self.SellMarket(volume, self.Security)
            self.SellMarket(volume, self.HedgeSecurity)

        self._pair_direction = direction
        self._base_entry_price = self._base_last_close
        self._hedge_entry_price = self._hedge_last_close

    def _check_profit_target(self):
        if self._pair_direction == 0 or not self._has_base_close or not self._has_hedge_close:
            return

        volume = float(self.Volume)

        if self._pair_direction > 0:
            base_profit = (self._base_last_close - self._base_entry_price) * volume
            hedge_profit = (self._hedge_last_close - self._hedge_entry_price) * volume
        else:
            base_profit = (self._base_entry_price - self._base_last_close) * volume
            hedge_profit = (self._hedge_entry_price - self._hedge_last_close) * volume

        if base_profit + hedge_profit >= self.ProfitTarget:
            self._close_pair()

    def _close_pair(self):
        base_position = self._position_of(self.Security)
        if base_position > 0:
            self.SellMarket(base_position, self.Security)
        elif base_position < 0:
            self.BuyMarket(abs(base_position), self.Security)

        hedge_position = self._position_of(self.HedgeSecurity)
        if hedge_position > 0:
            self.SellMarket(hedge_position, self.HedgeSecurity)
        elif hedge_position < 0:
            self.BuyMarket(abs(hedge_position), self.HedgeSecurity)

        self._pair_direction = 0
        self._base_entry_price = 0.0
        self._hedge_entry_price = 0.0

    def _position_of(self, security):
        # A leg without a position reports None, which is the same as being flat.
        position = self.GetPositionValue(security, self.Portfolio)
        return 0 if position is None else position

    def OnReseted(self):
        super(improve_ma_rsi_hedge_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._rsi = None
        self._base_last_close = 0.0
        self._hedge_last_close = 0.0
        self._base_entry_price = 0.0
        self._hedge_entry_price = 0.0
        self._has_base_close = False
        self._has_hedge_close = False
        self._pair_direction = 0

    def CreateClone(self):
        return improve_ma_rsi_hedge_strategy()
