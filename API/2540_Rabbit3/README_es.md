# Estrategia Rabbit3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto original de MetaTrader 5 `Rabbit3 (edición de barabashkakvn)`.
- Implementa la lógica en el API de alto nivel de StockSharp con suscripciones de velas y vinculaciones de indicadores.
- Se centra en una doble confirmación entre Williams %R y el Índice del Canal de Materias Primas (CCI) antes de apilar posiciones.
- Agrega dimensionamiento dinámico de posiciones: las ganancias que superan un umbral en efectivo aumentan el volumen de la orden para la siguiente señal.

## Lógica de trading
### Condiciones de entrada
1. **Largo**
   - Las velas cerradas actuales y anteriores reportan Williams %R por debajo de `WilliamsOversold` (predeterminado `-80`).
   - El valor CCI está por debajo de `CciBuyLevel` (predeterminado `-80`).
   - La posición neta actual es no negativa y añadir otra posición mantiene la exposición dentro de `MaxPositions` × `BaseVolume` (se usa el volumen aumentado cuando está activo).
2. **Corto**
   - Las velas cerradas actuales y anteriores reportan Williams %R por encima de `WilliamsOverbought` (predeterminado `-20`).
   - El valor CCI está por encima de `CciSellLevel` (predeterminado `80`).
   - La posición neta actual es no positiva y la nueva orden permanece dentro del límite de apilamiento configurado.

### Salida y control de riesgo
- Los órdenes de stop-loss y take-profit protectores se registran automáticamente a través de `StartProtection`.
- Las distancias se expresan en "puntos ajustados": cuando el instrumento usa 3 o 5 decimales, la estrategia multiplica el paso de precio por 10 para emular la aritmética de pips de MetaTrader antes de aplicar `StopLossPips` y `TakeProfitPips`.
- No se requieren reglas de salida manuales adicionales; los órdenes protectores cierran las operaciones.

### Dimensionamiento de posiciones
- `BaseVolume` define el tamaño inicial de la operación (predeterminado `0.01`).
- Después de que se cierra cada operación, el delta de PnL realizado se compara con `ProfitThreshold` (predeterminado `4` unidades monetarias).
- Si el delta es estrictamente mayor, la siguiente señal usa `BaseVolume × VolumeMultiplier` (predeterminado `1.6`). De lo contrario, el tamaño se restablece a `BaseVolume`.
- El volumen actual también se expone a través de la propiedad `Volume` de la estrategia para retroalimentación de la interfaz.

### Indicadores y visualización
- Williams %R, CCI, una EMA rápida (`FastEmaPeriod`) y una EMA lenta (`SlowEmaPeriod`) están vinculadas al feed de velas para monitoreo y representación gráfica.
- Se crea automáticamente un área de gráfico que muestra velas, indicadores y operaciones ejecutadas.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | marco temporal de `1h` | Tipo de datos para la suscripción de velas. |
| `CciPeriod` | `15` | Longitud del Índice del Canal de Materias Primas. |
| `CciBuyLevel` | `-80` | Umbral CCI que permite entradas largas. |
| `CciSellLevel` | `80` | Umbral CCI que permite entradas cortas. |
| `WilliamsPeriod` | `62` | Período de lookback para Williams %R. |
| `WilliamsOversold` | `-80` | Umbral de sobreventa utilizado para confirmación larga. |
| `WilliamsOverbought` | `-20` | Umbral de sobrecompra utilizado para confirmación corta. |
| `FastEmaPeriod` | `17` | EMA rápida trazada para el contexto de tendencia. |
| `SlowEmaPeriod` | `30` | EMA lenta trazada para el contexto de tendencia. |
| `MaxPositions` | `2` | Número máximo de entradas apiladas por dirección. |
| `ProfitThreshold` | `4` | Beneficio realizado necesario para aumentar el tamaño de la siguiente orden (unidades monetarias). |
| `BaseVolume` | `0.01` | Volumen base de la orden. |
| `VolumeMultiplier` | `1.6` | Multiplicador aplicado cuando se cumple la condición de aumento. |
| `StopLossPips` | `45` | Distancia del stop-loss en puntos ajustados. |
| `TakeProfitPips` | `110` | Distancia del take-profit en puntos ajustados. |

## Notas
- La estrategia opera sobre posiciones netas. A diferencia de la versión MQL compatible con cobertura, los largos y cortos no se mantienen simultáneamente; las señales en la dirección opuesta se ignoran hasta que la exposición actual sea cerrada por órdenes protectoras.
- `MaxPositions` funciona como un límite en la posición agregada (volumen base multiplicado por el factor de apilamiento). Ajústelo cuidadosamente al cambiar los volúmenes base o aumentados.
- La tolerancia de volumen usa la mitad del paso de volumen del instrumento para absorber diferencias menores de redondeo al verificar el límite de apilamiento.
- La traducción a Python aún no está incluida y se puede agregar más adelante si es necesario.
