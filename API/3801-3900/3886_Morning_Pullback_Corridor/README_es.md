# Estrategia del corredor de retroceso matutino
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia del corredor de retroceso matutino** replica el comportamiento del asesor experto "3_Otkat_Sys_v1_2" MetaTrader 4. El sistema opera una vez al día durante la sesión de la mañana, evaluando la interacción entre el precio actual y el corredor de precios formado por velas separadas por 29 barras. Reacciona a los retrocesos matutinos después de un fuerte movimiento nocturno e inmediatamente fija niveles asimétricos de toma de ganancias para posiciones largas y cortas.

## Lógica de trading
1. **Filtro de sesión**: las órdenes se consideran solo dentro de la hora comercial configurada (hora predeterminada de la plataforma 05:00) y durante los primeros minutos de esa hora. Los lunes y viernes están excluidos de acuerdo con el EA original.
2. **Cálculos del corredor de precios**: para cada vela completada, la estrategia mantiene una ventana móvil de las barras más recientes. Se compara:
   - el precio de apertura 29 barras atrás con el cierre de la vela anterior (`Open[29] - Close[1]`),
   - la vela anterior cerró con el precio de apertura 29 barras atrás (`Close[1] - Open[29]`),
   - la distancia desde el cierre anterior hasta el mínimo más bajo dentro del rango de 29 barras,
   - la distancia desde el máximo más alto en el mismo rango hasta el cierre anterior.
3. **Reglas de entrada**: si el movimiento nocturno excede el umbral `CorridorOpenClosePoints` y el último retroceso cabe dentro del sobre configurado `PullbackPoints ± CorridorPullbackPoints`, se abre una posición de mercado al comienzo de la sesión de la mañana:
   - Las entradas largas requieren un fuerte movimiento hacia abajo con un retroceso superficial o un movimiento hacia arriba con una continuación extendida por encima del corredor.
   - Las entradas cortas reflejan la lógica de las configuraciones bajistas.
4. **Gestión de posiciones** – cada operación recibe:
   - un stop-loss a `StopLossPoints * PriceStep` del precio de entrada,
   - una toma de ganancias en `TakeProfitPoints * PriceStep` para posiciones cortas y en `(TakeProfitPoints + LongTakeProfitExtraPoints) * PriceStep` para posiciones largas.
5. **Salida diaria**: cualquier posición que aún esté abierta después del umbral de cierre configurado (predeterminado después de las 22:45) se cierra a la fuerza para evitar que se mantenga durante la noche.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPoints` | Distancia base de obtención de beneficios en puntos del instrumento, aplicada a operaciones cortas. Las operaciones largas añaden `LongTakeProfitExtraPoints`. |
| `StopLossPoints` | Distancia de parada de protección en los puntos del instrumento. |
| `PullbackPoints` | Tamaño de retroceso deseado alrededor del cual la estrategia evalúa los retrocesos. |
| `CorridorOpenClosePoints` | Distancia mínima entre precios separados por 29 barras para confirmar un impulso nocturno. |
| `CorridorPullbackPoints` | Tolerancia aplicada al umbral de retroceso para crear el corredor de entrada. |
| `LongTakeProfitExtraPoints` | Se agregaron puntos adicionales al objetivo de obtención de ganancias a largo plazo. |
| `TradeHour` | Hora (0–23) durante la cual se permiten nuevas entradas. |
| `TradeMinuteLimit` | Minuto máximo dentro de la hora comercial para aceptar nuevas señales. |
| `CloseHour` | Hora en la que la estrategia comienza a buscar salidas basadas en tiempo. |
| `CloseMinuteThreshold` | Minuto dentro de `CloseHour` después del cual se cierra cualquier posición abierta. |
| `CandleType` | Marco de tiempo utilizado para las suscripciones de velas (predeterminado 1 minuto). |

## Notas de implementación
- La estrategia se basa en `Security.PriceStep` para convertir entradas basadas en puntos en distancias de precios absolutas. Si el instrumento no proporciona un paso de precio válido, la lógica vuelve a `1.0`.
- Los niveles de stop-loss y take-profit se monitorean en cada vela completa; la estrategia cierra posiciones con órdenes de mercado una vez que se supera el nivel dentro de ese rango de velas.
- La ventana móvil contiene las últimas 60 velas para cubrir los cálculos de 29 barras requeridos e imitar los ayudantes `Lowest/Highest` utilizados en MetaTrader.
- La visualización de gráficos (velas y operaciones propias) está disponible automáticamente cuando se crea un área de gráfico en la aplicación host.

## Consejos de uso
- Asegúrese de que el volumen de la cuenta comercial (propiedad `Volume`) esté configurado antes de comenzar la estrategia; EA nunca escala el tamaño de la posición dinámicamente.
- Mantenga la fuente de datos alineada con la zona horaria de la sesión que esperaba el asesor experto original para mantener un comportamiento idéntico.
- Optimice los parámetros del corredor al aplicar la estrategia a mercados con diferentes perfiles de volatilidad, porque los umbrales basados en puntos se ajustaron para el instrumento original.
