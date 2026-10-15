# Estrategia de Contador Donchain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Donchain Counter es una portación para StockSharp del asesor experto MQL5 "Donchain counter" de Michal Rutka. El sistema observa cómo el Canal Donchian se expande para detectar rupturas y luego defiende la posición arrastrando el stop a lo largo de la banda opuesta una vez que el precio se ha movido una distancia fija. Solo se puede abrir una posición cada 24 horas, respetando la restricción del original.

## Lógica de trading
### Entradas largas
- Evalúa señales en velas completadas del período configurado (predeterminado **H1**).
- Observa la banda superior del Donchian en las dos barras cerradas anteriores. Cuando la banda en la barra *t-1* es superior a la de *t-2* (una ruptura fresca del máximo del canal), se coloca una orden de compra de mercado.
- El stop protector inicial se ancla a la banda inferior actual del Donchian.

### Entradas cortas
- Monitorea la banda inferior del Donchian en las dos barras cerradas anteriores. Cuando la banda en la barra *t-1* es inferior a la de *t-2* (una ruptura del mínimo del canal), se envía una orden de venta de mercado.
- El primer nivel de stop se establece en la banda superior actual del Donchian.

### Período de espera entre operaciones
- Después de cualquier nueva entrada el algoritmo registra el tiempo de ejecución y bloquea entradas posteriores durante la duración de `TradeCooldown` (predeterminado **24 horas**). Esto reproduce la regla de "solo una operación por día" de la versión MQL.

### Reglas de trailing y salida
- El mecanismo de trailing se activa solo después de que el precio avance al menos `BufferSteps` pasos de precio más allá de la banda Donchian opuesta. Esto reproduce el requisito del EA original donde el mercado debe moverse 50 puntos antes de que el stop se ajuste.
- Posiciones largas: una vez que se activa el disparador de trailing, el stop se actualiza a la banda inferior actual. Si el mínimo de la vela toca ese nivel, la estrategia sale con una orden de mercado.
- Posiciones cortas: después de que el disparador se activa, el stop sigue la banda superior actual. Si el máximo de la vela alcanza ese precio, la posición se cierra.
- Cuando el trailing stop fuerza una salida, la estrategia no abre una nueva posición hasta que la siguiente señal y el período de espera lo permitan.

### Gestión de riesgo
- La estrategia siempre opera una sola posición cuyo tamaño está definido por el parámetro `Volume`.
- No hay objetivo de beneficio; todas las salidas están impulsadas por la lógica de trailing del Donchian.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Volume` | Tamaño de orden para las entradas. | `1` |
| `ChannelPeriod` | Período de retrospección para el cálculo del Canal Donchian. | `20` |
| `BufferSteps` | Número de pasos de precio que el precio debe superar más allá de la banda opuesta antes de que se active el trailing (MQL usó 50 puntos). | `50` |
| `TradeCooldown` | Tiempo mínimo entre nuevas entradas. | `1 día` |
| `CandleType` | Serie de velas usada para el indicador (velas de 1 hora por defecto). | `velas de 1h` |

## Indicadores
- **Canales Donchian** – las bandas superior e inferior definen señales de ruptura y stops dinámicos.

## Notas
- Use instrumentos con un `PriceStep` razonable para que el buffer se traduzca en una distancia de precio realista. La estrategia usa un paso de 0.0001 por defecto si el instrumento no proporciona ninguno.
- Solo una dirección está abierta a la vez. Antes de cambiar de dirección, la posición existente debe cerrarse completamente, igual que el asesor experto original.
- Los objetos de gráfico se preparan automáticamente si hay un área de gráfico disponible: velas, el canal Donchian y las propias operaciones de la estrategia.
