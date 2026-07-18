# Estrategia Color JFATL Digit Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Color JFATL Digit Duplex es un sistema de módulo dual convertido del asesor experto de MetaTrader 5 `Exp_ColorJFatl_Digit_Duplex`. Opera dos flujos de señal independientes basados en el indicador Color Jurik Fast Adaptive Trend Line (JFATL). El módulo largo busca transiciones alcistas en el mapa de colores del indicador, mientras que el módulo corto reacciona a las transiciones bajistas. Cada lado tiene sus propios parámetros de suavizado, fuente de precio, precisión de redondeo, desplazamiento de barra y offsets de protección.

La implementación de StockSharp utiliza la API de alto nivel con suscripciones de velas y una clase de indicador dedicada que reproduce los pesos del kernel FATL y el suavizado Jurik. El indicador genera el valor JFATL redondeado junto con los códigos de color actual y anterior necesarios para la detección de señales.

## Lógica del indicador
1. **Convolución FATL** – los últimos 39 precios (seleccionados por la opción de precio aplicado) se ponderan con los coeficientes FATL originales para producir una serie filtrada.
2. **Suavizado Jurik** – la salida FATL se pasa a través de una Jurik Moving Average (JMA). El parámetro de fase se emula aplicando un ajuste diferencial que desplaza el valor suavizado hacia adelante o hacia atrás.
3. **Redondeo de dígitos** – el resultado se redondea al número especificado de dígitos para imitar la salida "digitalizada" del indicador original.
4. **Asignación de color** – el búfer de color se establece en 2 cuando el valor actual sube, 0 cuando cae, y de lo contrario hereda el color anterior. Un parámetro configurable `SignalBar` selecciona qué barra histórica inspeccionar, junto con su barra anterior.

El indicador devuelve un valor complejo que contiene la lectura JFATL redondeada, el color en `SignalBar`, el color anterior y el tiempo de cierre de la barra de señal. Los manejadores de estrategia usan esta información para identificar transiciones de estado exactamente como en el código de MetaTrader.

## Reglas de trading
- **Módulo largo**
  - Abre una posición larga cuando el color en `SignalBar` cambia a 2 mientras el color anterior no era 2 y no hay exposición larga presente.
  - Cierra una posición larga existente cuando el color en `SignalBar` se convierte en 0.
- **Módulo corto**
  - Abre una posición corta cuando el color en `SignalBar` cambia a 0 mientras el color anterior estaba por encima de 0 y no hay exposición corta presente.
  - Cierra una posición corta existente cuando el color en `SignalBar` se convierte en 2.
- **Manejo de posición** – las órdenes tienen el tamaño necesario para eliminar la exposición opuesta antes de abrir una nueva operación en el otro lado. `ClosePosition()` se usa para las salidas de modo que la estrategia mantiene una posición neta única en cualquier momento.

## Gestión de riesgo
Cada módulo tiene distancias individuales de stop-loss y take-profit expresadas en pasos de precio. Cuando se abre una nueva posición, la estrategia registra el precio de entrada y calcula los niveles de protección usando el `PriceStep` de seguridad actual. En cada actualización del indicador se prueba el máximo/mínimo de la vela correspondiente contra los niveles almacenados:

- Para operaciones largas, la estrategia cierra la posición si el mínimo de la vela alcanza el precio de stop o si el máximo de la vela alcanza el precio de take-profit.
- Para operaciones cortas, la lógica se invierte usando el máximo de la vela para el stop y el mínimo para el take-profit.

Deshabilitar el stop o el take estableciendo la distancia en cero deja la operación sin gestionar hasta que el indicador emita una señal de salida.

