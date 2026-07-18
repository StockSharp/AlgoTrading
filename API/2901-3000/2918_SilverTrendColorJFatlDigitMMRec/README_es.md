# Estrategia SilverTrend ColorJFatl Digit MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader `Exp_SilverTrend_ColorJFatl_Digit_MMRec`. Recrea la arquitectura de doble bloque donde dos módulos de lógica independientes gestionan sus propios tamaños de posición virtuales y los combinan en la posición final de la estrategia:

- **Bloque SilverTrend** – lee los colores de las velas producidos por el indicador SilverTrend para detectar cuándo el precio cruza los bordes del canal adaptativo.
- **Bloque ColorJFatl** – calcula una FATL (Fast Adaptive Trend Line) filtrada usando la tabla de pesos publicada y un suavizador basado en EMA que emula la media móvil Jurik usada en MetaTrader.

Ambos módulos pueden abrir operaciones largas y cortas de forma independiente, cerrar exposición opuesta en nuevas señales y aplicar sus propias distancias de stop-loss y take-profit. La posición final de la estrategia es igual a la suma de las posiciones virtuales gestionadas por los dos bloques.

## Configuración predeterminada

- Instrumento: el instrumento de la estrategia seleccionado en StockSharp.
- Marcos temporales: ambos módulos usan velas de 6 horas por defecto (configurables a través de parámetros).
- Tamaño de orden: cada módulo envía órdenes de mercado con un parámetro de volumen separado (predeterminado `1`).

## Indicadores y lógica de señal

### Bloque SilverTrend

1. Construye un canal de precio deslizante desde las últimas `SSP` velas.
2. Aplica el desplazamiento `Risk` original `(33 - Risk) / 100` para mover los bordes del canal dentro del rango alto/bajo.
3. Colorea cada vela según la tendencia activa (`0`/`1` alcista, `3`/`4` bajista, `2` neutral) como el indicador de MetaTrader.
4. Señales:
   - **Largo** cuando la vela en la `Signal Bar` configurada se vuelve alcista mientras la barra anterior no lo era (`color < 2` y anterior `> 1`).
   - **Corto** cuando se vuelve bajista mientras la barra anterior no lo era (`color > 2` y anterior `< 3`).
5. Los niveles opcionales de stop-loss y take-profit se miden en puntos usando el paso de precio del instrumento.

### Bloque ColorJFatl

1. Construye un valor FATL aplicando la tabla de coeficientes oficial a la fuente de `Applied Price` elegida.
2. Suaviza el resultado con una EMA de longitud `JMA Length` (el input de fase Jurik se conserva para compatibilidad y documentación).
3. Colorea la línea FATL según la pendiente: `2` para subiendo, `0` para bajando, y `1` para segmentos planos.
4. Señales:
   - **Largo** cuando el color FATL cambia a `2` mientras el color anterior era `0` o `1`.
   - **Corto** cuando el color cambia a `0` mientras el valor anterior era `1` o `2`.
5. Cada dirección puede opcionalmente cerrar la posición del bloque opuesto antes de abrir una nueva operación.

## Gestión de riesgo

- SilverTrend y ColorJFatl cada uno mantiene su propio precio de entrada y distancias de stop/objetivo.
- Si se alcanza un stop o objetivo, solo se cierra el bloque afectado (el otro bloque puede permanecer abierto).
- Cuando ambos bloques coinciden en la misma dirección, sus volúmenes se acumulan.

## Parámetros

| Grupo | Nombre | Descripción |
| --- | --- | --- |
| SilverTrend | `Silver Candle Type` | Suscripción de velas usada para el indicador SilverTrend. |
| SilverTrend | `SSP` | Longitud del rango alto/bajo deslizante. |
| SilverTrend | `Risk` | Factor de contracción del canal (input `Risk` original). |
| SilverTrend | `Signal Bar` | Desplazamiento de barra usado para la señal (0 = barra cerrada actual, 1 = barra anterior, etc.). |
| SilverTrend | `Allow Silver Long/Short` | Habilitar entradas para cada dirección. |
| SilverTrend | `Close Silver Long/Short` | Permitir el cierre automático de la posición opuesta. |
| SilverTrend | `Silver Volume` | Volumen para las operaciones abiertas por el bloque SilverTrend. |
| SilverTrend | `Silver SL/TP` | Distancias de stop-loss y take-profit en puntos. |
| ColorJFatl | `Color Candle Type` | Suscripción de velas usada para los cálculos FATL. |
| ColorJFatl | `JMA Length` | Longitud del suavizador EMA que emula JMA. |
| ColorJFatl | `JMA Phase` | Conservado para completitud (sin influencia directa dentro de StockSharp). |
| ColorJFatl | `Applied Price` | Precio fuente (cierre, mediano, típico, seguidor de tendencia, etc.). |
| ColorJFatl | `Digits` | Precisión decimal aplicada al valor FATL. |
| ColorJFatl | `Color Signal Bar` | Desplazamiento de barra usado para señales FATL. |
| ColorJFatl | Interruptores `Allow/Close` | Habilitar entradas y salidas automáticas para cada dirección. |
| ColorJFatl | `Color Volume` | Volumen para las operaciones abiertas por el bloque ColorJFatl. |
| ColorJFatl | `Color SL/TP` | Distancias de stop-loss y take-profit en puntos para el bloque. |

## Notas

- La estrategia se suscribe a ambos flujos de velas incluso si son idénticos. Las suscripciones duplicadas son gestionadas internamente por StockSharp.
- El parámetro de fase Jurik se conserva para mantenerse cerca del asesor experto original. El suavizador basado en EMA de StockSharp replica el comportamiento curvo de FATL mientras mantiene el parámetro disponible para futuras extensiones.
- Asegurarse de que el instrumento tiene `PriceStep` configurado para usar límites de riesgo basados en puntos.

## Consejos de uso

1. Establecer la propiedad `Volume` de la estrategia o ajustar los parámetros de volumen específicos de cada bloque para controlar la exposición absoluta.
2. Usar los indicadores de habilitar/deshabilitar para probar cada bloque por separado antes de combinarlos.
3. Debido a que los bloques operan de forma independiente, la estrategia puede mantener un neto largo y corto simultáneamente (por ejemplo, largo de SilverTrend y corto de ColorJFatl) – la posición resultante es la suma algebraica de ambos.
4. Optimizar `SSP`, `Risk` y `JMA Length` para el mercado objetivo si se planea usar búsqueda de parámetros automatizada.
