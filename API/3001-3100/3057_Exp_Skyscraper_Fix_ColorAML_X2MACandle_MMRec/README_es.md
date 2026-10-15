# Estrategia Exp Skyscraper Fix + ColorAML + X2MA Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión en C# del experto de MetaTrader **Exp_Skyscraper_Fix_ColorAML_X2MACandle_MMRec**.
- Combina tres filtros independientes basados en color (canal Skyscraper Fix, nivel adaptativo ColorAML, velas de doble suavizado X2MACandle).
- Cada filtro puede abrir o cerrar operaciones por sí solo mientras comparte el mismo símbolo, permitiendo seguimiento de tendencia cooperativo y reversales rápidas.
- Incluye un módulo de gestión monetaria simplificado: cuando las últimas operaciones de una dirección pierden repetidamente, el módulo cambia al volumen reducido (`SmallMM`).

## Lógica de la estrategia
### Bloque Skyscraper Fix
1. Construye el canal trailing de Skyscraper Fix analizando el rango ATR y la fuente de precio elegida (high/low o close).
2. Cuando el color del canal se vuelve alcista, el bloque:
   - cierra cualquier posición corta pendiente (si `Skyscraper Close Shorts` está habilitado);
   - puede abrir una nueva posición larga después del retraso de señal configurado (si `Skyscraper Buy` está habilitado).
3. Cuando el color se vuelve bajista, la lógica refleja los pasos para operaciones cortas.
4. Los envelopes de high/low, el multiplicador ATR (`Kv`) y el offset porcentual reproducen el comportamiento del indicador original.

### Bloque ColorAML
1. Calcula el Nivel de Mercado Adaptativo (AML) midiendo el rango de dos ventanas fractales consecutivas y suavizando el precio compuesto.
2. El indicador produce tres colores: `2` (alcista), `0` (bajista) y `1` (neutral). Las velas neutrales no desencadenan ninguna acción.
3. Un color alcista cierra cortos (si está permitido) y puede abrir un largo cuando el color cambió desde alcista en la vela anterior inspeccionada.
4. Un color bajista realiza las acciones simétricas para operaciones cortas.

### Bloque X2MACandle
1. Cascada de dos medias móviles configurables sobre cada componente OHLC (apertura, máximo, mínimo, cierre) para construir una vela sintética.
2. El color se determina por el cuerpo de la vela suavizada: alcista cuando cierre > apertura, bajista cuando cierre < apertura, neutral en caso contrario.
3. Un pequeño umbral de brecha (en pasos de precio) aplana cuerpos de velas muy pequeños para evitar flips rápidos de color.
4. Los colores alcistas cierran cortos y pueden abrir largos; los colores bajistas realizan lo opuesto.

### Gestión monetaria
1. Cada bloque mantiene un historial independiente de sus propias operaciones para las direcciones larga y corta.
2. Después de que una operación se cierra, el módulo registra si terminó con pérdida.
3. Si las últimas `Loss Trigger` operaciones para una dirección fueron todas pérdidas, la siguiente orden de ese bloque cambia al volumen reducido (`SmallMM`).
4. Cuando una operación rentable o neutral rompe la racha perdedora, el módulo vuelve automáticamente al volumen normal (`MM`).

