# Estrategia de Búsqueda de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Trend Finder es una estrategia de seguimiento de tendencia multitemporal convertida del asesor experto original **TREND FINDER.mq4**. La lógica ahora utiliza la API de alto nivel de StockSharp y mantiene la idea central de combinar medias móviles ponderadas linealmente con confirmaciones de momentum de marcos temporales superiores y filtros MACD. La estrategia se centra en detectar rupturas que siguen a máximos o mínimos sostenidos, con el objetivo de entrar en la dirección de la ruptura una vez que se confirman el momentum y la alineación de la tendencia a largo plazo.

## Datos de mercado e indicadores
- **Marco temporal base (`CandleType`)** – velas primarias utilizadas para el reconocimiento de patrones y la ejecución de órdenes. Las medias móviles ponderadas linealmente se calculan sobre el precio típico de estas velas.
- **Marco temporal de momentum (`MomentumCandleType`)** – velas de marco temporal superior utilizadas para evaluar desviaciones del momentum respecto al valor neutro de 100. Las tres lecturas de momentum más recientes deben superar umbrales configurables antes de permitir una operación.
- **Marco temporal MACD (`MacdCandleType`)** – velas a largo plazo procesadas a través de un MACD con longitudes de rápido, lento y señal personalizables. Se requiere una condición MACD alcista (bajista) para configuraciones largas (cortas).

## Lógica de entrada
1. **Detección de ruptura de tendencia** – la estrategia escanea hasta las últimas 100 velas históricas (excluyendo las tres más recientes) para encontrar el máximo más alto o el mínimo más bajo. Una configuración alcista requiere que la barra actual abra por encima de un clúster previo de máximos mientras al menos uno de los tres máximos anteriores permanezca por debajo de ese nivel histórico. Una configuración bajista refleja la lógica para mínimos.
2. **Alineación de medias móviles** – la LWMA rápida debe estar por encima de la LWMA lenta para largos y por debajo para cortos.
3. **Estructura reciente de velas** – para largos, el mínimo de hace dos barras debe estar por debajo del máximo de la barra anterior (`Low[2] < High[1]`), mientras que los cortos requieren que el último mínimo esté por debajo del máximo de hace dos barras (`Low[1] < High[2]`). Esto preserva la verificación de estructura de precio original.
4. **Confirmación de momentum** – al menos una de las últimas tres desviaciones de momentum (calculadas como |Momentum – 100|) en el marco temporal superior debe superar los umbrales de compra/venta configurados.
5. **Confirmación MACD** – el último valor MACD en el marco temporal a largo plazo debe estar por encima de su señal para largos y por debajo para cortos.
6. **Filtrado por posición** – las nuevas órdenes largas se emiten solo cuando la posición actual es no positiva, y las nuevas órdenes cortas solo cuando es no negativa. El volumen de la orden es igual a `Volume + |Position|` para admitir reversiones rápidas de posición.

## Salida y gestión de riesgo
- **Stop-loss (`StopLoss`)** – distancia fija por debajo (encima) del precio de entrada para posiciones largas (cortas).
- **Take-profit (`TakeProfit`)** – objetivo de beneficio fijo; cuando se alcanza, la posición se cierra inmediatamente.
- **Stop dinámico (`TrailingStop`)** – sigue el precio más alto alcanzado después de entrar en un largo o el precio más bajo para cortos. El stop se ajusta en cada vela completada.
- **Punto de equilibrio (`BreakEvenTrigger`, `BreakEvenOffset`)** – una vez que el precio se mueve a favor de la operación por la distancia de activación, el stop de protección se mueve al precio de entrada más (menos) el offset para largos (cortos), asegurando que los beneficios queden bloqueados si el precio retrocede.
- **Cierre automático** – los métodos auxiliares cierran todo el tamaño de la posición y luego reinician todas las variables de seguimiento. No hay salidas parciales en esta implementación.

