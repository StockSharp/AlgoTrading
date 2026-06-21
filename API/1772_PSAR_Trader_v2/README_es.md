# Estrategia PSAR Trader v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia opera reversiones del mercado usando el indicador Parabolic SAR. Se abre una posición cuando el valor del SAR cambia de lado con respecto al precio, señalando un posible cambio de tendencia. El algoritmo opera únicamente dentro de una ventana temporal especificada y puede opcionalmente cerrar una posición existente cuando aparece una señal opuesta.

## Lógica de la estrategia
- **Indicador**: Parabolic SAR.
- **Compra** cuando el SAR se desplaza por debajo del cierre de la vela tras haber estado por encima de la vela anterior.
- **Venta** cuando el SAR se desplaza por encima del cierre de la vela tras haber estado por debajo de la vela anterior.
- Opera solo en el rango `StartHour`–`EndHour`.
- Cuando `CloseOnOppositeSignal` está habilitado, la posición se cierra si aparece una señal opuesta antes de abrir una nueva.

### Gestión del riesgo
Al entrar en una posición, la estrategia establece niveles internos de take-profit y stop-loss. La posición se cierra automáticamente si el precio toca cualquiera de los niveles.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| `CandleType` | Marco temporal de las velas utilizadas para operar. |
| `Step` | Paso de aceleración del Parabolic SAR. |
| `Maximum` | Factor de aceleración máximo del Parabolic SAR. |
| `TakeProfit` | Objetivo de ganancia en unidades de precio. |
| `StopLoss` | Stop loss en unidades de precio. |
| `StartHour` | Hora de inicio de la operativa (0–23). |
| `EndHour` | Hora de fin de la operativa (0–23). |
| `CloseOnOppositeSignal` | Cerrar la posición actual cuando aparezca una señal opuesta. |

## Notas
Este ejemplo demuestra el uso básico de la API de alto nivel con un popular indicador de reversión de tendencia. Ajuste los parámetros y la gestión del riesgo según el instrumento operado y las preferencias personales.
