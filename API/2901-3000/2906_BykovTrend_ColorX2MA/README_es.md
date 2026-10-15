# Estrategia BykovTrend + ColorX2MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el indicador de tendencia de color BykovTrend V2 con el filtro de pendiente de media móvil de doble suavizado ColorX2MA. Ambos bloques lógicos operan en el mismo símbolo y pueden emitir órdenes de forma independiente, lo que permite que la posición neta refleje el último acuerdo entre las dos fuentes de señal.

## Descripción general

- **Sesgo de mercado**: Funciona en cualquier instrumento que soporte datos de velas. El marco temporal predeterminado para ambos bloques es 4 horas (H4), reflejando el Asesor Experto original.
- **Indicadores**:
  - *BykovTrend V2*: Usa Williams %R para colorear las velas según la tendencia predominante.
  - *ColorX2MA*: Aplica dos medias móviles consecutivas a una fuente de precio configurable y clasifica la dirección de la pendiente.
- **Señales**: Las entradas y salidas son generadas por separado por cada bloque. La posición final es la suma de todos los trades ejecutados.

## Bloque BykovTrend

1. Williams %R se calcula usando el período configurado (predeterminado 9).
2. Los umbrales se desplazan por `33 - Risk`. Cuando %R sube por encima de `-Risk` la tendencia local se vuelve alcista; cuando cae por debajo de `-100 + (33 - Risk)` la tendencia se vuelve bajista.
3. Colores de vela:
   - Verde/teal (códigos 0, 1): tendencia alcista.
   - Gris (código 2): neutral, sin cambio de tendencia.
   - Chocolate/dorado (códigos 3, 4): tendencia bajista.
4. Las señales se evalúan en la vela que está `SignalBar` pasos detrás de la última barra cerrada. Un valor de 1 significa la vela completada anterior, que coincide con la implementación MetaTrader.
5. Lógica de trading:
   - **Entrada larga**: Color actual < 2 (alcista) y color anterior > 1 (era neutral/bajista). Opcional a través de *Bykov Allow Long Entries*.
   - **Salida corta**: Color actual < 2. Opcional a través de *Bykov Allow Short Exits*.
   - **Entrada corta**: Color actual > 2 (bajista) y color anterior < 3 (era neutral/alcista). Opcional a través de *Bykov Allow Short Entries*.
   - **Salida larga**: Color actual > 2. Opcional a través de *Bykov Allow Long Exits*.

## Bloque ColorX2MA

1. Una primera media móvil suaviza el precio aplicado seleccionado (cierre por defecto) usando el método y longitud elegidos.
2. Una segunda media móvil suaviza la salida de la primera MA, nuevamente con un método y longitud configurables.
3. La pendiente del segundo suavizado define el flujo de color:
   - 1 (magenta): el valor aumentó desde la vela anterior.
   - 2 (violeta): el valor disminuyó.
   - 0 (gris): sin cambio.
4. Las señales se evalúan en la vela que está `SignalBar` pasos detrás del último cierre.
5. Lógica de trading:
   - **Entrada larga**: Color actual = 1 y color anterior ≠ 1. Opcional a través de *Color Allow Long Entries*.
   - **Salida corta**: Color actual = 1. Opcional a través de *Color Allow Short Exits*.
   - **Entrada corta**: Color actual = 2 y color anterior ≠ 2. Opcional a través de *Color Allow Short Entries*.
   - **Salida larga**: Color actual = 2. Opcional a través de *Color Allow Long Exits*.

## Gestión de Posición

- Las órdenes son órdenes de mercado. Al cambiar de dirección, la estrategia compra/vende suficientes contratos para neutralizar la posición existente y establecer una nueva de tamaño `Volume`.
- Cada bloque puede desencadenar una salida incluso si el otro bloque todavía favorece el lado actual; el efecto neto es un gradual tira y afloja entre los dos módulos.
- No se aplica stop-loss o take-profit automático. La gestión de riesgo debe manejarse externamente o ajustando los indicadores de permiso.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **BykovTrend Candle** | Tipo de datos (marco temporal) para el cálculo BykovTrend. |
| **Williams %R Period** | Retroceso para Williams %R. |
| **Risk Offset** | Desplaza los umbrales de Williams %R (`33 - Risk`). Valores más grandes aprietan los umbrales alcistas y aflojan los bajistas. |
| **Signal Bar** | Retraso (número de velas completadas) antes de actuar sobre un color BykovTrend. |
| **Allow Long/Short Entries** | Habilitar o deshabilitar entradas impulsadas por BykovTrend. |
| **Allow Long/Short Exits** | Habilitar o deshabilitar salidas impulsadas por BykovTrend. |
| **ColorX2MA Candle** | Tipo de datos (marco temporal) para el bloque ColorX2MA. |
| **First/Second MA Method** | Método de suavizado para cada etapa (SMA, EMA, SMMA, LWMA, Jurik). |
| **First/Second MA Length** | Longitud de período para cada etapa de suavizado. |
| **First/Second MA Phase** | Parámetro de compatibilidad retenido del EA original; la implementación actual lo mantiene para documentación, pero el suavizado Jurik usa sus valores predeterminados internos. |
| **Applied Price** | Fuente de precio para ColorX2MA (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuarto, variaciones de seguimiento de tendencia, DeMark). |
| **Color Signal Bar** | Retraso antes de actuar sobre colores ColorX2MA. |
| **Allow Long/Short Entries/Exits** | Habilitar o deshabilitar acciones impulsadas por ColorX2MA. |

## Notas y Limitaciones

- Solo se admiten los tipos de media móvil disponibles en StockSharp. Los suavizados exóticos de la biblioteca MetaTrader (JurX, Parabolic, T3, VIDYA, AMA) no se reproducen; elegir entre SMA, EMA, SMMA, LWMA o Jurik.
- Los parámetros de fase se conservan como referencia pero no alteran los indicadores de StockSharp incorporados.
- La estrategia asume que la propiedad `Volume` está configurada; de lo contrario, las entradas no colocarán órdenes.
- Debido a que ambos módulos pueden operar de forma independiente, el flujo de órdenes resultante puede diferir de las instalaciones de MetaTrader que segregan los trades por números mágicos.