## Parámetros
| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| `CandleType` | Marco temporal base para el reconocimiento de patrones y la ejecución de órdenes. | Velas de 15 minutos |
| `MomentumCandleType` | Marco temporal superior utilizado para calcular la confirmación de momentum. | Velas de 1 hora |
| `MacdCandleType` | Marco temporal para la confirmación MACD (por defecto ~30 días). | Velas de 30 días |
| `FastMaLength` | Longitud de la media móvil ponderada linealmente rápida. | 6 |
| `SlowMaLength` | Longitud de la media móvil ponderada linealmente lenta. | 85 |
| `MomentumPeriod` | Número de barras de mayor marco temporal usadas para el ratio de momentum. | 14 |
| `MomentumThresholdBuy` | Mínimo |Momentum − 100| requerido para permitir entradas largas. | 0.3 |
| `MomentumThresholdSell` | Mínimo |Momentum − 100| requerido para permitir entradas cortas. | 0.3 |
| `MacdShortLength` | Longitud del EMA rápido dentro del cálculo MACD. | 12 |
| `MacdLongLength` | Longitud del EMA lento dentro del cálculo MACD. | 26 |
| `MacdSignalLength` | Longitud del EMA de señal para MACD. | 9 |
| `StopLoss` | Distancia absoluta de stop-loss en unidades de precio del instrumento. | 0.0020 |
| `TakeProfit` | Distancia absoluta de take-profit en unidades de precio del instrumento. | 0.0050 |
| `TrailingStop` | Distancia del stop dinámico que sigue los movimientos favorables. | 0.0040 |
| `BreakEvenTrigger` | Distancia de beneficio que activa el stop de punto de equilibrio. | 0.0030 |
| `BreakEvenOffset` | Offset adicional aplicado una vez que el punto de equilibrio está activo. | 0.0010 |

> **Nota:** Establezca la propiedad `Strategy.Volume` al tamaño de orden deseado antes de iniciar la estrategia. Los parámetros anteriores se expresan en unidades de precio absolutas; ajústelos según el tamaño del tick del instrumento negociado.

## Directrices de uso
1. Asigne la estrategia al `Security` deseado y configure las propiedades `Portfolio` y `Volume`.
2. Asegúrese de que la fuente de datos seleccionada pueda entregar los tres marcos temporales de velas solicitados; de lo contrario, los filtros de confirmación nunca estarán listos.
3. Ajuste los parámetros de riesgo para que coincidan con la volatilidad del instrumento. Dado que los valores por defecto se expresan como distancias de precio absolutas, pueden requerir reescalado para acciones, futuros o criptomonedas.
4. Opcionalmente, adjunte el área de gráfico generada para visualizar el precio, las operaciones y ambas medias móviles.
5. Monitoree los logs para las confirmaciones de órdenes. La estrategia utiliza órdenes de mercado (`BuyMarket`, `SellMarket`) para entradas y salidas.

## Diferencias con el asesor experto original
- Los stops basados en capital, la lógica de take-profit basada en saldo y las notificaciones push/email presentes en el script MQL fueron omitidos intencionalmente para mantener la estrategia enfocada en las reglas de trading centrales y para alinearse con la API de alto nivel de StockSharp.
- La gestión del volumen está simplificada: la versión de StockSharp abre como máximo una posición neta a la vez y usa el `Volume` configurado para dimensionar las operaciones.
- Los parámetros de gestión monetaria expresados en divisa de la cuenta no se replican; en su lugar, se proporcionan controles de riesgo basados en precio (`StopLoss`, `TakeProfit`, `TrailingStop`, punto de equilibrio).

## Mejoras recomendadas
- Agregar controles de riesgo a nivel de portafolio si se operan múltiples símbolos simultáneamente.
- Combinar con filtros de sesión o de volatilidad para desactivar el trading durante períodos ilíquidos.
- Considere enviar las ejecuciones a análisis externos (por ejemplo, para seguimiento de capital) si dicha funcionalidad es necesaria.
