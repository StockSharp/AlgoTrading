# Estrategia de Four Hour Swing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Four Hour Swing** porta el asesor experto "4H swing" de MetaTrader a la API de alto nivel de StockSharp. El sistema original combina seguimiento de tendencia y confirmaciones de oscilador tomadas de marcos temporales superiores. Esta versión en C# se suscribe a tres marcos temporales (entrada, confirmación y filtro macro) y recrea la pila de indicadores con componentes de StockSharp.

## Lógica de negociación
- El filtro de tendencia principal utiliza tres medias móviles exponenciales calculadas sobre el precio típico de las velas de entrada. Una configuración larga requiere `Fast EMA > Medium EMA > Slow EMA`; una configuración corta refleja la condición de forma inversa.
- Los valores del oscilador Stochastic se evalúan en el marco temporal de confirmación superior. La línea %K debe mantenerse por encima de %D para largos y por debajo para cortos.
- El Momentum se muestrea de las mismas velas de confirmación y se convierte a la proporción de estilo MetaTrader alrededor de 100. Una operación solo se permite si al menos una de las últimas tres lecturas de momentum está más lejos que el umbral configurado.
- Los valores MACD mensuales (o definidos por el usuario) proporcionan el filtro de dirección macro. Una compra requiere que la línea MACD esté por encima de su señal, mientras que una venta verifica la relación opuesta.

Una posición se abre en la siguiente vela base una vez que todas las confirmaciones están alineadas y la cuenta está plana o posicionada en dirección opuesta (en ese caso la orden de mercado cierra y revierte).

## Gestión de riesgo
- Las distancias fijas de stop-loss y take-profit (expresadas en pips) se aplican inmediatamente después de la entrada.
- Un trailing stop opcional sigue el precio extremo alcanzado después de la entrada.
- La protección de break-even puede mover el stop al precio de entrada más un desplazamiento una vez que se alcanza la distancia de activación configurada.
- Una salida MACD opcional cierra las operaciones abiertas cuando el filtro macro cambia.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `TradeVolume` | Volumen de orden de mercado predeterminado. | `0.01` |
| `CandleType` | Tipo de vela de entrada (p. ej., velas de 4 horas). | `4H` |
| `SignalCandleType` | Tipo de vela de confirmación para Stochastic y Momentum. | `7D` (semanal) |
| `MacdCandleType` | Tipo de vela de filtro macro. | `30D` |
| `FastEmaPeriod`, `MediumEmaPeriod`, `SlowEmaPeriod` | Longitudes de EMA calculadas sobre el precio típico. | `4`, `14`, `50` |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmoothPeriod` | Configuración del oscilador Stochastic. | `13`, `5`, `5` |
| `MomentumPeriod` | Período de retrovisión utilizado por el indicador de Momentum. | `14` |
| `MomentumThreshold` | Distancia mínima desde 100 requerida para validar el Momentum. | `0.3` |
| `StopLossPips`, `TakeProfitPips` | Órdenes de protección en pips. | `20`, `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establecer en cero para desactivar. | `40` |
| `UseBreakEven` | Activa la protección de break-even. | `true` |
| `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Activador y desplazamiento para el movimiento de break-even. | `30`, `30` |
| `UseMacdExit` | Cerrar posiciones cuando el MACD macro cambia. | `false` |

## Notas
- Las características de gestión monetaria (stops de capital, objetivos de moneda) del experto original se omiten intencionalmente para mantener la implementación compacta.
- La estrategia procesa solo velas terminadas, correspondiendo a la evaluación barra por barra de MetaTrader.
- Los marcos temporales predeterminados reproducen la configuración común de 4 horas (confirmación semanal y filtro mensual), pero cada parámetro `DataType` puede reconfigurarse para ejecutarse en períodos alternativos.
