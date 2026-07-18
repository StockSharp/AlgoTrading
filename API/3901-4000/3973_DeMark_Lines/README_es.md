# Estrategia de líneas DeMark
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia DeMark Lines es una conversión del indicador MetaTrader "DeMark_lines" (MQL/8296). El guión original dibujó líneas de tendencia de DeMark basadas en máximos y mínimos recientes y destacó rupturas con alertas opcionales. Esta implementación StockSharp transforma la lógica de visualización en una estrategia de ruptura automatizada. Busca continuamente líneas de tendencia bajista y alcista formadas por puntos de pivote validados y abre posiciones cuando la acción del precio rompe decisivamente esas líneas.

## Lógica de trading
1. **Detección de pivote**: las velas terminadas se procesan en orden cronológico. Una vela se convierte en un máximo cuando su máximo es estrictamente más alto que las velas *PivotDepth* anteriores y no más bajo que las siguientes velas *PivotDepth*. Los mínimos oscilantes siguen la condición reflejada de los mínimos.
2. **Construcción de la línea de tendencia**: los dos máximos más recientes forman la línea de resistencia de la tendencia bajista activa. Los dos últimos mínimos forman la línea de soporte de la tendencia alcista. Los pivotes adicionales se ignoran si ocurren demasiado cerca del ancla anterior, lo que evita líneas inestables.
3. **Filtros de ruptura**: la estrategia mide el valor teórico de la línea de tendencia para el índice de barra actual. Una ruptura requiere que el precio de cierre supere la línea de resistencia (o caiga por debajo del soporte) en al menos *BreakoutBuffer* pips antes de que se ejecuten las operaciones.
4. **Colocación de órdenes**: cuando aparece una ruptura alcista, se cierra cualquier exposición corta y se abre una posición larga del volumen de estrategia configurado. La lógica de ruptura bajista refleja este comportamiento. Cada línea puede activar una nueva señal sólo después de que un nuevo pivote la redefina, evitando entradas repetidas mientras el precio ronda el nivel.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `PivotDepth` | Número de velas en cada lado necesarias para confirmar un máximo/mínimo de pivote. Controla el rigor de la detección de swing. | 2 |
| `MinBarsBetweenPivots` | Distancia mínima, en barras, entre dos pivotes del mismo tipo. Evita la superposición de anclajes y mantiene estables las líneas de tendencia. | 5 |
| `BreakoutBuffer` | Distancia adicional (en pips) agregada más allá de la línea de tendencia antes de que una ruptura se considere válida. Filtra toques ruidosos. | 2 |
| `CandleType` | Tipo de datos de vela (período de tiempo) utilizado para el análisis y la generación de señales. | velas de 30 minutos |

## Notas de conversión
- Los objetos visuales, alertas y notificaciones por correo electrónico del indicador original no se replican. En cambio, las áreas del gráfico muestran series de precios y las operaciones propias de la estrategia.
- La estrategia se basa en la suscripción de velas de alto nivel de StockSharp API y utiliza buffers internos para validar pivotes sin hacer referencia a métodos de historial de indicadores prohibidos por las pautas.
- Las operaciones de ruptura respetan la propiedad base `Volume` y revierten automáticamente la exposición existente cuando se activa la ruptura opuesta.

## Consejos de uso
- Aumente `PivotDepth` en períodos de tiempo más altos para requerir cambios más amplios, lo que reduce la frecuencia de la señal pero mejora la confiabilidad de la línea de tendencia.
- Ajuste `BreakoutBuffer` para tener en cuenta la volatilidad del instrumento. Los valores ajustados favorecen las entradas más tempranas, mientras que los buffers más grandes ayudan a evitar falsificaciones.
- Combine la estrategia con gestión de dinero externa o módulos de protección si se requiere un manejo de salida automatizado (take-profit/stop-loss), ya que el script original solo se centraba en la detección de rupturas.
