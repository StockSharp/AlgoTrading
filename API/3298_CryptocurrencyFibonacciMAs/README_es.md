# Estrategia Cryptocurrency Fibonacci MAs (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia adapta el asesor experto de MetaTrader "Cryptocurrency Fibonacci MAs" a la API de alto nivel de StockSharp. El sistema rastrea una pila de medias móviles exponenciales basadas en Fibonacci (8/13/21/55), valida momentum en un marco temporal superior y confirma la tendencia macro con un filtro MACD mensual antes de enviar órdenes de mercado. Solo se procesan velas completadas y todas las actualizaciones de indicadores se realizan mediante la canalización `Bind`/`BindEx`.

Comparada con la versión MetaTrader, se hicieron los siguientes ajustes intencionales:
- Se omitieron take profit basado en dinero, equity stop-out, trailing vela a vela y automatización de break-even. La adaptación StockSharp usa stop-loss y take-profit clásicos basados en pips mediante `StartProtection`.
- La piramidación de órdenes se limita a una posición neta por dirección. Las inversiones cierran primero la exposición opuesta, reflejando el modelo de posición neteada de StockSharp.
- Los datos multitemporales se proporcionan mediante suscripciones adicionales de velas en lugar de solicitudes ad hoc de indicadores bajo demanda.

## Lógica de trading
### Entrada larga
1. Alineación EMA: 8 > 13 > 21 > 55 en el marco temporal principal.
2. Momentum de marco temporal superior: la desviación absoluta del Momentum de 14 períodos respecto al nivel neutral 100 está por encima del umbral de compra configurado en al menos una de las tres últimas velas del marco temporal superior.
3. Filtro MACD mensual: la línea principal MACD está por encima de la línea de señal.
4. Filtro de posición: la posición neta actual debe estar plana o corta y permanecer por debajo del volumen máximo configurado.

### Entrada corta
1. Alineación EMA: 8 < 13 < 21 < 55.
2. Desviación de momentum por encima del umbral de venta en al menos una de las tres últimas velas del marco temporal superior.
3. Línea principal MACD por debajo de su línea de señal.
4. La exposición neta debe estar plana o larga y dentro del límite `MaxPositions`.

### Lógica de salida
- `StartProtection` coloca órdenes de stop-loss y take-profit de protección expresadas en distancias pip. No se aplica lógica adicional de trailing ni break-even en esta adaptación.
- Las señales de inversión envían el tamaño de orden de mercado opuesto, que primero compensa la posición existente antes de establecer la nueva exposición.

## Mapeo multitemporal
El marco temporal superior usado para el indicador de momentum refleja la tabla de coeficientes original:

| Marco temporal principal | Marco temporal de momentum |
| --- | --- |
| 1 minuto | 15 minutos |
| 5 minutos | 30 minutos |
| 15 minutos | 1 hora |
| 30 minutos | 4 horas |
| 1 hora | 1 día |
| 4 horas | 1 semana |
| 1 día | 1 mes |
| 1 semana | 1 mes |
| 1 mes | 1 mes |

La confirmación MACD siempre se ejecuta en una aproximación mensual de 30 días.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño de orden en lotes. | 0.1 |
| `StopLossPips` | Distancia de stop-loss en pips. | 20 |
| `TakeProfitPips` | Distancia de take-profit en pips. | 50 |
| `MomentumBuyThreshold` | Desviación absoluta mínima de momentum desde 100 requerida para operaciones largas. | 0.3 |
| `MomentumSellThreshold` | Desviación absoluta mínima de momentum desde 100 requerida para operaciones cortas. | 0.3 |
| `MaxPositions` | Volumen neto máximo por dirección expresado como múltiplos de `TradeVolume`. | 1 |
| `CandleType` | Marco temporal principal para cálculos EMA. | Velas de 1 hora |

## Notas de uso
1. Adjunte la estrategia a un símbolo y seleccione un marco temporal apropiado mediante `CandleType`.
2. Asegúrese de que la fuente de datos pueda proporcionar tanto el marco temporal principal como los marcos temporales superiores derivados (momentum y mensual).
3. Ajuste los parámetros de riesgo basados en pips para que coincidan con el tamaño de tick del instrumento. El helper convierte pips a pasos del instrumento usando `Security.PriceStep`.
4. El backtesting y la optimización pueden ajustar los umbrales de momentum y las distancias de stop usando los rangos de parámetros proporcionados.
