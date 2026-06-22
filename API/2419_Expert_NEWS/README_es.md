# Estrategia Expert NEWS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Expert NEWS es una conversión directa del robot MQL5 "Expert_NEWS". La estrategia coloca continuamente órdenes stop simétricas por encima y por debajo del precio de mercado actual, gestionando las posiciones resultantes con protección de punto de equilibrio, stops de seguimiento y actualizaciones programadas de órdenes pendientes. La implementación se basa en cotizaciones Level1 y mantiene el volumen de negociación predeterminado en 0.1 lotes.

## Lógica de negociación
1. **Suscripción a cotizaciones** – la estrategia escucha las actualizaciones de mejor bid/ask y calcula los precios de las órdenes a partir de los últimos valores.
2. **Órdenes stop iniciales** – cuando no existe posición larga ni buy stop activo, se coloca un nuevo buy stop en `ask + EntryOffsetTicks * PriceStep`. Cuando no existe posición corta ni sell stop activo, se coloca un sell stop en `bid - EntryOffsetTicks * PriceStep`.
3. **Actualización de órdenes** – cada `OrderRefreshSeconds`, la estrategia cancela y recrea un stop pendiente si el precio requerido se desvía más de `TrailingStepTicks` ticks.
4. **Protección de posición** – tras una ejecución, la estrategia abre órdenes stop de protección y take-profit si las distancias solicitadas cumplen la restricción `MinimumStopTicks`.
5. **Control de punto de equilibrio** – cuando `UseBreakEven` está activado, el stop se mueve a `entrada ± BreakEvenProfitTicks` una vez que el mercado se desplaza suficientemente y el nuevo stop respeta la distancia mínima de la cotización actual.
6. **Stop de seguimiento** – una vez que el precio avanza `TrailingStartTicks`, el stop sigue usando `TrailingStopTicks` como distancia y `TrailingStepTicks` como paso de mejora mínima.
7. **Limpieza** – cerrar la posición cancela todas las órdenes de protección restantes.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `StopLossTicks` | Distancia inicial del stop de protección (ticks). Establezca en cero para desactivar la orden stop inicial. |
| `TakeProfitTicks` | Distancia inicial del take-profit (ticks). Establezca en cero para desactivar la orden objetivo. |
| `TrailingStopTicks` | Distancia del stop de seguimiento (ticks). |
| `TrailingStartTicks` | Beneficio en ticks requerido antes de que se active la lógica de seguimiento. |
| `TrailingStepTicks` | Mejora mínima al actualizar el stop de seguimiento o las órdenes de entrada pendientes. |
| `UseBreakEven` | Activa el desplazamiento del stop al punto de equilibrio una vez que hay suficiente beneficio. |
| `BreakEvenProfitTicks` | Margen de beneficio adicional al mover el stop al punto de equilibrio. |
| `EntryOffsetTicks` | Distancia entre la cotización actual y cada nueva orden stop de entrada. |
| `OrderRefreshSeconds` | Intervalo de tiempo entre intentos automáticos de actualización de órdenes stop pendientes. |
| `MinimumStopTicks` | Respaldo manual para el requisito de nivel de stop del bróker. Los stops más cercanos que esta distancia no se envían. |

## Gestión de posición
- Las órdenes de protección siempre coinciden con el volumen de la posición neta. Las ejecuciones parciales redimensionan automáticamente las órdenes stop y take-profit.
- La lógica de punto de equilibrio y de seguimiento funciona incluso cuando el stop inicial está desactivado; el stop se creará dinámicamente una vez que se cumplan las reglas.
- La estrategia mantiene el precio del stop más reciente en memoria para que las actualizaciones de seguimiento preserven un comportamiento monótono.

## Notas de uso
- Asegúrese de que `Security.PriceStep` esté configurado; cada parámetro de distancia en ticks se multiplica por este valor.
- El volumen predeterminado es `0.1` para reflejar el robot original. Ajuste la propiedad `Volume` si se requiere otro tamaño.
- `MinimumStopTicks` debe establecerse al requisito de nivel de stop de la plataforma de negociación si ésta lo exige. Déjelo en cero para permitir los stops más ajustados posibles.
- El algoritmo no depende de barras históricas y puede operar solo con cotizaciones en tiempo real.
