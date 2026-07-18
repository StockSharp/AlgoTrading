# Estrategia BrainTrend2 V2 Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia BrainTrend2 V2 Duplex es un port de alto nivel para StockSharp del experto MetaTrader 5 original `Exp_BrainTrend2_V2_Duplex`. Ejecuta dos instancias independientes del indicador BrainTrend2 V2: una dedicada a oportunidades largas y otra ajustada para oportunidades cortas. Cada lado puede operar en su propia serie de velas, longitud ATR y desplazamiento de señal, lo que permite a la estrategia combinar confirmaciones multi-marco temporal o configuraciones de riesgo asimétricas.

BrainTrend2 V2 es un motor de seguimiento de tendencia que construye un canal dinámico de "río" comparando el rango verdadero más reciente con un promedio ATR ponderado. El indicador pinta las velas con cinco colores distintos:

- **0** – Vela alcista dentro de un río de tendencia alcista.
- **1** – Vela bajista dentro de un río de tendencia alcista.
- **2** – Marcador de posición neutro mientras el río cambia de dirección.
- **3** – Vela alcista dentro de un río de tendencia bajista.
- **4** – Vela bajista dentro de un río de tendencia bajista.

La estrategia duplex interpreta esas transiciones de color para activar entradas y salidas, reflejando estrechamente las reglas codificadas en el experto MQL5.

## Lógica de operación
### Lado largo
- Evaluar el indicador en la vela ubicada `Long Signal Bar` pasos atrás (predeterminado 1 = la barra terminada anterior).
- Abrir una posición larga cuando:
  - El color en la barra `SignalBar + 1` (dos barras atrás) era **menor que 2** (tonos verdes de un río de tendencia alcista), **y**
  - El color en la barra `SignalBar` es **mayor que 1** (transición fuera del estado puramente alcista).
- Cerrar una posición larga existente cuando el color en la barra `SignalBar + 1` es **mayor que 2** (tonos magenta producidos por el río de tendencia bajista).

### Lado corto
- Evaluar el indicador en la vela ubicada `Short Signal Bar` pasos atrás (predeterminado 1).
- Abrir una posición corta cuando:
  - El color en la barra `SignalBar + 1` era **mayor que 2** (tonos magenta), **y**
  - El color en la barra `SignalBar` es **mayor que 0** (cualquier cosa excepto una vela puramente alcista).
- Cerrar una posición corta existente cuando el color en la barra `SignalBar + 1` es **menor que 2** (retorno al río de tendencia alcista).

Cuando se emite una nueva orden, la estrategia automáticamente compensa cualquier exposición opuesta. Por ejemplo, una solicitud de entrada corta primero recomprará la posición larga actual (si la hay) y luego enviará la orden de venta por el volumen corto configurado.

## Gestión de riesgo
- Ambos lados pueden especificar distancias independientes de stop-loss y take-profit en puntos. Un valor de `0` deshabilita el bracket respectivo.
- Los stops y objetivos se calculan en precios absolutos usando el paso de precio del instrumento. Los largos monitorean el mínimo/máximo de la vela, los cortos monitorean el máximo/mínimo para emular la ejecución intrabarra.
- El tamaño de posición se expresa directamente en unidades de operación y puede diferir entre los flujos largo y corto.
- La estrategia también habilita `StartProtection()` para integrarse con cualquier salvaguarda a nivel de cartera configurada dentro de StockSharp.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Long Candle Type` | Tipo de datos de vela usado para el indicador largo (marco temporal). | Marco temporal H4 |
| `Long ATR Period` | Período ATR usado en el cálculo de BrainTrend2 V2 para el flujo largo. | 7 |
| `Long Signal Bar` | Desplazamiento histórico (en barras) evaluado para decisiones largas. | 1 |
| `Enable Long Entries` | Permite o bloquea nuevas órdenes largas. | true |
| `Enable Long Exits` | Permite o bloquea salidas largas generadas por el indicador. | true |
| `Long Volume` | Tamaño de orden base para entradas largas. | 1 |
| `Long Stop Loss` | Distancia de stop-loss en puntos para operaciones largas (0 = deshabilitado). | 1000 |
| `Long Take Profit` | Distancia de take-profit en puntos para operaciones largas (0 = deshabilitado). | 2000 |
| `Short Candle Type` | Tipo de datos de vela usado para el indicador corto. | Marco temporal H4 |
| `Short ATR Period` | Período ATR usado en el cálculo de BrainTrend2 V2 para el flujo corto. | 7 |
| `Short Signal Bar` | Desplazamiento histórico (en barras) evaluado para decisiones cortas. | 1 |
| `Enable Short Entries` | Permite o bloquea nuevas órdenes cortas. | true |
| `Enable Short Exits` | Permite o bloquea salidas cortas generadas por el indicador. | true |
| `Short Volume` | Tamaño de orden base para entradas cortas. | 1 |
| `Short Stop Loss` | Distancia de stop-loss en puntos para operaciones cortas (0 = deshabilitado). | 1000 |
| `Short Take Profit` | Distancia de take-profit en puntos para operaciones cortas (0 = deshabilitado). | 2000 |

## Notas de uso
- Use desplazamientos de señal más grandes para esperar confirmación adicional de velas o combine diferentes marcos temporales asignando tipos de velas distintos a los flujos largo y corto.
- Dado que la estrategia usa una implementación personalizada de BrainTrend2, no depende de ningún archivo de indicador externo; todo está contenido en la clase C#.
- Los stops y objetivos se gestionan en cada vela terminada. Cuando se ejecuta con datos en vivo, considere usar un intervalo de velas suficientemente pequeño si requiere un control de riesgo más ajustado.
- Establecer tanto las distancias de stop como de take-profit en cero mantiene las posiciones abiertas hasta que aparece un trigger de color opuesto.
- El buffer del indicador se inicializa una vez que se han procesado suficientes velas iguales al período ATR. Hasta ese momento no se toman decisiones de operación.
