# Estrategia de The Predator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una traducción de alto nivel de StockSharp del experto asesor MQL **"The Predator"**. El sistema original mezcla filtros de dirección de tendencia con momentum, Bandas de Bollinger y osciladores estocásticos. Dos plantillas de entrada independientes (Estrategia 1 y Estrategia 2) están disponibles, replicando los modos seleccionables dentro de la implementación MQL.

La conversión se centra en el procesamiento basado en velas, usando suscripciones e indicadores de StockSharp. Todos los cálculos se realizan en una única serie de velas configurable.

## Indicadores principales

- **Medias móviles linealmente ponderadas (LWMA)** – estructura rápida/lenta para confirmar la tendencia a corto plazo.
- **Índice de Movimiento Direccional + Índice de Dirección Promedio (DMI/ADX)** – fuerza direccional y confirmación de tendencia.
- **Momentum (período 14 por defecto)** – mide la distancia desde el nivel neutral 100 tanto para lógica de ruptura como de retroceso.
- **Bandas de Bollinger** – dos envolventes (estrecha y ancha) para detectar contexto y la ubicación de la vela anterior, especialmente para la Estrategia 2.
- **Oscilador Estocástico** – filtro adicional para la Estrategia 2 que requiere zonas de agotamiento de momentum.
- **MACD** – confirmación de momentum de tendencia comparando la línea MACD con su señal.

## Lógica de trading

### Filtros comunes

1. Procesar solo velas completadas.
2. Requerir que los indicadores seleccionados estén formados antes de operar (`IsFormedAndOnlineAndAllowTrading`).
3. El ADX debe ser mayor que el umbral configurado.
4. El historial de desviación de Momentum se mantiene para los últimos tres valores para coincidir con las comprobaciones MQL sin llamar a `GetValue` en los indicadores.

### Estrategia 1

- **Entradas largas** cuando:
  - ADX por encima del umbral y +DI supera a −DI.
  - LWMA rápida por encima de la LWMA lenta.
  - Desviación de Momentum por encima del umbral de compra en cualquiera de los últimos tres valores.
  - Línea MACD por encima de su línea de señal.
- **Entradas cortas** reflejan lo anterior con los signos invertidos.

### Estrategia 2

- **Entradas largas** requieren adicionalmente:
  - El cierre de la vela anterior en o por encima del límite inferior de Bollinger de banda estrecha anterior.
  - Las líneas de señal y principal del estocástico ambas por encima del umbral superior.
  - Desviación de Momentum por debajo del umbral de compra en cualquiera de los últimos tres valores (buscando retrocesos dentro de tendencias).
- **Entradas cortas** requieren:
  - El cierre de la vela anterior en o por debajo del límite superior de Bollinger de banda estrecha anterior.
  - La línea de señal del estocástico por encima del umbral superior mientras la línea principal está por debajo del umbral inferior.
  - Desviación de Momentum por debajo del umbral de venta en cualquiera de los últimos tres valores.

### Manejo de posiciones

- La estrategia cancela cualquier orden activa pendiente antes de abrir una nueva operación.
- Cuando ocurre una señal de reversión, la estrategia cierra la exposición actual y abre una nueva posición en la dirección opuesta usando una orden de mercado combinada.

## Gestión de riesgo

- `StartProtection` configura:
  - Distancia inicial de stop-loss en pips.
  - Distancia inicial de take-profit en pips.
  - Trailing stop opcional que sigue un monto fijo de pips una vez activado.
- Las distancias de riesgo se convierten en unidades de precio absolutas usando el paso de precio del instrumento.
- Los módulos de punto de equilibrio basado en dinero y trailing del EA original se reemplazan con estas protecciones basadas en pips (diferencia documentada a continuación).

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Mode` | Elige Estrategia 1 (ruptura de tendencia) o Estrategia 2 (retroceso con filtros estocásticos). |
| `FastMaLength`, `SlowMaLength` | Longitudes LWMA usadas para determinar la dirección de la tendencia. |
| `DmiPeriod`, `AdxSmoothing` | Parámetros del Índice de Movimiento Direccional. |
| `MomentumPeriod` | Período de retroceso usado por el indicador de momentum. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Desviación mínima desde 100 requerida para aceptar señales. |
| `AdxThreshold` | Nivel mínimo de ADX que señala una tendencia accionable. |
| `BollingerPeriod`, `TightBandWidth`, `WideBandWidth` | Configuración de Bandas de Bollinger para los filtros de contexto. |
| `StochasticLength`, `StochasticSmooth`, `StochasticUpper`, `StochasticLower` | Parámetros para el oscilador estocástico usado en la Estrategia 2. |
| `TradeVolume` | Volumen enviado con órdenes de mercado. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Distancias de riesgo (convertidas a unidades de precio con el paso del instrumento). |
| `CandleType` | Serie de datos usada por la estrategia. |

## Diferencias respecto a la versión MQL

- Los valores de take-profit, stop-loss y trailing basados en dinero se traducen en distancias de pips manejadas por `StartProtection`.
- Los ajustes de punto de equilibrio y los mensajes de notificación por email/push no están portados (no disponibles en la API de alto nivel).
- El experto MQL llamaba a MACD y Momentum en marcos temporales superiores. En StockSharp la lógica se ejecuta solo en la serie de velas configurada; los datos multitemporal pueden añadirse a través de suscripciones adicionales si es necesario.
- La optimización de volumen de órdenes y el dimensionamiento estilo martingala no están implementados; la versión StockSharp usa un parámetro `TradeVolume` fijo.

## Uso

1. Crear un conector y portafolio como en otras muestras de StockSharp.
2. Instanciar `ThePredatorStrategy`, asignar `Security`, `Portfolio` y los parámetros deseados.
3. Iniciar la estrategia. La visualización es opcional pero disponible cuando se proporciona un área de gráfico.

La traducción mantiene el árbol de decisiones fiel al original mientras adopta las mejores prácticas de StockSharp como el enlace de indicadores y `StartProtection` para el riesgo. Ajuste los umbrales al instrumento y marco temporal elegidos.
