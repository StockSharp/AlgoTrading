# Estrategia controlada por tiempo FitFul 13
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia controlada por tiempo FitFul 13** es una versión StockSharp del MetaTrader 4 asesor experto "FitFul_13". La estrategia construye una escalera de pivote semanal (PP, R0.5, R1, R1.5, R2, R2.5, R3 y los niveles de soporte correspondientes) utilizando el máximo, el mínimo y el cierre de la semana anterior. Las decisiones comerciales se toman en el plazo principal (predeterminado, 1 hora) y, opcionalmente, se confirman en un plazo más rápido (predeterminado, 15 minutos). Se permiten nuevas posiciones solo en minutos intradiarios específicos para imitar el comportamiento original de EA.

## Lógica de señal
1. **Cálculo de pivote semanal**
   - Al cierre de cada vela semanal, se recalcula la escalera de pivote.
   - Los precios de limitación de pérdidas y toma de ganancias se compensan con respecto a los niveles base mediante una distancia configurable expresada en puntos de precio.
2. **Condiciones de plazo principal**
   - La última vela primaria completada debe ser alcista para buscar entradas largas o bajista para buscar entradas cortas.
   - La vela primaria anterior debe abarcar uno de los niveles de pivote (abrir por debajo y cerrar por arriba para largos, abrir por arriba y cerrar por debajo para cortos).
3. **Condiciones de plazo de confirmación**
   - Si la vela de confirmación actual es alcista, los mínimos de las dos velas de confirmación anteriores deben perforar y cerrar por encima del mismo nivel de pivote para confirmar una señal larga.
   - Si la vela de confirmación actual es bajista, los máximos de las dos velas de confirmación anteriores deben perforar y cerrar por debajo de un nivel de pivote para confirmar una señal corta.
4. **Tiempo de entrada**
   - Se realiza una operación solo cuando el minuto de apertura de la vela primaria terminada es igual a uno de los cuatro minutos configurados (0, 15, 30 o 45 por defecto).
   - La exposición neta está limitada por `MaxNetPositions × Volume` para emular la restricción de "tres órdenes abiertas como máximo" de la versión MetaTrader.

## Gestión de riesgos
- **Paradas y objetivos**: a cada posición se le asigna un límite de pérdidas y toma de ganancias derivado de un pivote inmediatamente después de la entrada.
- **Parada dinámica**: una vez que el precio avanza en la cantidad de puntos configurada, la parada sigue la dirección comercial.
- **Tiempo máximo de retención**: las operaciones rentables se cierran una vez que el tiempo de retención excede la duración configurada (48 horas de forma predeterminada).
- **Regla fija de los viernes**: los viernes, cualquier posición abierta se cierra entre los minutos configurados de la hora especificada (predeterminado de 21:50 a 21:59).

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `PrimaryCandleType` | Plazo utilizado para las verificaciones cruzadas del pivote principal. |
| `ConfirmationCandleType` | Plazo más rápido que valida las reacciones de pivote. |
| `Volume` | Volumen neto de órdenes de mercado. |
| `MaxNetPositions` | Exposición máxima medida en múltiplos de `Volume`. |
| `OffsetPoints` | Distancia de precio-punto aplicada a paradas y objetivos alrededor de cada pivote. |
| `TrailingStopPoints` | Distancia del trailing stop en puntos de precio. |
| `CloseAfter` | Tiempo máximo de tenencia para posiciones rentables. |
| `CloseHour`, `CloseMinuteFrom`, `CloseMinuteTo` | Ventana horaria del viernes para salidas forzadas. |
| `EntryMinute0..3` | Minutos permitidos (dentro de cada hora) para abrir nuevas posiciones. |

## Notas
- La conversión mantiene la dependencia del EA original de la escalera dinámica de la semana anterior y de las ventanas de ejecución de un cuarto de hora.
- La administración del dinero se ha simplificado: el parámetro StockSharp `Volume` controla el tamaño del pedido directamente en lugar de volver a implementar el cálculo dinámico del lote desde MetaTrader.
- Todos los comentarios dentro del código están escritos en inglés, como lo exigen las pautas del proyecto.
