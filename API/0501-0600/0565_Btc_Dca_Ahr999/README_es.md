# BTC DCA AHR999

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Usa velas diarias UTC terminadas. Con los últimos `Length` cierres positivos, incluido el actual, calcula `G=exp(mean(ln(C)))`. La edad de BTC son días enteros desde 2009-01-03: `F=10^(5.84*log10(age)-17.01)` y `AHR999=(C/G)*(C/F)`. Se exige la ventana completa.

Un lunes permitido compra con presupuesto `UsdInvest2` si el índice es estrictamente menor que 0.45; en otro caso usa `UsdInvest1` si es menor que 1.2. Otros valores, días y fechas no compran. El rango UTC incluye sus extremos y permite una decisión por vela diaria. Cantidad: `floor(budget/C/VolumeStep)*VolumeStep`; se omiten cantidades inferiores al mínimo. El presupuesto usa el cierre de señal y la ejecución a mercado puede variar. Las órdenes pendientes bloquean duplicados. Se acumula sin salida automática.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | `1d` |
| `Length` | `200` |
| `UsdInvest1` | `100` |
| `UsdInvest2` | `1000` |
| `StartDate` | `2024-02-01T00:00:00Z` |
| `EndDate` | `2025-12-31T00:00:00Z` |

## Referencias

- [AHR999 geometric-cost implementation](https://github.com/alibaba-web3/quant-master/blob/main/ahr999_index.py)
