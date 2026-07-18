# Estrategia SV Rompimiento Diario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia SV Rompimiento Diario** es una conversión directa en C# del asesor experto de MetaTrader 5 "SV v.4.2.5". El sistema evalúa la acción del precio una vez por barra completada y permite como máximo una operación por día de bolsa. El trading comienza solo después de la hora de inicio configurada y se basa en la relación entre el rango reciente de máximo/mínimo y dos medias móviles suavizadas. Se abre una posición larga cuando el rango analizado completo permanece por debajo de ambas medias, señalando un rebote anticipado desde condiciones de sobreventa. Por el contrario, se abre una posición corta cuando el rango permanece por encima de ambas medias, señalando una posible reversión desde territorio de sobrecompra.

## Reglas de trading
### Condiciones de entrada
- **Filtro diario** – no se evalúan operaciones hasta que el tiempo actual del servidor sea posterior a *Start Hour*/*Start Minute*. Solo se permite una entrada por día.
- **Ventana de datos** – la estrategia omite las `Shift` barras más recientes y analiza las siguientes `Interval` barras. Sus precios más altos y más bajos se comparan con las medias móviles desplazadas.
- **Entrada larga** – si el precio más alto en la ventana analizada está estrictamente por debajo de la MA lenta **y** el precio más bajo está estrictamente por debajo de la MA rápida, entrar largo (cerrando primero cualquier posición corta existente).
- **Entrada corta** – si el precio más bajo en la ventana analizada está estrictamente por encima de la MA lenta **y** el precio más alto está estrictamente por encima de la MA rápida, entrar corto (cerrando primero cualquier posición larga existente).

### Gestión de salida
- **Stop loss inicial** – colocado a `Stop Loss (pips)` del precio de entrada. Si se alcanza el nivel, la posición se cierra.
- **Take profit** – colocado a `Take Profit (pips)` del precio de entrada. Si se alcanza el nivel, la posición se cierra.
- **Trailing stop** – cuando está habilitado (tanto la distancia de trailing como el paso son mayores que cero), el stop se mueve en la dirección del beneficio. Para largos, el stop se eleva a `Cierre − Trailing Stop` una vez que el precio avanza más de `Trailing Stop + Trailing Step`; los cortos replican la lógica.
- **Bloqueo diario** – independientemente de cómo salga una operación, la estrategia no abrirá una nueva posición hasta el siguiente día de trading.

### Dimensionamiento de posición
- **Modo manual** – cuando *Use Manual Volume* es `true`, la estrategia envía el valor fijo de *Volume* (ajustado al paso de volumen del instrumento).
- **Modo basado en riesgo** – cuando *Use Manual Volume* es `false`, la estrategia estima el tamaño de la operación a partir del capital de la cuenta y el `Risk %`. Divide el capital en riesgo por el valor monetario de la distancia de stop configurada, usando información del paso del instrumento cuando esté disponible.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| Use Manual Volume | `false` | Usar el valor fijo de `Volume` en lugar del dimensionamiento basado en riesgo. |
| Volume | `0.1` | Volumen de operación cuando el dimensionamiento manual está habilitado. |
| Risk % | `5` | Porcentaje del capital de la cuenta arriesgado por operación cuando el dimensionamiento manual está activo. |
| Stop Loss (pips) | `50` | Distancia del stop-loss en pips. Establezca en `0` para deshabilitar. |
| Take Profit (pips) | `50` | Distancia del take-profit en pips. Establezca en `0` para deshabilitar. |
| Trailing Stop (pips) | `5` | Distancia del trailing stop en pips. Requiere que `Trailing Step` sea mayor que cero. |
| Trailing Step (pips) | `5` | Incremento mínimo de beneficio antes de que se mueva el trailing stop. |
| Start Hour | `19` | Hora (tiempo de bolsa) en que pueden comenzar las entradas. |
| Start Minute | `0` | Minuto (tiempo de bolsa) en que pueden comenzar las entradas. |
| Shift | `6` | Número de barras más recientes excluidas antes de analizar el rango. |
| Interval | `27` | Número de barras históricas usadas para calcular la ventana de máximo/mínimo. |
| Fast MA Period | `14` | Longitud de la media móvil rápida. |
| Fast MA Shift | `0` | Desplazamiento horizontal (barras atrás) usado para el valor de la MA rápida. |
| Fast MA Method | `Smma` | Método de media móvil para la MA rápida. |
| Fast Applied Price | `Median` | Fuente de precio para la MA rápida. |
| Slow MA Period | `41` | Longitud de la media móvil lenta. |
| Slow MA Shift | `0` | Desplazamiento horizontal (barras atrás) usado para el valor de la MA lenta. |
| Slow MA Method | `Smma` | Método de media móvil para la MA lenta. |
| Slow Applied Price | `Median` | Fuente de precio para la MA lenta. |
| Candle Type | `1 hour` | Serie de velas usada para los cálculos. |

## Notas adicionales
- La conversión mantiene el comportamiento original de analizar una ventana de precio retrasada (`Shift` + `Interval`) para evitar las barras más recientes al determinar rompimientos.
- La lógica de trailing usa el precio de cierre de la vela para aproximar las actualizaciones de trailing basadas en ticks de MetaTrader. Ajuste las distancias en pips si su instrumento requiere diferente precisión.
- El dimensionamiento basado en riesgo depende de `Security.PriceStep`, `Security.StepPrice` y `Security.VolumeStep`. Proporcione estos valores en la configuración de su instrumento para un dimensionamiento de lotes preciso.
- La estrategia llama a `StartProtection()` para que pueda adjuntar reglas de riesgo globales adicionales si es necesario.
- Para reflejar el EA original, asegúrese de que su feed de datos y cuenta de trading operen en la misma zona horaria del servidor referenciada por los parámetros *Start Hour* y *Start Minute*.
