# ABE BE RSI Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia **ABE BE RSI** es una adaptación del MetaTrader asesor experto `Expert_ABE_BE_RSI`. El sistema combina patrones clásicos de inversión de velas con la confirmación del impulso del índice de fuerza relativa (RSI). Dos velas consecutivas deben formar un patrón envolvente alcista o bajista, y la vela completada más reciente debe mostrar una lectura RSI dentro de umbrales predefinidos. Se aplican reglas cruzadas RSI adicionales para aplanar o revertir posiciones existentes, lo que refleja fielmente la lógica de decisión de la implementación MQL original.

## Lógica de trading

1. **Detección de patrones envolventes**
La estrategia evalúa las dos últimas velas completadas. Una señal alcista requiere:
   - La vela *t-2* cierra por debajo de su apertura (cuerpo bajista).
   - La vela *t-1* cierra más alto de lo que abre (cuerpo alcista).
   - El tamaño del cuerpo de la vela *t-1* excede el promedio móvil de los tamaños de cuerpo recientes (cinco barras por defecto).
   - La vela *t-1* cierra por encima de la apertura de la vela *t-2* y se abre por debajo de su cierre, lo que garantiza un verdadero evento envolvente.
   - El punto medio de la vela *t-2* está por debajo de la media móvil de los precios de cierre, lo que confirma una tendencia bajista a corto plazo.

Una señal envolvente bajista utiliza condiciones simétricas: la vela más antigua es alcista, la vela más nueva es bajista con un cuerpo más grande que el promedio y la vela más nueva envuelve completamente el cuerpo anterior mientras que el punto medio de la barra más antigua se ubica por encima del promedio móvil para confirmar el agotamiento de la tendencia bajista.

2. **RSI Confirmación**
   - Las entradas largas requieren que el RSI de la vela cerrada más recientemente esté por debajo del nivel de entrada alcista configurado (predeterminado 40).
   - Las entradas cortas requieren que el RSI de la vela cerrada más recientemente esté por encima del nivel de entrada bajista (predeterminado 60).

3. **Gestión de salida**
Se monitorean RSI cruces en dos niveles para cerrar posiciones existentes:
   - Las posiciones cortas se cubren cuando RSI supera el umbral de salida inferior (predeterminado 30) o superior (predeterminado 70) después de estar por debajo de él en la vela anterior.
   - Las posiciones largas se cierran cuando RSI cae por debajo de cualquiera de los umbrales después de haber estado por encima de él en la vela anterior.

4. **Ejecución de órdenes**
Las órdenes de mercado se utilizan tanto para entradas como para salidas. Al invertir, la estrategia primero cierra la exposición actual y luego entra en la nueva dirección con el volumen base configurado. El tamaño de la posición imita el modelo de lote fijo del experto MQL.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Tamaño del pedido en contratos. | `0.1` |
| `RsiPeriod` | Número de barras utilizadas por el filtro RSI. | `11` |
| `MovingAveragePeriod` | Período para el tamaño del cuerpo de la vela y las medias móviles del precio de cierre. | `5` |
| `BullishEntryLevel` | Valor máximo RSI que aún valida una entrada envolvente alcista. | `40` |
| `BearishEntryLevel` | Valor mínimo RSI requerido para una entrada envolvente bajista. | `60` |
| `ExitLowerLevel` | Baje el nivel de cruce RSI para posiciones planas. | `30` |
| `ExitUpperLevel` | Nivel de cruce superior RSI para posiciones de aplanamiento. | `70` |
| `CandleType` | Serie de velas procesadas por la estrategia. | `1 hour time frame` |

Todos los parámetros se pueden optimizar dentro de Designer o Runner gracias a los contenedores `StrategyParam`.

## Tubería de indicadores

- **Índice de fuerza relativa (RSI)**: calcula el impulso sobre el `RsiPeriod` configurable y proporciona umbrales de entrada/salida.
- **Promedio móvil simple de precios de cierre**: proporciona un contexto de tendencia utilizado para validar patrones envolventes.
- **Promedio móvil simple de tamaños de cuerpo de velas**: garantiza que la vela envolvente sea más grande que el tamaño de cuerpo promedio en las últimas `MovingAveragePeriod` barras.

## Notas de uso

- La estrategia sólo actúa sobre velas completamente completadas (`CandleStates.Finished`). Los datos de la barra parcial se ignoran para evitar señales prematuras.
- El historial de velas se almacena internamente para evaluar las condiciones envolventes sin atravesar grandes colecciones, respetando las pautas de conversión de todo el proyecto.
- `StartProtection()` está habilitado para que los mecanismos de protección base StockSharp se activen cuando la exposición de la posición no es cero.

## Diferencias frente al Expert Advisor original

- El Asesor Experto original se basa en el sistema de votación de señales de MetaTrader. En este puerto, los votos se traducen en acciones directas de entrada y salida que replican las mismas condiciones.
- La administración del dinero se simplifica a un único parámetro `Volume`, que refleja el tamaño de lote fijo (`Money_FixLot_Lots`) utilizado por el experto en fuentes.
- La compatibilidad con trailing-stop no está incluida, ya que la versión MT5 utilizaba un módulo "sin seguimiento".

## Pruebas recomendadas

1. Adjunte la estrategia a un gráfico en Designer o API Runner con un símbolo que históricamente reacciona a las reversiones envolventes (por ejemplo, los principales pares de divisas).
2. Verifique RSI y los parámetros de promedio móvil antes de ejecutar sesiones en vivo; los valores predeterminados reproducen la configuración del asesor experto publicada.
3. Utilice las funciones de optimización integradas para explorar umbrales RSI alternativos o períodos promedio para diferentes mercados.
