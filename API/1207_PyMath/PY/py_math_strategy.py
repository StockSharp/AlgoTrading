import math


POS_INF_PROXY = 1e300


def is_inf(value):
    return math.isinf(value) or abs(value) >= POS_INF_PROXY


def is_finite(value):
    return not math.isnan(value) and not is_inf(value)


def fmod(value, divisor):
    if math.isnan(value) or math.isnan(divisor) or divisor == 0.0:
        return float('nan')
    if math.isinf(divisor):
        return value if is_finite(value) else float('nan')
    if math.isinf(value):
        return float('nan')
    return value - math.trunc(value / divisor) * divisor
