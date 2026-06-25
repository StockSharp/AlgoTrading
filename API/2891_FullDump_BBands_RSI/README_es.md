# Estrategia FullDump BB RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema de múltiples pasos basado en Bollinger Bands y RSI, convertido desde el asesor experto MT5 "FullDump". La estrategia espera el agotamiento del impulso, confirma un sesgo de reversión a la media con Bollinger Bands y solo opera cuando el precio se realinea con la banda media. La gestión de operaciones refleja el EA original con offsets fijos de stop-loss/objetivo y un ajuste de break-even cuando el precio regresa a la banda opuesta.

## Descripción General

- **Mercados**: Cualquier instrumento líquido que soporte Bollinger Bands y RSI.
- **Marco temporal**: Tipo de vela configurable (predeterminado 15 minutos).
- **Dirección**: Largo/Corto.
- **Tipo de Orden**: Órdenes de mercado con niveles de protección predefinidos.
- **Concepto**: Desvanecimiento de extremos a corto plazo dentro del envelope de Bollinger mientras el precio revierte hacia la banda media.

## Lógica de Trading

1. **Escaneo RSI (Paso 1)**
   - La condición de largo requiere al menos una lectura RSI por debajo de 30 dentro de la ventana reciente.
   - La condición de corto requiere al menos una lectura RSI por encima de 70 dentro del mismo lookback.
2. **Violación de banda (Paso 2)**
   - Largo: el cierre actual debe estar por debajo o igual a cualquiera de los valores recientes de la banda inferior.
   - Corto: el cierre actual debe estar por encima o igual a cualquiera de los valores recientes de la banda superior.
3. **Alineación con banda media (Paso 3)**
   - Los trades en largo solo se activan una vez que el precio cierra de nuevo por encima de la línea media de Bollinger.
   - Los trades en corto requieren que el cierre esté por debajo de la línea media.
4. **Ejecución de entrada**
   - Cuando todas las condiciones coinciden y no hay posición abierta en esa dirección, se envía una orden de mercado por el volumen configurado.

## Gestión de Riesgo

- **Stop-loss**: Colocado por debajo (largo) o por encima (corto) del mínimo/máximo extremo de la ventana de lookback menos/más el offset de sangría configurado.
- **Take-profit**: Colocado en la banda de Bollinger opuesta actual más el mismo offset de sangría.
- **Regla break-even**: Una vez que el precio toca la banda opuesta, el stop-loss se mueve al precio de entrada para asegurar la posición.
- **Salida de posición**: Las posiciones se cierran cuando el precio supera los niveles de stop-loss o take-profit; señales opuestas aplastan la posición actual antes de cambiar de dirección.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `BandsPeriod` | Longitud del cálculo de Bollinger Bands. | 20 | Optimizable (10 → 40 paso 1). |
| `RsiPeriod` | Longitud de promediado para el RSI. | 14 | Optimizable (7 → 21 paso 1). |
| `Depth` | Número de velas recientes inspeccionadas para condiciones. | 6 | Optimizable (3 → 12 paso 1). |
| `IndentInPoints` | Offset en pasos de precio agregado al stop-loss y take-profit. | 10 | Optimizable (5 → 30 paso 5). |
| `OrderVolume` | Tamaño de la orden en lotes. | 1 | Usado tanto para entradas como salidas. |
| `CandleType` | Marco temporal de las velas de entrada. | Velas de 15 minutos | Cambiar para adaptar el horizonte de la estrategia. |

## Filtros y Etiquetas

- **Categoría**: Reversión a la media, bandas de volatilidad.
- **Indicadores**: Bollinger Bands, Relative Strength Index.
- **Stops**: Stop duro, objetivo duro, ajuste break-even.
- **Complejidad**: Intermedio (lógica multi-condición con gestión con estado).
- **Nivel de Automatización**: Entradas y salidas totalmente automatizadas.
- **Mejor Uso**: Fases de rango limitado donde los extremos de Bollinger frecuentemente revierten hacia la mediana.

## Notas

- El offset de sangría se escala por el paso de precio del instrumento para coincidir con la lógica basada en pips del EA original.
- El algoritmo mantiene colas de los valores recientes del indicador para replicar exactamente las comprobaciones de profundidad MT5.
- Asegurarse de que el instrumento proporcione suficientes velas históricas para inicializar tanto RSI como Bollinger Bands antes del trading en vivo.
