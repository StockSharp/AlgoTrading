# Estrategia TradeEATemplateNews
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia TradeEATemplateNews es una conversión en C# del asesor experto de MetaTrader 4 "Trade EA Template for News". El sistema original pausaba el trading en torno a eventos económicos programados descargados de sitios web externos. Este port de StockSharp mantiene las ideas principales mientras las adapta a la API de alto nivel:

- Usa velas completadas del marco temporal configurado (H1 por defecto).
- Opera sólo cuando la cuenta está plana, exactamente como la plantilla MQL que requería cero órdenes abiertas.
- Aplica una zona de silencio manual de noticias económicas que bloquea entradas antes y después de los eventos según su importancia.
- Crea automáticamente brackets protectores de stop-loss y take-profit a 100 puntos del precio de ejecución (convertidos a través del paso del instrumento).

## Lógica de trading
1. Cada vela completada activa un recálculo del calendario de noticias. La estrategia almacena el precio de apertura de la vela anterior para que la siguiente barra pueda comparar su cierre con la apertura previa.
2. Si el tiempo actual cae dentro de cualquier ventana de silencio configurada, la estrategia cancela órdenes pendientes y no abre nuevas operaciones.
3. Cuando no hay posición abierta y el trading está permitido:
   - Se abre una posición larga si la última vela cierra por encima del precio de apertura de la vela anterior.
   - Se abre una posición corta si la última vela cierra por debajo del precio de apertura de la vela anterior.
4. Los niveles de stop-loss y take-profit se expresan en puntos (`TakeProfitPoints` y `StopLossPoints`) y se convierten en desplazamientos de precio absolutos usando el valor `Step` del instrumento.

## Calendario de noticias manual
El experto original descargaba datos de investing.com o DailyFX. Para portabilidad, la versión StockSharp espera un calendario curado manualmente suministrado a través del parámetro `NewsEventsDefinition`. El formato acepta una lista de entradas separadas por punto y coma o saltos de línea. Cada entrada debe contener al menos tres campos separados por comas:

```
AAAA-MM-DD HH:MM,DIVISAS,IMPORTANCIA[,TÍTULO]
```

- `AAAA-MM-DD HH:MM` — inicio del evento en UTC. El parámetro opcional `TimeZoneOffsetHours` desplaza todos los tiempos parseados por la cantidad solicitada (por ejemplo, establece `3` para UTC+3).
- `DIVISAS` — códigos de divisas o identificadores de instrumentos como `USD`, `EUR`, `EUR/USD`. Múltiples códigos pueden separarse con `/`, `,`, `;`, `|` o espacios.
- `IMPORTANCIA` — palabra clave de importancia. Valores reconocidos: `Low`, `Medium`, `Mid`, `Midle`, `Moderate`, `High`, `NFP`, cadenas que contengan `Nonfarm` o `Non-farm`.
- `TÍTULO` — descripción de texto libre opcional que se imprimirá en los mensajes de registro.

Ejemplo:

```
2024-03-01 13:30,USD,High,Nonfarm Payrolls;2024-03-01 15:00,USD,Low,Factory Orders
```

### Ventanas de silencio
- `UseLowNews`, `UseMediumNews`, `UseHighNews` y `UseNfpNews` alternan qué eventos se consideran.
- `LowMinutesBefore/After`, `MediumMinutesBefore/After`, `HighMinutesBefore/After` y `NfpMinutesBefore/After` determinan cuántos minutos alrededor del evento se debe deshabilitar el trading.
- `OnlySymbolNews` restringe el silencio a entradas cuyos códigos de divisas coincidan con el instrumento actual (por ejemplo, `EURUSD` resulta en el par `{EUR, USD}`). Desactívalo para pausar el trading en cada evento.
- La estrategia mantiene sólo el evento de mayor importancia activo en cualquier momento. Los mensajes de registro informativos anuncian la razón del estado actual y la próxima publicación programada.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Tipo de datos de velas al que suscribirse. Por defecto 1 hora. | `1h` |
| `UseLowNews` | Habilitar eventos de baja importancia. | `true` |
| `LowMinutesBefore` / `LowMinutesAfter` | Minutos antes/después de noticias de bajo impacto para bloquear entradas. | `15 / 15` |
| `UseMediumNews` | Habilitar eventos de importancia media. | `true` |
| `MediumMinutesBefore` / `MediumMinutesAfter` | Minutos antes/después de noticias de impacto medio. | `30 / 30` |
| `UseHighNews` | Habilitar eventos de alta importancia. | `true` |
| `HighMinutesBefore` / `HighMinutesAfter` | Minutos antes/después de noticias de alto impacto. | `60 / 60` |
| `UseNfpNews` | Habilitar el indicador de Non-farm Payrolls. | `true` |
| `NfpMinutesBefore` / `NfpMinutesAfter` | Minutos antes/después de eventos NFP. | `180 / 180` |
| `OnlySymbolNews` | Filtrar el calendario por los códigos de divisas del instrumento actual. | `true` |
| `NewsEventsDefinition` | Cadena de descripción del calendario económico manual. | vacío |
| `TimeZoneOffsetHours` | Desplazamiento aplicado a cada evento parseado (UTC por defecto). | `0` |
| `TakeProfitPoints` | Distancia en puntos para la orden protectora de take-profit. | `100` |
| `StopLossPoints` | Distancia en puntos para la orden protectora de stop-loss. | `100` |

`Volume` se hereda de `Strategy` y debe establecerse según el tamaño de posición deseado.

## Diferencias con la versión MQL
- Sin descarga HTTP automática — el usuario suministra la lista de noticias manualmente, lo que evita dependencias externas y mantiene la conversión determinista.
- Las etiquetas de gráfico y líneas verticales se reemplazan con mensajes de registro que describen el evento activo o próximo.
- El experto MQL abría órdenes con tamaño de lote fijo `0.01`; en StockSharp el tamaño de posición proviene de la propiedad `Volume`.
- Toda la lógica se implementa con la API de suscripción de velas de alto nivel preservando el comportamiento consciente de noticias de la plantilla.

## Notas de despliegue
1. Llena `NewsEventsDefinition` antes de iniciar la estrategia o actualízalo, detén y reinicia para recargar el calendario.
2. Ajusta `TimeZoneOffsetHours` y los parámetros de minutos antes/después para que coincidan con tu sesión de trading.
3. Configura `Volume`, portafolio e instrumento en la interfaz o en código, luego inicia la estrategia.
4. Observa el registro de la estrategia para mensajes como "Trading paused due to high news" o "Next scheduled news" para confirmar la lógica de silencio.

La traducción a Python se omite intencionalmente según lo solicitado.
