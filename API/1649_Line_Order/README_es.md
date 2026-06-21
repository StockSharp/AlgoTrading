# Estrategia de Orden en Línea
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Orden en Línea** dispara una orden de mercado cuando el precio cruza una línea horizontal definida por el usuario. Está diseñada como una conversión simplificada del script MQL original *LineOrder.mq4*, proporcionando funcionalidad de trading manual por líneas a través de la API de alto nivel de StockSharp.

La estrategia expone parámetros para controlar la dirección, el nivel de entrada y la gestión de riesgos. Después de entrar en una posición, los niveles opcionales de stop-loss, take-profit y trailing stop se monitorean en cada vela completada. La lógica es completamente event-driven y no mantiene colecciones personalizadas.

## Parámetros
- **LinePrice** – nivel de precio para colocar la orden.
- **IsBuy** – `true` para entradas largas, `false` para entradas cortas.
- **StopLoss** – distancia del stop-loss en unidades de precio (0 lo desactiva).
- **TakeProfit** – distancia del take-profit en unidades de precio (0 lo desactiva).
- **TrailingStop** – distancia del trailing stop en unidades de precio (0 lo desactiva).
- **Volume** – volumen de la orden.
- **CandleType** – tipo de vela utilizado para monitorear el precio.

## Reglas de Trading
- **Entrada**: cuando el precio de cierre cruza `LinePrice` en la dirección elegida.
- **Stop-loss**: cierra la posición cuando la pérdida supera la distancia `StopLoss` desde la entrada.
- **Take-profit**: cierra la posición cuando la ganancia alcanza la distancia `TakeProfit`.
- **Trailing stop**: tras la entrada, se ajusta al precio más favorable y cierra cuando el precio se mueve contra la posición en `TrailingStop`.

## Notas
- Funciona con cualquier valor soportado por StockSharp.
- Diseñado con fines educativos para ilustrar la traducción del trading manual por líneas desde MQL.
- La versión en Python está intencionalmente omitida.
