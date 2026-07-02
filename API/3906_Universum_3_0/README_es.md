# Estrategia original de Universum 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto original **Universum_3_0** MQL4 utilizando la API de alto nivel de StockSharp.
Combina un modelo de entrada de umbral simple DeMarker con una regla de tamaño de posición similar a una martingala que se adapta
tamaño del lote después de perder operaciones.

## Lógica de trading

- **Indicador**: oscilador DeMarker clásico con período configurable.
- **Generación de señal**:
  - Abra una posición larga cuando `DeMarker > 0.5` esté al cierre de una vela terminada.
  - Abra una posición corta cuando `DeMarker < 0.5` esté al cierre de una vela terminada.
  - Sólo puede haber una posición activa a la vez; Las nuevas señales se ignoran mientras una operación está abierta.
- **Gestión de salida**:
  - Los niveles protectores de stop-loss y take-profit se fijan utilizando compensaciones de precios absolutas medidas en puntos.
  - Las posiciones se cierran automáticamente mediante estos niveles de protección; la estrategia no cambia inmediatamente.
- **Gestión del dinero**:
  - Después de una operación rentable, el volumen se restablece al lote base.
  - Después de una operación perdedora, el volumen se multiplica por `(TakeProfitPoints + StopLossPoints) / (TakeProfitPoints - SpreadPoints)`.
  - El valor del diferencial se toma de cotizaciones activas de Nivel 1 y se convierte en "puntos" utilizando precisión de símbolo.
  - Se cuentan las pérdidas consecutivas; alcanzar el límite detiene la estrategia para emular la protección contra pérdidas original.
  - La configuración `FastOptimize = true` deshabilita la regla de tamaño adaptable y siempre usa el lote base, lo que acelera las optimizaciones.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Marco de tiempo utilizado para los cálculos de DeMarker. | marco de tiempo de 1 minuto |
| `DemarkerPeriod` | Período retrospectivo del oscilador DeMarker. | `10` |
| `TakeProfitPoints` | Distancia de obtención de beneficios expresada en puntos (convertida internamente a precio absoluto). | `50` |
| `StopLossPoints` | Distancia de stop-loss expresada en puntos. | `50` |
| `BaseVolume` | Volumen de operaciones inicial utilizado después de cada operación rentable. | `1` |
| `LossesLimit` | Número máximo de pérdidas consecutivas antes de que se detenga la estrategia. | `1,000,000` |
| `FastOptimize` | Cuando `true` desactiva el tamaño adaptable para pases de optimización rápidos. | `true` |

## Notas de implementación

- Se requieren datos de Nivel 1 para estimar el diferencial actual y replicar el multiplicador del lote original.
- La normalización del volumen respeta el volumen mínimo, el volumen máximo y el tamaño del paso del instrumento.
- Las compensaciones de stop-loss y take-profit se adaptan automáticamente a instrumentos de 3/5 dígitos ajustando el tamaño del punto.
- La visualización del gráfico traza velas, el indicador DeMarker y las operaciones ejecutadas para una validación más sencilla.

## Consejos de uso

1. Proporcione datos de oferta/demanda de Nivel 1 además de velas para garantizar que el multiplicador basado en diferenciales funcione correctamente.
2. Utilice `FastOptimize = true` durante búsquedas generales de parámetros y luego desactívelo para realizar pruebas retrospectivas precisas y operaciones en vivo.
3. Supervise el contador de pérdidas consecutivas cuando utilice multiplicadores agresivos para evitar exceder los límites del corredor.
4. Ajuste `TakeProfitPoints` y `StopLossPoints` para que coincidan con el símbolo original o su perfil de riesgo antes de operar en vivo.
