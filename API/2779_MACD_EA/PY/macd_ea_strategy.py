import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_ea_strategy(Strategy):

    def __init__(self):
        super(macd_ea_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 55)
        self._slow_period = self.Param("SlowPeriod", 69)
        self._signal_period = self.Param("SignalPeriod", 90)
        self._stop_loss_pips = self.Param("StopLossPips", 80)
        self._take_profit_pips = self.Param("TakeProfitPips", 500)
        self._partial_profit_pips = self.Param("PartialProfitPips", 70)
        self._breakeven_pips = self.Param("BreakevenPips", 0)
        self._use_money_management = self.Param("UseMoneyManagement", False)
        self._risk_multiplier = self.Param("RiskMultiplier", 1.0)
        self._base_volume = self.Param("BaseVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._macd = None
        self._macd_diffs = []
        self._entry_price = None
        self._current_position_volume = 0.0
        self._entry_direction = 0
        self._partial_taken = False
        self._breakeven_active = False
        self._trade_pnl = 0.0
        self._consecutive_losses = 0

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def PartialProfitPips(self):
        return self._partial_profit_pips.Value

    @property
    def BreakevenPips(self):
        return self._breakeven_pips.Value

    @property
    def UseMoneyManagement(self):
        return self._use_money_management.Value

    @property
    def RiskMultiplier(self):
        return self._risk_multiplier.Value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(macd_ea_strategy, self).OnStarted(time)
        self.Volume = float(self.BaseVolume)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.FastPeriod
        self._macd.Macd.LongMa.Length = self.SlowPeriod
        self._macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ = CandleIndicatorValue(self._macd, candle)
        civ.IsFinal = True
        result = self._macd.Process(civ)
        if not self._macd.IsFormed:
            return

        try:
            macd_line = float(result.Macd) if result.Macd is not None else 0.0
            signal_line = float(result.Signal) if result.Signal is not None else 0.0
        except:
            return

        diff = macd_line - signal_line
        self._macd_diffs.append(diff)
        if len(self._macd_diffs) > 50:
            self._macd_diffs = self._macd_diffs[-50:]

        if len(self._macd_diffs) < 5:
            return

        diff_two = self._macd_diffs[-3]
        diff_four = self._macd_diffs[-5]

        bullish = diff_two > 0 and diff_four < 0
        bearish = diff_two < 0 and diff_four > 0

        pip = self._get_pip_size()
        pos = float(self.Position)

        if pos > 0:
            if self._handle_long_position(candle, bearish, pip):
                return
        elif pos < 0:
            if self._handle_short_position(candle, bullish, pip):
                return

        if float(self.Position) != 0:
            return

        volume = self._calculate_order_volume()
        if volume <= 0:
            return

        if bullish:
            self.BuyMarket(volume)
            self._init_trade_state(float(candle.ClosePrice), volume, 1)
        elif bearish:
            self.SellMarket(volume)
            self._init_trade_state(float(candle.ClosePrice), volume, -1)

    def _handle_long_position(self, candle, bearish_signal, pip):
        if self._entry_price is None:
            return False
        entry = self._entry_price
        remaining = self._current_position_volume if self._current_position_volume > 0 else abs(float(self.Position))
        if remaining <= 0:
            return False

        stop = entry - self.StopLossPips * pip if self.StopLossPips > 0 else None
        take = entry + self.TakeProfitPips * pip if self.TakeProfitPips > 0 else None
        partial = entry + self.PartialProfitPips * pip if self.PartialProfitPips > 0 else None
        breakeven = entry + self.BreakevenPips * pip if self.BreakevenPips > 0 else None

        if stop is not None and float(candle.LowPrice) <= stop:
            self.SellMarket(remaining)
            self._register_pnl(stop, remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        if take is not None and float(candle.HighPrice) >= take:
            self.SellMarket(remaining)
            self._register_pnl(take, remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        if not self._partial_taken and partial is not None and float(candle.HighPrice) >= partial:
            half = remaining / 2.0
            if half > 0:
                self.SellMarket(half)
                self._register_pnl(partial, half)
                self._current_position_volume = max(0.0, self._current_position_volume - half)
                self._partial_taken = True
                return True

        if breakeven is not None and not self._breakeven_active and float(candle.HighPrice) >= breakeven:
            self._breakeven_active = True

        if self._breakeven_active and float(candle.LowPrice) <= entry:
            self.SellMarket(remaining)
            self._register_pnl(entry, remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        if bearish_signal:
            self.SellMarket(remaining)
            self._register_pnl(float(candle.ClosePrice), remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        return False

    def _handle_short_position(self, candle, bullish_signal, pip):
        if self._entry_price is None:
            return False
        entry = self._entry_price
        remaining = self._current_position_volume if self._current_position_volume > 0 else abs(float(self.Position))
        if remaining <= 0:
            return False

        stop = entry + self.StopLossPips * pip if self.StopLossPips > 0 else None
        take = entry - self.TakeProfitPips * pip if self.TakeProfitPips > 0 else None
        partial = entry - self.PartialProfitPips * pip if self.PartialProfitPips > 0 else None
        breakeven = entry - self.BreakevenPips * pip if self.BreakevenPips > 0 else None

        if stop is not None and float(candle.HighPrice) >= stop:
            self.BuyMarket(remaining)
            self._register_pnl(stop, remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        if take is not None and float(candle.LowPrice) <= take:
            self.BuyMarket(remaining)
            self._register_pnl(take, remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        if not self._partial_taken and partial is not None and float(candle.LowPrice) <= partial:
            half = remaining / 2.0
            if half > 0:
                self.BuyMarket(half)
                self._register_pnl(partial, half)
                self._current_position_volume = max(0.0, self._current_position_volume - half)
                self._partial_taken = True
                return True

        if breakeven is not None and not self._breakeven_active and float(candle.LowPrice) <= breakeven:
            self._breakeven_active = True

        if self._breakeven_active and float(candle.HighPrice) >= entry:
            self.BuyMarket(remaining)
            self._register_pnl(entry, remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        if bullish_signal:
            self.BuyMarket(remaining)
            self._register_pnl(float(candle.ClosePrice), remaining)
            self._current_position_volume = 0.0
            self._finalize_trade()
            return True

        return False

    def _init_trade_state(self, entry_price, volume, direction):
        self._entry_price = entry_price
        self._current_position_volume = abs(volume)
        self._entry_direction = direction
        self._partial_taken = False
        self._breakeven_active = False
        self._trade_pnl = 0.0

    def _calculate_order_volume(self):
        volume = float(self.BaseVolume)
        if self.UseMoneyManagement:
            losses = self._consecutive_losses
            if losses == 0:
                mult = 1.0
            elif losses == 1:
                mult = 2.0
            elif losses == 2:
                mult = 3.0
            elif losses == 3:
                mult = 4.0
            elif losses == 4:
                mult = 5.0
            elif losses == 5:
                mult = 6.0
            else:
                mult = 7.0
            volume *= mult * float(self.RiskMultiplier)
        return volume

    def _get_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0:
            return 1.0
        tmp = step
        decimals = 0
        while decimals < 10 and int(tmp) != tmp:
            tmp *= 10.0
            decimals += 1
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def _register_pnl(self, exit_price, volume):
        if self._entry_price is None or self._entry_direction == 0:
            return
        pnl = (exit_price - self._entry_price) * volume * self._entry_direction
        self._trade_pnl += pnl

    def _finalize_trade(self):
        if self._current_position_volume > 0:
            return
        if self._trade_pnl > 0:
            self._consecutive_losses = 0
        elif self._trade_pnl < 0:
            self._consecutive_losses += 1
        else:
            self._consecutive_losses = 0
        self._reset_trade_state()

    def _reset_trade_state(self):
        self._entry_price = None
        self._current_position_volume = 0.0
        self._entry_direction = 0
        self._partial_taken = False
        self._breakeven_active = False
        self._trade_pnl = 0.0

    def OnReseted(self):
        super(macd_ea_strategy, self).OnReseted()
        self._macd_diffs = []
        self._entry_price = None
        self._current_position_volume = 0.0
        self._entry_direction = 0
        self._partial_taken = False
        self._breakeven_active = False
        self._trade_pnl = 0.0
        self._consecutive_losses = 0

    def CreateClone(self):
        return macd_ea_strategy()
