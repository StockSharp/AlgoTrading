# Estrategia de Cruce de Ichimoku Chinkou
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce del Ichimoku Chinkou Span (línea rezagada) con el precio.

## Lógica de la estrategia

- **Largo:** Chinkou cruza por encima del precio, tanto el precio actual como Chinkou están por encima de la nube Kumo, y el RSI está por encima de `RsiBuyLevel`.
- **Corto:** Chinkou cruza por debajo del precio, tanto el precio actual como Chinkou están por debajo de la nube Kumo, y el RSI está por debajo de `RsiSellLevel`.

La estrategia utiliza protección de stop-loss mediante `StartProtection` y parámetros para Tenkan, Kijun, Senkou Span B y RSI.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `TenkanPeriod` | Período de Tenkan-sen | 9 |
| `KijunPeriod` | Período de Kijun-sen | 26 |
| `SenkouSpanPeriod` | Período de Senkou Span B | 52 |
| `RsiPeriod` | Período de cálculo del RSI | 14 |
| `RsiBuyLevel` | RSI mínimo para largos | 70 |
| `RsiSellLevel` | RSI máximo para cortos | 30 |
| `StopLoss` | Porcentaje o valor del stop-loss | 2% |
| `CandleType` | Tipo de vela para la suscripción | Velas de 5 minutos |

## Indicadores

- Ichimoku
- RSI
