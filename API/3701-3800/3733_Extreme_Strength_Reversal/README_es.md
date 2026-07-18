# Estrategia de reversión de fuerza extrema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Sistema de contratendencia convertido del asesor experto MetaTrader EXSR.
- Combina Bollinger Bandas y RSI extremos para localizar movimientos de agotamiento.
- Utiliza un tamaño de posición basado en porcentaje con stop-loss fijo y take-profit en pips.

## Lógica de trading
1. Suscríbase a la serie de velas configuradas (el valor predeterminado es velas de 1 hora).
2. Calcula una envolvente de bandas Bollinger (período, desviación) y un oscilador RSI.
3. Cuando se cierra una vela:
   - Una configuración larga requiere: RSI por debajo del nivel de sobreventa pero por encima de cero, el mínimo de la vela por debajo de la banda inferior y un cuerpo alcista (cierre por encima de apertura).
   - Una configuración corta requiere: RSI por encima del nivel de sobrecompra, la vela muy por encima de la banda superior y un cuerpo bajista (cierre debajo de apertura).
4. Sólo podrá haber una posición abierta a la vez. La exposición opuesta se cierra antes de invertir.
5. Las paradas y los objetivos se proyectan a partir del precio de ejecución utilizando pips estilo MetaTrader. El motor monitorea las velas posteriores y sale cuando se toca cualquiera de los niveles.

## Gestión monetaria
- El tamaño del pedido tiene como valor predeterminado la propiedad `Volume` de la estrategia. Cuando es cero, la estrategia deriva el volumen de `RiskPercent` y la distancia de parada.
- El riesgo se calcula a partir del capital de la cartera actual (retroceso al valor de equilibrio/inicial). La distancia de parada se traduce en precio o unidades monetarias utilizando el paso y el precio de paso del instrumento.
- El volumen se normaliza según el paso de volumen del instrumento y las restricciones mínimas y máximas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| Porcentaje de riesgo | Porcentaje de capital arriesgado por operación. | 1% |
| Stop Loss (pips) | Distancia de parada en MetaTrader pips. | 150 |
| Tomar ganancias (pips) | Distancia de toma de ganancias en pips. | 300 |
| Bollinger Período | Velas utilizadas para Bollinger Bandas. | 20 |
| Bollinger Desviación | Multiplicador de desviación estándar. | 2.0 |
| RSI Período | Velas utilizadas para RSI. | 14 |
| RSI Sobrecomprado | El nivel RSI se considera extremadamente sobrecomprado. | 80 |
| RSI Sobreventa | El nivel RSI se considera extremadamente sobrevendido. | 20 |
| Tipo de vela | Plazo de velas para el análisis. | 1 hora |

## Notas
- Asegúrese de que el símbolo seleccionado exponga el paso de precio, el precio de paso y el paso de volumen para un tamaño preciso. La estrategia recurre a valores predeterminados razonables cuando no está disponible.
- La gestión de riesgos se activa incluso cuando el comercio está temporalmente desactivado, por lo que las salidas protectoras permanecen activas.
- La lógica procesa solo velas completadas, reflejando el EA original que funciona en la barra anterior.
