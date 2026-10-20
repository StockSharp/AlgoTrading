# Binomial Option Pricing Model

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de valoración

Módulo de valoración de opciones Cox–Ross–Rubinstein de dos pasos, sin negociación ni órdenes. `CalculatePrice(spot, volatility, timeToExpiry)` devuelve el precio según ejercicio, call/put, estilo europeo/americano y clase de activo. Tasas y volatilidad son fracciones anuales: `0.05` significa 5%. El año tiene 365 días y el plazo admite minutos y horas.

Para `dt = T / 2`: `u = exp(volatility * sqrt(dt))`, `d = 1/u`, `p = (exp(b * dt) - d) / (u - d)`. `b = r - q` para `Stock`, `Index`, `Currency` (q: dividendo/tasa extranjera); `b = 0` para `Future`. Cada paso hacia atrás descuenta por `exp(-r * dt)`. El estilo europeo ejerce al vencimiento; el americano compara continuación e intrínseco en cada nodo. Plazo cero devuelve intrínseco; volatilidad cero sigue la trayectoria determinista con las mismas reglas. Clases/estilos no admitidos y probabilidades inválidas fallan explícitamente, sin recorte.

`EvaluatePrice(candle)` procesa cada vela terminada una sola vez. Tras `Length` cierres estima volatilidad anual con `SD poblacional(close) / mean(close) * sqrt(365 días / intervalo)`. Valora `TimeToExpiry`, actualiza `LastOptionPrice`, registra el valor y dibuja velas/desviación si existe gráfico. Es dispersión de niveles de precio, no volatilidad de rendimientos ni señal de negociación.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `StrikePrice` | `100` |
| `DividendYield` | `0` |
| `AssetClass` | `Stock` |
| `OptionStyle` | `European` |
| `OptionType` | `Call` |
| `TimeToExpiry` | `30d` |
| `Length` | `20` |
| `CandleType` | `1m` |
| `RiskFreeRate` | `0.05` |
