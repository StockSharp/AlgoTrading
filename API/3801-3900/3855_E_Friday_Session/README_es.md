# Estrategia de la sesión del viernes electrónico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia E-Friday Session replica el clásico asesor experto MetaTrader que opera solo los viernes. Observa la vela diaria anterior y abre una posición a una hora configurada al inicio de la sesión del viernes. La dirección es contraria: si el día anterior cerró por debajo de su apertura (vela bajista), la estrategia compra; si el día anterior cerró por encima de su apertura (vela alcista), la estrategia vende. Las posiciones se gestionan intradía y pueden cerrarse automáticamente después de una hora configurable o mediante paradas de protección.

## Reglas de trading
1. Reúna velas diarias (predeterminado: 1 día) para obtener la apertura y el cierre del día anterior.
2. Los viernes, monitoree las velas intradiarias (predeterminado: 1 minuto) para detectar la hora de entrada configurada.
3. A la primera vela de la hora de entrada:
   - Vaya en largo cuando el día anterior fue bajista.
   - Vaya en corto cuando el día anterior fue alcista.
   - Evite operar si el día anterior fue un doji (apertura es igual a cierre).
4. Opcionalmente cerrar la posición automáticamente una vez alcanzada la hora de salida configurada.
5. Administre las salidas utilizando una lógica de stop-loss, take-profit y trailing stop opcional que imita al Asesor Experto original, incluida la activación de ganancias y los umbrales de los pasos finales.

## Notas de implementación
- Utiliza StockSharp suscripciones de velas de alto nivel tanto para el contexto diario como para el tiempo intradiario.
- Convierte los controles de riesgo basados en puntos de la versión MQL en compensaciones de precios absolutas utilizando el paso de precio del valor.
- Mantiene trailingstops en el código, actualizándolos en cada vela terminada y cerrando la posición cuando se superan los precios extremos.
- Garantiza solo una operación por viernes mediante el seguimiento del estado diario.
- Admite entradas tanto largas como cortas, respetando la activación de números mágicos original al intercambiar un solo símbolo por instancia de estrategia.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Tamaño comercial en lotes/contratos. | `0.1` |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio (0 desactivaciones). | `75` |
| `TakeProfitPoints` | Distancia de toma de ganancias en pasos de precio (0 inhabilitaciones). | `0` |
| `HourOpen` | Hora del día (0-23) para abrir la posición. | `7` |
| `UseClosePositions` | Habilitar el cierre automático después de la hora de salida. | `true` |
| `HourClose` | Hora del día (0-23) para cerrar la posición si está habilitado. | `19` |
| `UseTrailing` | Habilite los ajustes del trailing stop. | `true` |
| `ProfitTrailing` | Exija que las ganancias excedan la distancia de seguimiento antes de que se active el seguimiento. | `true` |
| `TrailingStopPoints` | Distancia del trailing stop en pasos de precio. | `60` |
| `TrailingStepPoints` | Se requieren puntos adicionales antes de apretar el tope final. | `5` |
| `IntradayCandleType` | Tipo de vela para sincronización intradía (velas predeterminadas de 1 minuto). | `TimeSpan.FromMinutes(1)` |
| `DailyCandleType` | Tipo de vela para la detección de sentimiento diario (velas predeterminadas de 1 día). | `TimeSpan.FromDays(1)` |

## Consejos de uso
- Alinee la sesión de negociación del instrumento para que la hora de entrada del viernes coincida con la apertura de mercado deseada.
- Al configurar los valores de stop-loss y trailing, expréselos en los mismos "puntos" utilizados por el paso de precio del símbolo para reproducir el comportamiento MetaTrader.
- La estrategia está diseñada para una única operación cada viernes. Para intercambiar múltiples símbolos, ejecute instancias de estrategia separadas por símbolo.

## Diferencias con el original EA
- Utiliza datos de cierre de velas para la toma de decisiones, mientras que los precios por tick originales sondeados.
- Las salidas protectoras se ejecutan mediante órdenes de mercado cuando las velas indican que se alcanzaron los niveles objetivo o de parada dentro del intervalo.
- Los parámetros de la estrategia se exponen a través del sistema `StrategyParam` de StockSharp, lo que admite la optimización y el enlace de la interfaz de usuario.