## Parámetros
| Grupo | Parámetro | Descripción |
| --- | --- | --- |
| General | `LongCandleType` | Marco temporal utilizado para la suscripción del indicador largo. |
| General | `ShortCandleType` | Marco temporal utilizado para la suscripción del indicador corto. |
| Indicador (Long) | `LongJmaLength` | Longitud de la media móvil Jurik para el módulo largo. |
| Indicador (Long) | `LongJmaPhase` | Ajuste de fase aplicado a la salida JMA larga (rango −100…100). |
| Indicador (Long) | `LongAppliedPrice` | Fuente de precio aplicado usado en la convolución FATL. |
| Indicador (Long) | `LongDigit` | Número de dígitos utilizados para redondear el valor del indicador. |
| Indicador (Long) | `LongSignalBar` | Offset de barra histórica inspeccionada para señales (0 = barra cerrada actual). |
| Riesgo (Long) | `LongStopLossPoints` | Distancia de stop-loss para largos medida en pasos de precio. |
| Riesgo (Long) | `LongTakeProfitPoints` | Distancia de take-profit para largos medida en pasos de precio. |
| Trading (Long) | `EnableLongOpen` | Habilita o deshabilita nuevas entradas largas. |
| Trading (Long) | `EnableLongClose` | Habilita o deshabilita salidas largas generadas por el indicador. |
| Indicador (Short) | `ShortJmaLength` | Longitud de la media móvil Jurik para el módulo corto. |
| Indicador (Short) | `ShortJmaPhase` | Ajuste de fase aplicado a la salida JMA corta. |
| Indicador (Short) | `ShortAppliedPrice` | Fuente de precio aplicado para el indicador corto. |
| Indicador (Short) | `ShortDigit` | Número de dígitos utilizados para redondear el valor del indicador corto. |
| Indicador (Short) | `ShortSignalBar` | Offset de barra histórica inspeccionada para señales cortas. |
| Riesgo (Short) | `ShortStopLossPoints` | Distancia de stop-loss para cortos medida en pasos de precio. |
| Riesgo (Short) | `ShortTakeProfitPoints` | Distancia de take-profit para cortos medida en pasos de precio. |
| Trading (Short) | `EnableShortOpen` | Habilita o deshabilita nuevas entradas cortas. |
| Trading (Short) | `EnableShortClose` | Habilita o deshabilita salidas cortas generadas por el indicador. |

## Notas de uso
1. Asigne tipos de vela apropiados para los módulos largo y corto. Pueden apuntar a diferentes marcos temporales si se desea.
2. Configure el precio aplicado y los dígitos de redondeo para que coincidan con las características del instrumento del Asesor Experto original.
3. El parámetro `SignalBar` controla cuántas velas cerradas atrás se valida la señal. Configúrelo en 1 para replicar el valor predeterminado de MT5 (vela completada anterior).
4. Asegúrese de que la propiedad `Volume` de la estrategia refleje el tamaño de operación deseado. Al revertir posiciones, la estrategia agrega automáticamente la magnitud de la exposición existente para que la posición neta cambie correctamente.
5. Los stops y objetivos dependen del `PriceStep` de seguridad. Para instrumentos sin un tamaño de tick definido, los offsets predeterminan a pasos numéricos brutos.

## Notas de conversión
- El parámetro de fase Jurik en StockSharp se emula aplicando un ajuste de avance/retraso diferencial porque el `JurikMovingAverage` empaquetado no expone una propiedad de fase directa. Esto preserva el comportamiento del experto original, incluyendo respuestas agresivas o retrasadas.
- La estrategia utiliza un modelo de posición neta única. La versión de MetaTrader podría ejecutar múltiples órdenes por dirección; en StockSharp la lógica las consolida en una exposición larga o corta a la vez.
- Los niveles de protección se evalúan en cada cierre de vela del indicador en lugar de en cada tick. Esto coincide con la frecuencia de señal del experto MT5 y mantiene la implementación dentro de las pautas de la API de alto nivel.

## Archivos
- `CS/ColorJfatlDigitDuplexStrategy.cs` – implementación de la estrategia con el indicador personalizado.
- `README.md` / `README_zh.md` / `README_ru.md` – documentación en inglés, chino y ruso.
