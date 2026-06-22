# Ejemplo de Estrategia MACD Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia replica el asesor experto "Example of MACD Automated" de MetaTrader 4 usando la API de alto nivel de StockSharp. Monitorea la línea principal del MACD en dos marcos temporales y abre una única posición cuando ambos filtros de tendencia coinciden. Las distancias de stop-loss y take-profit se aplican en pasos de precio, y el tamaño de la posición sigue la lógica original de AdvancedMM que acumula el volumen de las operaciones perdedoras recientes.

## Lógica de trading

1. **Filtro de marco temporal superior** – un MACD (12, 26, 9) calculado en el marco temporal superior (por defecto: velas diarias) debe tener una línea principal positiva para señales largas o negativa para señales cortas.
2. **Confirmación del marco temporal de entrada** – los mismos ajustes de MACD en el marco temporal de entrada (por defecto: velas de 15 minutos) deben apuntar en la misma dirección que el filtro de marco temporal superior.
3. **Posición única** – la estrategia opera una posición a la vez. Las nuevas entradas se omiten hasta que la posición existente sea cerrada por los niveles protectores.
4. **Órdenes protectoras** – los niveles de stop-loss y take-profit se miden en múltiplos del paso de precio del instrumento, replicando las entradas `StopLoss` y `TakeProfit` del MT4 original. Un valor de `0` deshabilita la protección correspondiente.
5. **Gestión monetaria avanzada** – el volumen de la operación aumenta después de operaciones perdedoras consecutivas sumando el tamaño de las pérdidas, y vuelve al volumen base después de operaciones ganadoras, emulando la función `AdvancedMM()` del EA fuente.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ------ | ----------- | -------------- |
| `BaseVolume` | Volumen base de la orden usado por la lógica de AdvancedMM. | `0.01` |
| `StopLossPoints` | Distancia del stop-loss expresada en pasos de precio. `0` deshabilita el stop. | `50` |
| `TakeProfitPoints` | Distancia del take-profit expresada en pasos de precio. `0` deshabilita el objetivo. | `30` |
| `MacdFastLength` | Período de la EMA rápida del MACD en ambos marcos temporales. | `12` |
| `MacdSlowLength` | Período de la EMA lenta del MACD. | `26` |
| `MacdSignalLength` | Período de la EMA de la línea de señal. | `9` |
| `EntryCandleType` | Marco temporal para la ejecución de operaciones. | Velas de `15m` |
| `FilterCandleType` | Marco temporal superior usado como filtro de tendencia. | Velas de `1d` |

## Gestión de la posición

- Los precios de stop-loss y take-profit se recalculan en cada nueva posición basándose en el paso de precio del instrumento.
- Cuando cualquier nivel protector es tocado dentro de una barra, la estrategia asume que la orden se ejecuta a ese nivel y registra la ganancia o pérdida realizada.
- Después de cada operación cerrada, la lógica de AdvancedMM actualiza el tamaño de la siguiente orden:
  - Menos de dos operaciones históricas → usar el volumen base.
  - La operación más reciente fue una pérdida → repetir su volumen.
  - Pérdidas consecutivas antes de la última ganancia → sumar sus volúmenes para recuperar.
  - De lo contrario → volver al volumen base.

## Notas

- La conversión mantiene el comportamiento original de mantener una posición hasta que se alcanza un nivel protector; no hay salida en los cruces del MACD.
- Asegúrese de que el instrumento tenga información válida de `PriceStep` para que las distancias de stop y objetivo basadas en puntos se calculen correctamente.
- La estrategia se basa en velas completadas y debe usarse con datos históricos o feeds en vivo que proporcionen actualizaciones de velas terminadas.
