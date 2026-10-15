# Estrategia Bobnaley
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Bobnaley reproduce el MetaTrader 5 asesor experto "bobnaley" utilizando el StockSharp alto nivel API. Combina un filtro de tendencia de media móvil simple con el oscilador estocástico para buscar oportunidades de reversión. El experto original evaluó los precios de las garrapatas; el puerto utiliza velas completadas y mantiene intactas las reglas de gestión de pedidos.

## Cómo funciona
1. **Indicadores**
   - Una media móvil simple con el período configurado filtra la dirección predominante.
   - Un oscilador estocástico (líneas principal y de señal) identifica situaciones de sobreventa y sobrecompra. Sólo se necesita la línea principal para las señales; la línea de señal se calcula para que esté completa.
2. **Condiciones de entrada**
   - La estrategia espera hasta que finalice la vela actual y se formen todos los indicadores.
   - Las entradas largas requieren que el promedio móvil sea estrictamente decreciente durante las últimas tres muestras mientras el precio cierra por encima del último promedio. Al mismo tiempo, la línea principal estocástica debe estar por debajo del nivel de sobreventa y su valor anterior debe ser mayor que el anterior, reflejando el requisito EA original `stochVal[1] > stochVal[2]`.
   - Las entradas cortas son la imagen especular: la media móvil debe estar subiendo en las últimas tres muestras mientras el precio cierra por debajo de ella, y la línea principal estocástica debe estar por encima del nivel de sobrecompra mientras su valor anterior es más bajo que el anterior.
   - Las nuevas operaciones se abren solo cuando no hay ninguna posición activa actualmente, replicando la guardia `PositionSelect` de MetaTrader.
3. **Gestión de riesgos**
   - Cuando se abre una posición, la estrategia se basa en el servicio de protección de StockSharp para colocar una toma de ganancias y un límite de pérdidas en unidades de precio absoluto. Estas distancias coinciden con las entradas MetaTrader (0,007 y 0,0035 de forma predeterminada).
   - Antes de cada decisión, el valor de la cartera se compara con el parámetro `Minimum Balance`, reflejando el filtro de margen libre (`ACCOUNT_FREEMARGIN > 5000`) del código original. Si el valor de la cuenta es conocido y está por debajo del umbral, se omite la entrada.
4. **Manejo de volumen**
   - Los pedidos utilizan un parámetro fijo `Base Volume`. Esto reproduce la configuración de lote que utilizó el script MetaTrader después de aplicar su propia rutina de redondeo.

## Parámetros
| categoría | Nombre | Descripción | Predeterminado |
| --- | --- | --- | --- |
| generales | Tipo de vela | Tipo de datos de vela utilizado para los cálculos del indicador. | marco de tiempo de 5 minutos |
| Comercio | Volumen básico | Volumen de orden fijo aplicado a cada nueva posición. | 5 |
| Indicadores | Período MA | Longitud de la media móvil simple. | 76 |
| Indicadores | Stochastic Período | Mirando hacia atrás para ver la línea principal estocástica. | 5 |
| Indicadores | Stochastic %K | Longitud de suavizado para la línea %K. | 3 |
| Indicadores | Stochastic%D | Longitud de suavizado para la línea %D. | 3 |
| Indicadores | Stochastic Sobreventa | Umbral que define el territorio de sobreventa para la línea principal. | 30 |
| Indicadores | Stochastic Sobrecomprado | Umbral que define el territorio de sobrecompra para la línea principal. | 70 |
| Gestión de riesgos | Tomar ganancias | Distancia entre el precio de entrada y la toma de ganancias en unidades de precio. | 0.007 |
| Gestión de riesgos | Detener pérdidas | Distancia entre el precio de entrada y el stop-loss en unidades de precio. | 0.0035 |
| Gestión de riesgos | Saldo Mínimo | Se requiere un valor mínimo de cartera antes de poder enviar un nuevo pedido. | 5000 |

## Notas
- El experto original utilizó cotizaciones de oferta y demanda; en StockSharp el cierre de la vela se utiliza como indicador del precio de ejecución.
- No se implementan salidas finales: la operación se cierra únicamente con las órdenes de protección.
- Los cálculos de Stochastic siguen la configuración predeterminada de MetaTrader (5/3/3), pero se pueden optimizar mediante los parámetros expuestos.
