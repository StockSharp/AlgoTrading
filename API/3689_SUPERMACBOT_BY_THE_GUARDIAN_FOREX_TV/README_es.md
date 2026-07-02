# SUPERMACBOT de The Guardian Estrategia de TV Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El **SUPERMACBOT de The Guardian Forex TV Strategy** replica el concepto del asesor experto MetaTrader original al combinar el oscilador MACD con un filtro de tendencia dual de promedio móvil simple y un filtro de salida de promedio móvil. La implementación convertida StockSharp funciona en velas completadas y envía órdenes de mercado siempre que se forma una confluencia alcista o bajista. La estrategia evita el comercio tick-by-tick y sigue las pautas API de alto nivel al confiar en suscripciones de velas y vinculaciones de indicadores.

El motor comercial evalúa el impulso a través del histograma MACD y la alineación de tendencias entre dos promedios móviles simples. Una media móvil móvil actúa como referencia de gestión comercial y como filtro de confirmación retrasada, reflejando el módulo de seguimiento configurado en el experto MQL. La versión StockSharp se centra en la claridad y la portabilidad entre instrumentos y plazos al exponer cada valor clave como un parámetro configurable.

## Lógica de trading
1. **Fuente de datos**: la estrategia se suscribe a un tipo de vela configurable (período de tiempo). Cada vela completa desencadena el flujo de decisión.
2. **Preparación del indicador**: MACD (con períodos rápido, lento y de señal ajustables) y se recalculan dos SMA en cada vela. Un SMA adicional replica el filtro final del experto MQL.
3. **Reglas de entrada**
   - **Entrada larga**
     - El histograma MACD supera el umbral configurable.
     - El rápido SMA está por encima del lento SMA, mostrando una tendencia alcista establecida.
     - El precio de cierre se mantiene por encima del SMA final para garantizar la solidez del precio.
     - La estrategia no tiene ninguna posición larga existente (solo se mantiene una posición neta).
   - **Entrada corta**
     - El histograma MACD cruza por debajo del umbral negativo.
     - El SMA rápido está por debajo del SMA lento, lo que indica un entorno bajista.
     - El precio de cierre se mantiene por debajo del SMA final.
     - La estrategia no tiene exposición corta.
4. **Reglas de salida**
   - Las posiciones largas se cierran cuando ocurre cualquiera de las siguientes situaciones: el histograma se vuelve negativo, el SMA rápido cae por debajo del SMA lento o el precio cierra por debajo del SMA.
   - Las posiciones cortas se cierran cuando el histograma se vuelve positivo, el SMA rápido sube por encima del SMA lento o el precio cierra por encima del SMA final.
5. **Manejo de riesgos**: el algoritmo comercializa una única posición neta y nunca pirámides. Se pueden agregar paradas de protección externamente utilizando StockSharp reglas de riesgo si se desea.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Serie de velas procesadas por la estrategia. | plazo de 1 minuto |
| `FastMaPeriod` | Periodo del filtro de media móvil rápida simple. | 12 |
| `SlowMaPeriod` | Periodo del filtro de media móvil simple lenta. | 26 |
| `MacdFastPeriod` | Período EMA rápida para el indicador MACD. | 12 |
| `MacdSlowPeriod` | Período EMA lenta para el indicador MACD. | 24 |
| `MacdSignalPeriod` | Periodo de señal EMA para el indicador MACD. | 9 |
| `HistogramThreshold` | Valor absoluto mínimo requerido del histograma MACD antes de abrir una posición. | 0.0 |
| `TrailingPeriod` | Período de la media móvil simple final utilizado para confirmaciones y salidas. | 12 |

Todos los parámetros están expuestos a través de `StrategyParam<T>` y se pueden optimizar dentro de StockSharp Designer.

## Notas de uso
- Adjunte la estrategia a cualquier seguridad y plazo que se adapte a su entorno de pruebas.
- Asegúrese de que haya suficiente búfer de historial disponible para que todos los indicadores estén completamente formados antes de que comience la negociación.
- Debido a que la estrategia funciona con velas terminadas y posiciones netas, es seguro operar en carteras de múltiples instrumentos sin órdenes conflictivas.
- Se puede agregar administración de dinero adicional (tamaño de lote, stop loss, salidas parciales) componiendo la estrategia con otros módulos StockSharp.

## Diferencias con el experto original
- La conversión StockSharp se centra en la lógica de cierre de velas en lugar del motor controlado por eventos del Asesor Experto MetaTrader. Esto mantiene el comportamiento determinista en las pruebas retrospectivas y en el comercio real.
- El tamaño de lote y las órdenes de trailing stop del Asesor Experto original se reemplazan por una salida simplificada basada en posiciones condicionada por el promedio móvil.
- Los umbrales de señal se manejan a través del parámetro de umbral de histograma MACD, lo que permite a los usuarios imitar el sistema de puntuación del experto MQL ajustando el valor.

## Descargo de responsabilidad
Los algoritmos comerciales implican riesgos financieros. Pruebe minuciosamente la estrategia antes de implementarla con capital real.
