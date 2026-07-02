# Media móvil con fotogramas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto MetaTrader 5 **"Media Móvil con Marcos"**. El sistema original evalúa la relación entre los precios de apertura/cierre de cada vela y un promedio móvil simple desplazado (SMA) mientras muestra múltiples "marcos" de optimización en los gráficos. Este puerto StockSharp se centra en la lógica comercial: reacciona solo una vez por barra completada, abre una única posición de compensación y refleja las reglas de administración de dinero del código fuente.

## Lógica de trading

- **Fuente de datos**: la estrategia se suscribe al período de tiempo configurado (`CandleType`) y procesa solo velas terminadas, lo que reproduce la restricción MetaTrader `if(rt[1].tick_volume>1) return;`.
- **Indicador**: una media móvil simple con período `MovingPeriod`. La salida del indicador avanza `MovingShift` velas completadas manteniendo un búfer de valores pasados.
- **Calentamiento**: el comercio se suspende hasta que se recolecten al menos 100 velas completas, que coincidan con la guardia `Bars(_Symbol,_Period)>100` original.
- **Condiciones de entrada**
  - Vaya **largo** cuando la vela se abra por debajo del SMA desplazado y cierre por encima de él.
  - Vaya **corto** cuando la vela se abra por encima del SMA desplazado y cierre por debajo de él.
  - El motor impone una única posición: la exposición opuesta se aplana antes de entrar en la nueva dirección.
- **Condiciones de salida**: una posición larga existente se cierra cuando el precio de apertura está por encima y el precio de cierre está por debajo del SMA desplazado; Los pantalones cortos se cierran en el cruce opuesto. Las nuevas operaciones no se abren en la misma barra después de una salida, como el experto original.

## Tamaño de la posición y riesgo

- **Riesgo Máximo**: determina el volumen de pedido bruto como `Portfolio.CurrentValue * MaximumRisk / price` cuando los datos de la cartera están disponibles. Si el feed del corredor no proporciona información sobre el capital, la estrategia recurre a la propiedad manual `Volume`.
- **DecreaseFactor**: después de más de una operación perdedora consecutiva, el tamaño de la siguiente posición se reduce en `volume * losses / DecreaseFactor`, imitando la lógica de reducción de lotes de MetaTrader. Cualquier operación rentable reinicia el contador.
- **Alineación de volumen**: el tamaño calculado se normaliza al `VolumeStep` del instrumento, se fija entre `MinVolume` y `MaxVolume` y se redondea a dos decimales cuando el intercambio no publica un paso.

## Notas adicionales

- La visualización de "marcos" MetaTrader no se transfiere porque StockSharp ya proporciona paneles de optimización enriquecidos. La lógica comercial, el momento de la señal y el comportamiento del tamaño siguen siendo fieles a la fuente.
- Todos los valores del indicador se consumen directamente desde la devolución de llamada `Bind`; no se utilizan llamadas `GetValue` manuales.
- El seguimiento de pérdidas consecutivas se implementa dentro de `OnOwnTradeReceived`, lo que permite que la estrategia reaccione correctamente a los llenados parciales y al comportamiento de compensación.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `MaximumRisk` | `0.02` | Fracción del patrimonio de la cartera arriesgada en cada entrada. |
| `DecreaseFactor` | `3` | Divisor utilizado para reducir el tamaño de la posición después de dos o más pérdidas consecutivas. |
| `MovingPeriod` | `12` | Longitud de la media móvil simple aplicada a los precios de cierre. |
| `MovingShift` | `6` | Número de velas completadas utilizadas para compensar el avance SMA en el tiempo. |
| `CandleType` | `1h time frame` | Serie de velas primarias procesadas por la estrategia. |

## Consejos de uso

1. Adjunte la estrategia a un valor y una cartera en StockSharp Designer o código.
2. Ajuste el tipo de vela para que coincida con el período del gráfico MetaTrader deseado.
3. Ajuste `MaximumRisk` y `DecreaseFactor` para que coincidan con el tamaño de su cuenta y la tolerancia al riesgo deseada.
4. Ejecute pruebas retrospectivas para validar que las señales cruzadas se alineen con los resultados originales de MetaTrader.
