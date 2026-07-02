# Estrategia Macd Secrets
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia Macd Secrets** es un sistema multitemporal de seguimiento de momentum inspirado en el asesor experto original "Macd Secrets I" para MetaTrader. La adaptación a StockSharp usa la API de alto nivel y se centra en alinear la dirección MACD en tres marcos temporales, filtrando operaciones con una línea base de media móvil ponderada lineal (LWMA) y una comprobación de desviación de momentum. La estrategia mantiene solo una posición neta en cada momento, ofreciendo un perfil de riesgo simplificado y transparente frente al EA fuente, que podía piramidar múltiples órdenes.

## Generación de señales
### Configuración larga
1. La LWMA rápida está por debajo de la LWMA lenta en el marco temporal de ejecución, lo que indica que el precio opera cerca del lado inferior del canal de tendencia (el EA original aplica el mismo filtro).
2. La línea MACD está por encima de su línea de señal en todos los marcos temporales rastreados: ejecución, confirmación de tendencia y confirmación mensual. Esto refleja la alineación triple de MACD de la versión MQL.
3. Al menos una de las tres últimas lecturas de momentum en el marco temporal de tendencia se desvía de 100 por el mínimo configurado (predeterminado 0.3). El cálculo de desviación reproduce la lógica `MathAbs(100 - Momentum)` del EA.
4. No hay ninguna posición abierta.

Cuando se cumplen las condiciones, se coloca una orden de compra a mercado con el volumen configurado.

### Configuración corta
1. La línea MACD está por debajo de su línea de señal en los marcos temporales de ejecución, tendencia y mensual.
2. Al menos una de las tres últimas desviaciones de momentum en el marco temporal de tendencia supera el umbral corto configurado.
3. No hay ninguna posición abierta (la adaptación evita cobertura y escalado).

Si todas las reglas se mantienen, se envía una orden de venta a mercado.

### Gestión de operaciones
- La estrategia puede iniciar opcionalmente órdenes de protección usando distancias basadas en puntos para stop-loss y take-profit. Estas distancias se multiplican por el paso de precio del valor para convertir puntos en incrementos de precio.
- No se incluye lógica de trailing-stop, breakeven ni protección basada en equity del EA original; la protección de StockSharp se aplica una vez al inicio.
- Las señales se evalúan solo en velas terminadas para evitar ruido intrabar.

## Datos multitemporales
- **Marco temporal principal**: frecuencia de ejecución (predeterminado 15 minutos). MACD y el par de LWMAs se calculan aquí.
- **Marco temporal de tendencia**: confirmación de marco temporal superior (predeterminado 1 hora). Tanto MACD como momentum se ejecutan en esta suscripción. Las desviaciones de momentum se recopilan de las tres últimas velas cerradas.
- **Marco temporal mensual**: confirmación MACD de largo plazo (predeterminado 30 días para aproximar un mes calendario).

La estrategia sobrescribe `GetWorkingSecurities` para que las tres suscripciones se soliciten al conector desde el principio.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | --------------- |
| `OrderVolume` | Volumen de operación en lotes. Debe ser positivo. | `0.1` |
| `TakeProfitPoints` | Distancia de take-profit medida en puntos. Establecer en cero para desactivar. | `50` |
| `StopLossPoints` | Distancia de stop-loss en puntos. Establecer en cero para desactivar. | `20` |
| `FastMaPeriod` | Longitud de LWMA rápida en el marco temporal principal. | `6` |
| `SlowMaPeriod` | Longitud de LWMA lenta en el marco temporal principal. | `85` |
| `MacdFastPeriod` | Período de EMA rápida usado por cada instancia MACD. | `12` |
| `MacdSlowPeriod` | Período de EMA lenta usado por cada instancia MACD. | `26` |
| `MacdSignalPeriod` | Período de EMA de señal para MACD. | `9` |
| `MomentumPeriod` | Período retrospectivo de momentum en el marco temporal de tendencia. | `14` |
| `MomentumBuyThreshold` | Desviación absoluta mínima desde 100 requerida para operaciones largas. | `0.3` |
| `MomentumSellThreshold` | Desviación absoluta mínima desde 100 requerida para operaciones cortas. | `0.3` |
| `PrimaryCandleType` | Tipo de vela para ejecución. Predeterminado: marco temporal de 15 minutos. | `15m` |
| `TrendCandleType` | Tipo de vela para confirmación. Predeterminado: marco temporal de 1 hora. | `1h` |
| `MonthlyCandleType` | Tipo de vela para confirmación de largo plazo. Predeterminado: barra de 30 días. | `30d` |

## Notas de uso
- El filtro LWMA es intencionalmente asimétrico: solo las operaciones largas requieren que la LWMA rápida esté por debajo de la LWMA lenta, coincidiendo con el comportamiento observado en el script MQL.
- Como la adaptación opera una sola posición neta, omite el dimensionamiento de posición estilo martingala presente en el código fuente (`LotsOptimized`). Si se necesita apilamiento, puede reintroducirse rastreando el volumen ejecutado y comparándolo con `OrderVolume`.
- Asegúrese de que el bróker o proveedor de datos conectado pueda proporcionar los tres marcos temporales de velas; de lo contrario, la estrategia permanecerá inactiva esperando la formación de indicadores.
- Considere ajustar el marco temporal mensual para mercados donde no haya velas de 30 días disponibles, suministrando un parámetro `DataType` personalizado.
- La estrategia opera completamente sobre velas cerradas y no lee directamente buffers históricos de indicadores, cumpliendo las directrices de uso de indicadores de StockSharp.

## Diferencias frente al EA original
- No se adaptan trailing-stop, breakeven, salidas basadas en dinero ni protección de equity de toda la cuenta. En su lugar se usa la protección de StockSharp con distancias estáticas.
- La piramidación de órdenes y la lógica martingala se omiten por claridad. El dimensionamiento de posición permanece constante.
- Las notificaciones (alertas, correos electrónicos, mensajes push) no están implementadas.

## Aviso
El trading algorítmico implica un riesgo financiero significativo. Pruebe la estrategia con datos históricos y en un entorno simulado antes de desplegarla con capital real.
