# ROC Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia ROC es un puerto StockSharp del asesor experto MetaTrader almacenado en `MQL/26938/ROC.mq4`. Opera con un solo símbolo y evalúa la acción del precio utilizando una cadena de promedios móviles ponderados lineales (LWMA), un modelo de tasa de cambio personalizado (ROC), mayor impulso de marco temporal y un filtro mensual MACD. Se conservan las características originales de administración del dinero, como el punto de equilibrio, los trailingstops basados ​​en pips, la protección del capital y los objetivos de ganancias denominados en dinero.

## Lógica de entrada
1. La estrategia se suscribe a tres flujos de datos:
   - Velas comerciales principales definidas por la propiedad `CandleType`.
   - Un marco de tiempo más alto para el oscilador de impulso de 14 períodos (seleccionado automáticamente según el marco de tiempo de negociación).
   - Velas mensuales para el filtro de confirmación MACD.
2. En cada vela comercial terminada se deben cumplir las siguientes condiciones para abrir una posición:
   - El modelo personalizado ROC debe informar una tendencia alcista (`Line4 < Line5`) para compras o una tendencia bajista (`Line4 > Line5`) para ventas.
   - La LWMA rápida calculada sobre el precio típico debe cotizar por encima de la LWMA lenta para compras y por debajo para ventas.
   - Cualquiera de las últimas tres lecturas de impulso tomadas del período de tiempo superior debe exceder el umbral de compra o venta configurado (desviación absoluta de 100).
   - La línea principal mensual MACD debe permanecer por encima de su línea de señal para compras y por debajo para ventas.
   - El tamaño de la posición respeta el límite `MaxTrades` y, opcionalmente, escala el siguiente volumen comercial después de pérdidas consecutivas cuando `IncreaseFactor` es mayor que cero.

## Lógica de salida
- Las órdenes clásicas de stop-loss y take-profit se proyectan en MetaTrader puntos tan pronto como cambia el tamaño de la posición.
- El bloque de equilibrio opcional mueve el tope de protección al precio de entrada más el desplazamiento configurado una vez que se alcanza la distancia de activación en puntos.
- Los trailingstops basados en pips ajustan el valor de stop en cada cierre de vela.
- Las comprobaciones de administración de dinero cierran la posición cuando se alcanza un objetivo de divisa o de porcentaje y pueden rastrear las ganancias flotantes al detectar retrocesos mayores que `StopLossMoney` después de que las ganancias exceden `TakeProfitMoney`.
- Una parada de acciones compara la reducción flotante con las acciones más altas registradas y liquida la posición cuando se excede el porcentaje permitido.
- Establecer `ExitStrategy` en `true` realiza la rutina de salida de emergencia y cierra la posición actual en el mercado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `LotSize` | Volumen comercial base abierto en cada señal. |
| `IncreaseFactor` | Vuelve a calcular el siguiente volumen después de operaciones perdedoras consecutivas. |
| `FastMaPeriod` / `SlowMaPeriod` | Longitud de los filtros de tendencia LWMA. |
| `PeriodMa0`, `PeriodMa1`, `BarsV`, `AverBars`, `KCoefficient` | Defina el modelo de tendencia personalizado ROC. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Desviación absoluta mínima de 100 utilizada por el filtro de impulso de marco temporal superior. |
| `StopLossSteps`, `TakeProfitSteps` | Distancias de protección iniciales expresadas en MetaTrader puntos. |
| `TrailingStopSteps` | Trailing stop basado en pips. |
| `UseBreakEven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Configurar el módulo de equilibrio. |
| `UseTpInMoney`, `TpInMoney`, `UseTpInPercent`, `TpInPercent` | Objetivos de obtención de beneficios basados en dinero y porcentajes. |
| `EnableMoneyTrailing`, `TakeProfitMoney`, `StopLossMoney` | Parámetros del módulo de seguimiento de dinero. |
| `UseEquityStop`, `TotalEquityRisk` | Configuración de protección del patrimonio. |
| `MaxTrades` | Número máximo de ampliaciones por dirección. |
| `ExitStrategy` | Fuerza una posición plana inmediata cuando está habilitado. |

## Notas
- El período de tiempo más alto para el indicador de impulso se deriva automáticamente del período de tiempo de negociación para que coincida con la declaración de cambio original en el código MetaTrader.
- Todos los cálculos de indicadores utilizan el nivel alto `Bind` API, por lo que no se requieren solicitudes de datos manuales.
- La estrategia es solo de compensación: cuando aparece una nueva señal larga mientras se mantienen posiciones cortas, la exposición corta se cierra primero antes de entrar en posición larga, reflejando el comportamiento de la EA original en cuentas sin cobertura.
