# EMA Estrategia de cobertura de competencia cruzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto MetaTrader **EMA_CROSS_CONTEST_HEDGED** dentro de StockSharp. El robot busca un cruce alcista/bajista entre una media móvil exponencial rápida y lenta (EMA) y, opcionalmente, comprueba el histograma MACD como confirmación de tendencia. Cuando aparece una señal, la estrategia abre inmediatamente una posición de mercado y coloca una escalera de órdenes stop que cubren la operación agregando más exposición si el precio mantiene la tendencia.

## Lógica de trading
- Calcule un EMA corto y un EMA largo en la serie de velas configuradas. Las señales se pueden tomar de la barra completada anteriormente (predeterminado) o de la barra actual una vez que se cierra la vela.
- Detecte un **cruce alcista** cuando el EMA corto sube por encima del EMA largo y un **cruce bajista** cuando cae por debajo del EMA largo.
- Opcionalmente, requiera que la línea MACD esté por encima de cero para operaciones largas y por debajo de cero para operaciones cortas, replicando el filtro MQL.
- Cuando se cumpla la condición alcista, compre en el mercado, fije objetivos de limitación de pérdidas y toma de ganancias, y ponga en cola cuatro órdenes pendientes de limitación de compra espaciadas por la distancia de cobertura.
- Cuando se cumpla la condición bajista, venda en el mercado, adjunte objetivos de riesgo y ponga en cola cuatro órdenes pendientes de venta por debajo del precio.
- Las órdenes pendientes se cancelan después de su fecha de vencimiento si no se activan.
- Los stop dinámicos se estrechan a medida que crecen las ganancias abiertas y los cruces opuestos pueden forzar salidas anticipadas cuando `Use Close` está habilitado.

## Parámetros
- **Tipo de vela**: período de tiempo utilizado para todos los cálculos.
- **Volumen de orden**: volumen de operaciones para la posición inicial y cada orden de cobertura.
- **Take Profit (pips)** – distancia de obtención de beneficios en pips.
- **Stop Loss (pips)** – distancia del stop-loss en pips.
- **Trailing Stop (pips)** – distancia de trailing stop (0 desactiva el trailing).
- **Nivel de cobertura (pips)** – espacio entre las órdenes pendientes de cobertura.
- **Usar Cerrar**: cierra posiciones existentes cuando ocurre un cruce opuesto.
- **Utilice MACD**: solicite MACD confirmación para las entradas comerciales.
- **Vencimiento(s)** – vida útil de las órdenes de cobertura pendientes.
- **Corto EMA** – duración de la EMA rápida.
- **Long EMA** – duración del EMA lento (debe ser mayor que el EMA rápido).
- **Barra de señal**: elija si desea evaluar las señales en la barra actual (0) o en la barra anterior (1).

## Notas
- Todos los comentarios en el código se proporcionan en inglés según lo solicitado.
- La estructura de cobertura pendiente sigue el comportamiento del asesor experto original MQL, colocando cuatro órdenes en pasos de igual distancia.
- Las conversiones de precios de pips tienen en cuenta los `PriceStep` y `Decimals` del símbolo para coincidir con los cálculos de puntos de MetaTrader.
