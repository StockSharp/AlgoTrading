# Estrategia Volume Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
- Port del asesor experto de MetaTrader 5 **"Volume trader" (ID 21050)** de Vladimir Karputov.
- Recreado sobre la API de estrategia de alto nivel de StockSharp.
- Opera en la dirección del cambio más reciente de volumen de tick mientras un filtro de sesión de trading personalizado está activo.

## Lógica de Trading
1. Se suscribe a las velas definidas por `CandleType` (predeterminado: marco temporal de 1 hora) y lee su volumen de tick (`TotalVolume`).
2. En cada vela finalizada, la estrategia compara los volúmenes de las **dos** velas cerradas previas, emulando el script MQL5 que se ejecuta al nacer una nueva barra.
3. Si el volumen más reciente es mayor que el anterior y no hay posición larga, la estrategia compra contratos de `Volume` y adicionalmente cubre una posición corta existente.
4. Si el volumen más reciente es menor que el anterior y no hay posición corta, la estrategia vende contratos de `Volume` y adicionalmente cierra una posición larga existente.
5. Las señales de trading se ignoran cuando la hora de apertura de la siguiente barra cae fuera de la ventana `[StartHour, EndHour]`. El rango predeterminado 09:00–18:00 replica las entradas originales.
6. No se define stop loss ni take profit por defecto; la estrategia simplemente se revierte ante la señal opuesta.

## Gestión de Órdenes
- Las órdenes de entrada se envían mediante `BuyMarket` o `SellMarket` para voltear la posición inmediatamente al inicio de una nueva vela.
- Cuando aparece una señal de reversión, la estrategia automáticamente negocia el tamaño absoluto de la posición más el `Volume` configurado, asegurando que la posición previa se cierre antes de que se abra una nueva.
- No hay lógica de sizing de posición incorporada más allá del parámetro fijo `Volume`.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Marco temporal de 1 hora | Serie de velas usada para calcular el volumen de tick. Ajustar para que coincida con el marco temporal usado en el experto original. |
| `StartHour` | 9 | Hora inclusiva (0–23) que marca el inicio de la sesión de trading. Las señales antes de esta hora se ignoran. |
| `EndHour` | 18 | Hora inclusiva (0–23) que marca el fin de la sesión de trading. Las señales después de esta hora se ignoran. |
| `Volume` | 0.1 | Volumen de orden para nuevas entradas. También se usa al voltear una posición existente. |

## Notas de Uso
- Asegurarse de que la fuente de datos proporciona volumen de tick en los mensajes de vela. Cuando solo está disponible el volumen real negociado, el comportamiento seguirá esos datos.
- Alinear el parámetro `CandleType` con el marco temporal del gráfico que se desea reproducir de MetaTrader.
- Considerar envolver la estrategia con gestión de riesgo externa (stop loss, take profit, límites de pérdida diaria) si lo requieren las reglas de trading.
- La estrategia llama a `LogInfo` cuando se abre una posición, facilitando la auditoría de decisiones de señal en el registro.

## Diferencias vs. Implementación MQL Original
- Usa el pipeline de suscripción de velas de StockSharp en lugar de llamar manualmente a `CopyTickVolume`.
- El filtrado de sesión se basa en el `CloseTime` de la vela finalizada (la hora de inicio de la siguiente barra) para mantenerse alineado con la lógica MQL que se ejecuta en la apertura de barra.
- La ejecución de órdenes se maneja a través de ayudantes API de alto nivel (`BuyMarket`, `SellMarket`) en lugar de llamadas directas a `CTrade`.
