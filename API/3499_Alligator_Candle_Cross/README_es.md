# Alligator Estrategia de cruce de velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia transfiere los expertos de MetaTrader **vela de cocodrilo cruzada hacia arriba/abajo** al API de alto nivel de StockSharp. Supervisa el indicador Bill Williams Alligator creado a partir de promedios móviles suavizados del precio medio y reacciona cada vez que el cuerpo de una vela completa viaja de un lado de la boca Alligator al otro. Las entradas se pueden restringir a direcciones alcistas, bajistas o ambas a través de un parámetro, mientras que los objetivos y paradas fijos basados ​​en pips se encargan de la gestión de riesgos.

## Lógica comercial

### Preparación de indicadores
- Calcule la Alligator **Mandíbula**, **Dientes** y **Labios** usando promedios móviles suavizados con las longitudes clásicas 13/8/5.
- Aplique los tradicionales desplazamientos hacia adelante (8/5/3 barras por defecto) para que cada línea se compare con la vela que se forma frente a ella.
- Todos los precios se toman como muestra de la mediana de velas `(High + Low) / 2` para que coincidan con la implementación de MetaTrader.

### Configuración larga ("vela cruzada hacia arriba")
1. La vela terminada anterior debe cerrar en o por debajo de la línea Alligator más baja (después de aplicar el cambio).
2. El cuerpo de la vela actual se abre en o por debajo del valor Alligator desplazado más alto y cierra por encima de ese mismo valor, lo que demuestra que el cuerpo cruzó la boca Alligator en dirección ascendente.
3. Actualmente no hay ninguna posición abierta y se permite la negociación.
4. Cuando todas las condiciones se alinean, la estrategia envía una **Compra** de mercado para el volumen configurado.

### Configuración corta ("vela cruzada hacia abajo")
1. El cierre anterior debe estar en o por encima de la línea Alligator desplazada más alta.
2. El cuerpo de la vela actual se abre en o por encima del valor más bajo desplazado Alligator y termina por debajo de él, confirmando un cruce bajista a través del Alligator.
3. No hay ninguna posición abierta y el comercio está habilitado.
4. Se envía una orden de mercado **Venta** para el volumen configurado.

### Gestión de posiciones
- Cuando se abre una nueva posición, la estrategia convierte las distancias de stop-loss y take-profit de pips en precios absolutos utilizando el paso del precio del símbolo.
- Las posiciones largas salen cuando la vela toca el stop-loss, alcanza el objetivo o se cierra por debajo del mínimo de las líneas desplazadas de Dientes y Labios.
- Las posiciones cortas salen en el stop-loss, el objetivo o un cierre por encima del máximo de los valores desplazados de Dientes y Labios.
- La llamada incorporada **StartProtection** se activa al inicio para garantizar que los llenados anormales se cierren de forma segura.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.1` | Tamaño del comercio en lotes o contratos. |
| `StopLossPips` | `int` | `50` | Distancia desde el precio de entrada hasta el stop de protección en pips. Cero desactiva la parada. |
| `TakeProfitPips` | `int` | `50` | Distancia desde la entrada hasta el objetivo de beneficio fijo en pips. Cero desactiva el objetivo. |
| `JawPeriod` | `int` | `13` | Longitud promedio móvil suavizada para la línea de la mandíbula (azul) Alligator. |
| `JawShift` | `int` | `8` | Desplazamiento hacia adelante aplicado a la línea de la mandíbula antes de evaluar las señales. |
| `TeethPeriod` | `int` | `8` | Longitud promedio móvil suavizada para la línea de dientes Alligator (roja). |
| `TeethShift` | `int` | `5` | Desplazamiento hacia adelante de la línea de dientes. |
| `LipsPeriod` | `int` | `5` | Longitud promedio móvil suavizada para la línea de labios (verde) Alligator. |
| `LipsShift` | `int` | `3` | Desplazamiento hacia adelante de la línea de los labios. |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Serie de velas utilizadas para los cálculos. |
| `EntryMode` | `AlligatorCrossMode` | `Both` | Elige si la estrategia opera con configuraciones largas, cortas o ambas. |

## Notas de uso
- Funciona en cualquier instrumento compatible con StockSharp; asegúrese de que `CandleType` coincida con el período de tiempo utilizado en la plantilla MetaTrader original.
- Los pips se deducen del paso del precio del instrumento: para cotizaciones de 3 o 5 decimales (por ejemplo, EURUSD), el pip equivale a diez pasos del precio.
- La lógica actúa solo en velas completadas y no se basa en datos de ticks, lo que la mantiene alineada con MetaTrader backtests.
