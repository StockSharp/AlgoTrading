# Estrategia inversa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia inversa es un sistema de negociación de reversión a la media que combina Bollinger bandas y el índice de fuerza relativa (RSI) para identificar movimientos agotados. La estrategia busca reversiones de precios cerca de los sobres Bollinger y al mismo tiempo requiere que RSI retroceda desde una zona de sobreventa o sobrecompra. Una vez que se cumplen ambas condiciones, la estrategia entra en contra del movimiento anterior y gestiona las operaciones con paradas y objetivos fijos basados ​​en bandas.

## Lógica de trading

1. Suscríbase a la serie de velas configuradas (velas predeterminadas de 5 minutos).
2. Calcula Bollinger Bandas usando una media móvil simple con el período configurado y el multiplicador de desviación.
3. Calcule RSI utilizando el período retrospectivo configurado.
4. Realice un seguimiento de la vela terminada anterior para detectar cruces:
   - **Configuración larga**: el cierre anterior está por debajo de la banda inferior anterior y RSI está por debajo del umbral de sobreventa. El cierre actual debe volver a superar la banda inferior mientras que RSI sube por encima del nivel de sobreventa.
   - **Configuración corta**: el cierre anterior está por encima de la banda superior anterior y RSI está por encima del umbral de sobrecompra. El cierre actual debe volver a caer por debajo de la banda superior mientras que RSI cae por debajo del nivel de sobrecompra.
5. Cuando se active una configuración larga, compre en el mercado, establezca un stop de protección una desviación estándar por debajo del cierre de entrada y una toma de ganancias dos desviaciones estándar por encima de él.
6. Cuando se active una configuración corta, venda en el mercado, establezca un stop de protección una desviación estándar por encima del cierre de entrada y una toma de ganancias dos desviaciones estándar por debajo de él.
7. Gestionar posiciones abiertas:
   - Cierre las operaciones largas si el precio toca la banda superior, toca el stop o alcanza el objetivo de obtención de beneficios.
   - Cierre las operaciones cortas si el precio toca la banda inferior, llega al tope o alcanza el objetivo de obtención de beneficios.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Plazo de suscripción de la vela. | marco de tiempo de 5 minutos |
| `BollingerPeriod` | Número de barras utilizadas para la media móvil y la desviación estándar Bollinger. | 20 |
| `BollingerWidth` | Multiplicador de desviación estándar aplicado a Bollinger bandas. | 2.0 |
| `RsiPeriod` | Número de barras utilizadas para calcular el RSI. | 14 |
| `RsiOverbought` | RSI umbral que indica condiciones de sobrecompra para entradas cortas. | 70 |
| `RsiOversold` | RSI umbral que indica condiciones de sobreventa para entradas largas. | 30 |

Todos los parámetros admiten la optimización a través del StockSharp Designer o Runner. El ajuste de los niveles de sobreventa/sobrecompra cambia la agresividad de la detección de reversión, mientras que el ancho Bollinger controla hasta dónde debe extenderse el precio antes de que se consideren las señales.

## Notas de uso

- La estrategia utiliza el StockSharp API de alto nivel con suscripciones de velas automáticas y vinculación de indicadores.
- Todas las operaciones comerciales se basan en órdenes de mercado (`BuyMarket`/`SellMarket`). Los niveles de stop-loss y take-profit se manejan en código en lugar de como órdenes pendientes.
- La configuración predeterminada apunta a reversiones importantes en los gráficos intradiarios, pero se puede adaptar a períodos de tiempo más altos cambiando `CandleType`.
- Considere combinar la estrategia con filtros adicionales (tendencia, volatilidad, tiempo de sesión) cuando se ejecute en entornos en vivo.
