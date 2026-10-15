# Estrategia Master MM Droid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Master MM Droid es un port multi-módulo del asesor experto original de MetaTrader 5. La implementación de StockSharp conserva las ideas centrales del robot heredado mientras usa la API de alto nivel para suscripciones a velas, vinculación de indicadores y gestión de órdenes. Cuatro bloques de gestión monetaria independientes pueden activarse o desactivarse, permitiendo a la estrategia mezclar entradas de momentum con órdenes de ruptura programadas y operaciones de gap semanales.

## Módulos
1. **Bloque RSI**
   - Usa un Índice de Fuerza Relativa de 14 períodos en el tipo de vela configurado.
   - Entra largo cuando el RSI cruza hacia arriba desde el umbral de sobreventa y corto cuando cruza hacia abajo desde el nivel de sobrecompra.
   - Permite piramidación con un número configurable de entradas adicionales separadas por un paso de precio fijo.
   - Aplica un stop inicial fijo basado en distancia en puntos y activa un stop móvil una vez que la posición está abierta.
2. **Bloque de ruptura de caja**
   - Reconstruye cajas de ruptura tres veces al día (horas con desplazamiento de sesión 6, 12 y 20 por defecto).
   - Coloca órdenes de stop agrupadas por encima del máximo de sesión y por debajo del mínimo con un buffer configurable.
   - Cancela las órdenes pendientes y posiciones en los reinicios de sesión (horas 0, 10 y 16), imitando el comportamiento del experto original.
3. **Bloque de ruptura semanal**
   - Rastrea la acción del precio del lunes y almacena el máximo/mínimo acumulado de la primera parte de la sesión.
   - Coloca órdenes de stop simétricas dentro de una ventana de activación limitada (`StartHour` – `WeeklySetupEndHour`) para que la semana comience con una ruptura OCO.
   - Fuerza un estado plano los viernes por la noche para evitar exposición de fin de semana.
4. **Bloque de gap**
   - Compara la nueva apertura diaria con el máximo/mínimo del día anterior (usando el calendario con desplazamiento).
   - Compra fuertes aperturas de gap bajista y vende fuertes aperturas de gap alcista.
   - Establece un stop protector a una distancia configurable y entrega la gestión adicional al motor de trailing.

## Parámetros
| Nombre | Descripción |
| ------ | ----------- |
| `CandleType` | Marco temporal usado para los cálculos de indicadores y las verificaciones de ventanas de tiempo. |
| `TimeShiftHours` | Desplazamiento de sesión aplicado a las marcas de tiempo de las velas para que el horario coincida con el EA original. |
| `StartHour` | Hora de inicio base del lunes para el módulo semanal (antes de aplicar el desplazamiento). |
| `EnableRsiModule`, `EnableBoxModule`, `EnableWeeklyModule`, `EnableGapModule` | Interruptores para los cuatro bloques independientes. |
| `RsiPeriod`, `RsiLowerLevel`, `RsiUpperLevel` | Cálculo RSI y niveles de disparo. |
| `RsiMaxEntries`, `RsiPyramidPoints` | Controles de piramidación para el bloque RSI. |
| `RsiStopLossPoints`, `RsiTrailingPoints` | Tamaños de stop inicial y stop móvil (en puntos) para operaciones dirigidas por RSI. |
| `BoxEntryPoints`, `BoxTrailingPoints` | Buffer de ruptura y distancia de trailing para las órdenes de caja. |
| `WeeklyEntryPoints`, `WeeklySetupEndHour`, `WeeklyTrailingPoints` | Configuración de ruptura semanal. |
| `GapStopLossPoints`, `GapTrailingPoints` | Stop protector y distancia de trailing del módulo de gap. |

Todos los parámetros basados en puntos se multiplican por el `TickSize` del instrumento para obtener compensaciones de precio, de modo que la estrategia se adapta a diferentes símbolos.

## Lógica de trading
- **Vinculación de indicadores**: Un único indicador RSI está vinculado a la suscripción de velas. Cada vela terminada dispara `ProcessCandle`, que envía los valores a los cuatro manejadores de módulo.
- **Seguimiento del estado diario**: La estrategia agrega apertura/máximo/mínimo para cada día con desplazamiento para soportar la lógica de gap y mantener una referencia histórica para el módulo semanal.
- **Colocación de órdenes**: Las órdenes se envían a través de `BuyMarket`, `SellMarket`, `BuyStop`, `SellStop` de acuerdo con las mejores prácticas de la API de alto nivel. Los módulos programados siempre cancelan las órdenes activas antes de rearmarse para evitar duplicados.
- **Gestión de trailing**: Una vez que una posición está activa, `_activeTrailingPoints` almacena la distancia específica del módulo. El método `UpdateTrailing` mueve las órdenes de stop solo en la dirección favorable.

## Gestión del riesgo
- Solo las órdenes de mercado creadas por los módulos RSI y gap están protegidas por un stop inmediato calculado en puntos.
- Los módulos de ruptura dependen del motor de trailing después de la activación; pueden combinarse con protección de portafolio externo si es necesario.
- Llamar a `ClosePosition()` es la forma canónica de aplanar, preservando la compatibilidad con las herramientas de riesgo de StockSharp.

## Notas de uso
- La estrategia opera en un único instrumento y usa el valor global `Volume` para el dimensionamiento. Ajuste la protección de portafolio por separado si necesita límites de riesgo por posición.
- Los tiempos de sesión se evalúan después de aplicar `TimeShiftHours`. Por ejemplo, con el valor predeterminado `2`, el reinicio de caja a la hora `0` corresponde a las 02:00 hora del servidor.
- Dado que las estrategias de StockSharp gestionan posiciones netas, las cestas largas/cortas simultáneas (posibles en cuentas de MetaTrader con cobertura) se consolidan. Esta es la principal diferencia de comportamiento con respecto al EA original y debe considerarse durante la validación.

## Registro y monitoreo
- Cada módulo restablece sus flags internos una vez que la posición vuelve a cero, ayudando a los operadores a diagnosticar qué bloque produjo una operación.
- Agregue gráficos opcionales o registro a través de las instalaciones de StockSharp si se requieren análisis detallados.
