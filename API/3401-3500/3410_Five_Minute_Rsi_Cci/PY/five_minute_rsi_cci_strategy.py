import math
import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex, CommodityChannelIndex, SmoothedMovingAverage,
    ExponentialMovingAverage, CandleIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class five_minute_rsi_cci_strategy(Strategy):
    def __init__(self):
        super(five_minute_rsi_cci_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._fast_smma_period = self.Param("FastSmmaPeriod", 2)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 6)
        self._fast_cci_period = self.Param("FastCciPeriod", 34)
        self._slow_cci_period = self.Param("SlowCciPeriod", 175)
        self._bullish_rsi_level = self.Param("BullishRsiLevel", 55.0)
        self._bearish_rsi_level = self.Param("BearishRsiLevel", 45.0)
        self._stop_loss_points = self.Param("StopLossPoints", 60.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 20.0)
        self._lot_coefficient = self.Param("LotCoefficient", 0.01)
        self._equity_divisor = self.Param("EquityDivisor", 10.0)
        self._max_spread_points = self.Param("MaxSpreadPoints", 18.0)
        self._reset_runtime()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def FastSmmaPeriod(self):
        return self._fast_smma_period.Value

    @FastSmmaPeriod.setter
    def FastSmmaPeriod(self, value):
        self._fast_smma_period.Value = value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @SlowEmaPeriod.setter
    def SlowEmaPeriod(self, value):
        self._slow_ema_period.Value = value

    @property
    def FastCciPeriod(self):
        return self._fast_cci_period.Value

    @FastCciPeriod.setter
    def FastCciPeriod(self, value):
        self._fast_cci_period.Value = value

    @property
    def SlowCciPeriod(self):
        return self._slow_cci_period.Value

    @SlowCciPeriod.setter
    def SlowCciPeriod(self, value):
        self._slow_cci_period.Value = value

    @property
    def BullishRsiLevel(self):
        return self._bullish_rsi_level.Value

    @BullishRsiLevel.setter
    def BullishRsiLevel(self, value):
        self._bullish_rsi_level.Value = value

    @property
    def BearishRsiLevel(self):
        return self._bearish_rsi_level.Value

    @BearishRsiLevel.setter
    def BearishRsiLevel(self, value):
        self._bearish_rsi_level.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def LotCoefficient(self):
        return self._lot_coefficient.Value

    @LotCoefficient.setter
    def LotCoefficient(self, value):
        self._lot_coefficient.Value = value

    @property
    def EquityDivisor(self):
        return self._equity_divisor.Value

    @EquityDivisor.setter
    def EquityDivisor(self, value):
        self._equity_divisor.Value = value

    @property
    def MaxSpreadPoints(self):
        return self._max_spread_points.Value

    @MaxSpreadPoints.setter
    def MaxSpreadPoints(self, value):
        self._max_spread_points.Value = value

    def _reset_runtime(self):
        self._previous_rsi = None
        self._best_bid = None
        self._best_ask = None
        self._entry_price = None
        self._trailing_stop = None
        self._exit_requested = False

    def OnReseted(self):
        super(five_minute_rsi_cci_strategy, self).OnReseted()
        self._reset_runtime()

    def _point(self):
        if self.Security is None or self.Security.PriceStep is None:
            raise ValueError("Positive Security.PriceStep is required.")
        step = float(self.Security.PriceStep)
        if step <= 0:
            raise ValueError("Positive Security.PriceStep is required.")
        return step * 10.0 if self.Security.Decimals in (3, 5) else step

    def OnStarted2(self, time):
        super(five_minute_rsi_cci_strategy, self).OnStarted2(time)
        self._reset_runtime()
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._smma = SmoothedMovingAverage()
        self._smma.Length = self.FastSmmaPeriod
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.SlowEmaPeriod
        self._fast_cci = CommodityChannelIndex()
        self._fast_cci.Length = self.FastCciPeriod
        self._slow_cci = CommodityChannelIndex()
        self._slow_cci.Length = self.SlowCciPeriod

        point = self._point()
        stop = Unit(Decimal(self.StopLossPoints * point), UnitTypes.Absolute) if self.StopLossPoints > 0 else Unit()
        take = Unit(Decimal(self.TakeProfitPoints * point), UnitTypes.Absolute) if self.TakeProfitPoints > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True, isLocalStop=True)
        self.SubscribeLevel1().Bind(self._process_level1).Start()
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()

    def _process_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None and bid > 0:
            self._best_bid = float(bid)
        if ask is not None and ask > 0:
            self._best_ask = float(ask)
        if self.Position == 0 or self._entry_price is None or self.TrailingStopPoints <= 0 or self._exit_requested:
            return

        gap = float(self.TrailingStopPoints) * self._point()
        price = self._best_bid if self.Position > 0 else self._best_ask
        if price is None:
            return

        if self.Position > 0:
            if price >= self._entry_price + gap:
                candidate = price - gap
                if self._trailing_stop is None or candidate > self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price <= self._trailing_stop:
                self._request_exit()
        else:
            if price <= self._entry_price - gap:
                candidate = price + gap
                if self._trailing_stop is None or candidate < self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price >= self._trailing_stop:
                self._request_exit()

    def _request_exit(self):
        if self._exit_requested or self.Position == 0:
            return
        self._exit_requested = True
        self.ClosePosition()

    def OnOwnTradeReceived(self, trade):
        super(five_minute_rsi_cci_strategy, self).OnOwnTradeReceived(trade)
        if self.Position == 0:
            self._entry_price = None
            self._trailing_stop = None
            self._exit_requested = False
        elif self._entry_price is None:
            self._entry_price = float(trade.Trade.Price)

    def _spread_allowed(self):
        return (self._best_bid is not None and self._best_ask is not None
                and self._best_ask >= self._best_bid
                and self._best_ask - self._best_bid < float(self.MaxSpreadPoints) * self._point())

    def _volume(self):
        portfolio = self.Portfolio
        if portfolio is None:
            return None
        equity = float(portfolio.CurrentValue or portfolio.BeginValue or 0)
        divisor = float(self.EquityDivisor)
        if equity <= 0 or divisor <= 0:
            return None
        raw = float(self.LotCoefficient) * math.sqrt(equity / divisor)
        minimum = float(self.Security.MinVolume or 0)
        maximum = float(self.Security.MaxVolume) if self.Security.MaxVolume is not None else float("inf")
        step = float(self.Security.VolumeStep or 0)
        if minimum > maximum:
            return None
        if step > 0:
            first = math.ceil(minimum / step) * step
            last = math.floor(maximum / step) * step if math.isfinite(maximum) else float("inf")
            if first > last:
                return None
            raw = max(first, min(last, math.floor(raw / step) * step))
        else:
            raw = max(minimum, min(maximum, raw))
        return Decimal.Parse(str(raw)) if raw > 0 and math.isfinite(raw) else None

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        rsi = process_float(self._rsi, candle.ClosePrice, candle.OpenTime, True)
        smma = process_float(self._smma, candle.OpenPrice, candle.OpenTime, True)
        ema = process_float(self._ema, candle.OpenPrice, candle.OpenTime, True)
        fast_in = CandleIndicatorValue(self._fast_cci, candle)
        fast_in.IsFinal = True
        slow_in = CandleIndicatorValue(self._slow_cci, candle)
        slow_in.IsFinal = True
        fast_cci = self._fast_cci.Process(fast_in)
        slow_cci = self._slow_cci.Process(slow_in)

        if not all((rsi.IsFormed, smma.IsFormed, ema.IsFormed, fast_cci.IsFormed, slow_cci.IsFormed)):
            return

        current = float(rsi)
        previous = self._previous_rsi
        if previous is not None and self.Position == 0 and not self._exit_requested and self._spread_allowed():
            volume = self._volume()
            if volume is not None:
                if (previous <= float(self.BullishRsiLevel) and current > float(self.BullishRsiLevel)
                        and float(smma) > float(ema)
                        and float(fast_cci) > 0 and float(slow_cci) > 0):
                    self.BuyMarket(volume)
                elif (previous >= float(self.BearishRsiLevel) and current < float(self.BearishRsiLevel)
                      and float(smma) < float(ema)
                      and float(fast_cci) < 0 and float(slow_cci) < 0):
                    self.SellMarket(volume)

        self._previous_rsi = current

    def CreateClone(self):
        return five_minute_rsi_cci_strategy()
