# Estrategia AOCCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia AOCCI es una conversión directa del asesor experto MetaTrader 4 "AOCCI". Combina filtros de impulso y reversión media mediante el uso del Awesome Oscillator (AO) y el Commodity Channel Index (CCI) junto con un pivote de piso diario. La versión convertida funciona en el nivel alto de StockSharp API y mantiene la misma lógica protectora que el script original.

## Lógica
1. **Preparación de datos**
   - Utiliza velas intradiarias (por defecto 1 hora) para generar señales.
   - Utiliza velas diarias para calcular el pivote del día anterior completado (máximo + mínimo + cierre dividido por 3).
   - Realiza un seguimiento de los últimos seis precios de apertura intradía para detectar grandes brechas.
2. **Filtro de espacios**
   - Cualquier diferencia de un solo paso que exceda el umbral del *Big Jump Filter* cancela la señal actual.
   - Cualquier diferencia combinada de dos pasos que exceda el umbral del *Filtro de doble salto* también cancela la señal.
3. **Comprobaciones de indicadores**
   - AO debe ser mayor que cero y CCI debe ser no negativo en la barra actual.
   - Al menos uno de los siguientes debe ser cierto en la barra anterior: AO por debajo de cero, CCI en cero o por debajo, o precio por debajo del pivote.
4. **Filtro direccional**
   - El precio de cierre debe permanecer por encima del nivel de pivote.
5. **Pedidos**
   - El asesor experto original solo abre operaciones largas porque la condición corta duplica la lógica larga. La conversión mantiene este comportamiento.
   - Las órdenes de mercado utilizan el *Volumen de órdenes* configurado.
6. **Protección**
   - El stop-loss inicial y la toma de ganancias se expresan en incrementos de precios.
   - El trailing stop opcional refuerza el stop una vez que el precio se mueve a favor de la posición al menos en la distancia de seguimiento.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CciPeriod` | Período para el índice del canal de productos básicos (por defecto 55). |
| `SignalCandleOffset` | Se aplica compensación adicional al hacer referencia a velas diarias históricas (predeterminado 0). |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio. |
| `TakeProfitPoints` | Distancia de obtención de beneficios en pasos de precio. |
| `TrailingStopPoints` | Distancia del trailing stop en pasos de precio (0 desactiva el trailing). |
| `BigJumpPoints` | Brecha de apertura máxima permitida de una sola barra expresada en incrementos de precio. |
| `DoubleJumpPoints` | Brecha combinada máxima permitida de dos barras expresada en incrementos de precios. |
| `OrderVolume` | Volumen utilizado al enviar órdenes de mercado. |
| `CandleType` | Tipo de vela intradiaria (barras de una hora predeterminadas). |
| `DailyCandleType` | Tipo de vela diaria utilizada para el cálculo del pivote. |

## Notas de uso
- La estrategia requiere suscripciones de datos tanto intradía como diarias.
- El paso de precio (tamaño de tick) del valor seleccionado se utiliza para traducir los parámetros de riesgo basados en puntos en precios reales.
- La gestión de trailing stop se aplica a las velas completadas, reflejando el comportamiento del EA original.
- Debido a que la versión original MQL4 nunca activa operaciones cortas, la conversión mantiene intencionalmente el mismo conjunto de reglas.
