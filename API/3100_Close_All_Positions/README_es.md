# 3100 Cerrar Todas las Posiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convierte la utilidad MQL5 **Close all positions** en una estrategia de alto nivel de StockSharp.
- Observa velas terminadas del marco temporal configurado y acumula el beneficio flotante de cada posición abierta en el portafolio asignado.
- Cuando el beneficio flotante es igual o supera el umbral, se envían órdenes de mercado para aplanar todos los valores manejados por la estrategia (incluyendo estrategias hijas) hasta que el libro esté completamente cerrado.
- El indicador `_closeAllRequested` refleja la variable MQL `m_close_all` para que las órdenes de salida continúen emitiéndose hasta que no queden posiciones.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `ProfitThreshold` | `decimal` | `10` | Beneficio flotante (en moneda de cuenta) requerido antes de que la estrategia aplana cada posición abierta. Refleja `InpProfit` del EA. |
| `CandleType` | `DataType` | Marco temporal `1m` | Serie de velas que define los momentos de "nueva barra". La comprobación de beneficio se ejecuta solo cuando termina una vela, emulando la lógica `PrevBars` original. |

## Lógica de trading
1. La estrategia se suscribe a velas de `CandleType` y procesa solo barras terminadas, igual que el EA evaluaba el beneficio solo en una nueva barra.
2. En cada barra terminada el helper `CalculateTotalProfit` recupera `Portfolio.CurrentProfit` (PnL flotante incluyendo comisión y swap). Si el adaptador no puede proporcionar este valor recurre a sumar los valores individuales de `PnL` de posición.
3. Si el beneficio flotante calculado está por debajo de `ProfitThreshold`, no ocurre nada.
4. Tan pronto como el beneficio alcanza el umbral, `_closeAllRequested` se establece en `true` y `CloseAllPositions()` se ejecuta inmediatamente.
5. `CloseAllPositions()` recopila cada valor que tiene una exposición en el portafolio o en estrategias anidadas y envía órdenes de mercado en la dirección opuesta al volumen actual (largo → venta, corto → compra).
6. El indicador `_closeAllRequested` permanece establecido hasta que `HasAnyOpenPosition()` detecta que el portafolio está plano, coincidiendo con el comportamiento MQL donde `m_close_all` permanecía verdadero hasta que todos los tickets estaban cerrados.

## Notas adicionales
- Solo se proporciona la implementación en C#; la carpeta Python se deja intencionalmente vacía según los requisitos de la tarea.
- La estrategia no cancela órdenes pendientes porque el script original solo cerraba posiciones de mercado.
- Usar `SetOptimize` en `ProfitThreshold` para explorar objetivos de beneficio alternativos a través del optimizador de Designer si es necesario.

## Archivos
- `CS/CloseAllPositionsStrategy.cs`