## Parámetros
| Sección | Nombre | Descripción | Por defecto |
| --- | --- | --- | --- |
| Skyscraper | `Skyscraper Candle` | Marco temporal para muestrear velas para el indicador Skyscraper Fix. | 4h |
| Skyscraper | `Skyscraper Length` | Ventana de promedio ATR (número de velas). | 10 |
| Skyscraper | `Skyscraper Kv` | Multiplicador de sensibilidad aplicado al paso ATR. | 0.9 |
| Skyscraper | `Skyscraper Percentage` | Porcentaje adicional añadido/eliminado de la línea media. | 0 |
| Skyscraper | `Skyscraper Mode` | Fuente de precio (High/Low o Close) usada para los envelopes. | High/Low |
| Skyscraper | `Skyscraper Signal Bar` | Número de velas ya cerradas a esperar antes de actuar sobre un color. | 1 |
| Skyscraper | `Skyscraper Buy` / `Skyscraper Sell` | Permitir apertura de operaciones largas / cortas. | true |
| Skyscraper | `Skyscraper Close Long` / `Skyscraper Close Short` | Permitir a este bloque salir de operaciones largas / cortas. | true |
| Skyscraper | `Skyscraper Normal Volume` | Volumen base de orden (`MM` en el EA). | 0.1 |
| Skyscraper | `Skyscraper Reduced Volume` | Volumen reducido de orden usado tras una racha de pérdidas (`SmallMM`). | 0.01 |
| Skyscraper | `Skyscraper Buy Loss Trigger` / `Skyscraper Sell Loss Trigger` | Número de pérdidas consecutivas que cambian al volumen reducido. | 2 |
| ColorAML | `ColorAML Candle` | Tipo de vela usado por el indicador ColorAML. | 4h |
| ColorAML | `ColorAML Fractal` | Ventana fractal (en barras) usada para el cálculo de rango. | 6 |
| ColorAML | `ColorAML Lag` | Parámetro de lag que controla el suavizado adaptativo. | 7 |
| ColorAML | `ColorAML Signal Bar` | Offset de vela aplicado antes de evaluar colores. | 1 |
| ColorAML | `ColorAML Buy` / `ColorAML Sell` | Habilitar entradas largas / cortas generadas por ColorAML. | true |
| ColorAML | `ColorAML Close Long` / `ColorAML Close Short` | Permitir a ColorAML cerrar posiciones largas / cortas. | true |
| ColorAML | `ColorAML Normal Volume` / `ColorAML Reduced Volume` | Volúmenes base y reducido para este bloque. | 0.1 / 0.01 |
| ColorAML | `ColorAML Buy Loss Trigger` / `ColorAML Sell Loss Trigger` | Pérdidas consecutivas que activan el volumen reducido. | 2 |
| X2MA | `X2MA Candle` | Marco temporal usado para la reconstrucción de velas X2MACandle. | 4h |
| X2MA | `First Method` / `Second Method` | Métodos de suavizado para la primera y segunda media móvil. | SMA / JJMA |
| X2MA | `First Length` / `Second Length` | Períodos de las dos etapas de suavizado. | 12 / 5 |
| X2MA | `First Phase` / `Second Phase` | Fases de compatibilidad usadas por las medias móviles Jurik. | 15 |
| X2MA | `Gap Points` | Umbral de brecha (en pasos de precio) que aplana cuerpos de velas pequeños. | 10 |
| X2MA | `X2MA Signal Bar` | Offset de vela aplicado antes de reaccionar a los colores. | 1 |
| X2MA | `X2MA Buy` / `X2MA Sell` | Permitir apertura de operaciones largas / cortas desde el bloque X2MACandle. | true |
| X2MA | `X2MA Close Long` / `X2MA Close Short` | Permitir al bloque salir de posiciones largas / cortas. | true |
| X2MA | `X2MA Normal Volume` / `X2MA Reduced Volume` | Volúmenes base y reducido para operaciones X2MACandle. | 0.1 / 0.01 |
| X2MA | `X2MA Buy Loss Trigger` / `X2MA Sell Loss Trigger` | Número de pérdidas consecutivas antes de cambiar al volumen reducido. | 2 |

## Consejos de uso
1. Ajuste los tipos de velas para que coincidan con la volatilidad del mercado (por ejemplo, 1h para trading intradía, 4h para swing trading).
2. Los tres módulos pueden ajustarse independientemente; deshabilitar un bloque deja a los otros activos.
3. Los umbrales de gestión monetaria son intencionalmente conservadores. Aumente los disparadores si el instrumento tiene tendencias fuertes y quiere mantener el volumen base por más tiempo.
4. Dado que la estrategia depende de velas terminadas, siempre aliméntela con datos de velas que coincidan con los marcos temporales configurados.
