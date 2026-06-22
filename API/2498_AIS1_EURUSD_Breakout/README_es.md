# Estrategia de Rompimiento AIS1 EURUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto original AIS1 "A System: EURUSD Daily Metrics" usando la API de alto nivel de StockSharp. Opera rompimientos de EURUSD comparando la acción del precio actual con el rango del día anterior y gestiona las operaciones con dimensionamiento adaptativo de posición más un trailing stop de cuatro horas.

## Descripción general de la estrategia

- **Mercado**: Instrumentos spot/CFD/forex de EURUSD.
- **Marco temporal primario**: Las velas diarias proporcionan el máximo, mínimo y cierre de referencia.
- **Marco temporal secundario**: Las velas de 4 horas impulsan las actualizaciones del trailing stop y las verificaciones de entrada.
- **Dirección**: Se permiten operaciones largas y cortas.
- **Estilo**: Continuación de rompimiento con objetivos y stops escalados por volatilidad.

## Lógica de trading

1. Rastrear la vela diaria anterior completada. Calcular el punto medio, el rango y las distancias derivadas de stop/take usando multiplicadores configurables (`StopFactor`, `TakeFactor`).
2. Evaluar cada vela de 4 horas completada:
   - **Entrada larga**: El cierre diario anterior está por encima del punto medio y el máximo de 4 horas rompe por encima del máximo diario anterior.
   - **Entrada corta**: El cierre diario anterior está por debajo del punto medio y el mínimo de 4 horas rompe por debajo del mínimo diario anterior.
3. El tamaño de la posición se determina a partir del patrimonio actual del portafolio y la participación de riesgo configurada (`OrderReserve`). El volumen se redondea a los pasos de trading del instrumento.
4. Para las posiciones abiertas, la estrategia aplica tres capas de control de salida:
   - Stop-loss fijo en el lado opuesto del rango diario escalado por `StopFactor`.
   - Take-profit fijo a una distancia de `TakeFactor` × rango diario.
   - Trailing stop dinámico usando el rango de 4 horas anterior multiplicado por `TrailFactor`. El trailing stop se activa solo después de que la operación se mueve en ganancia.
5. Un período de espera de cinco segundos después de cualquier operación o salida refleja el comportamiento del EA original y previene modificaciones rápidas.

## Gestión del riesgo

- `OrderReserve` define la fracción del patrimonio actual que puede arriesgarse en la próxima operación. Si el tamaño calculado está por debajo del mínimo del instrumento, la operación se omite.
- `AccountReserve` rastrea el patrimonio máximo y deja de abrir o gestionar operaciones una vez que el drawdown del patrimonio supera `AccountReserve - OrderReserve` (16% con los parámetros predeterminados).
- Las salidas de trailing y los objetivos fijos aseguran que las posiciones se cierren incluso si las nuevas operaciones están bloqueadas por el guard de drawdown.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `AccountReserve` | Porción del patrimonio excluida del trading, utilizada para calcular el drawdown permitido antes de que el trading se pause. |
| `OrderReserve` | Participación del patrimonio arriesgada por operación. Determina la pérdida máxima usando la distancia del stop. |
| `TakeFactor` | Multiplicador aplicado al rango diario anterior para establecer la distancia del take-profit. |
| `StopFactor` | Multiplicador aplicado al rango diario anterior para establecer la distancia del stop-loss. |
| `TrailFactor` | Multiplicador aplicado al rango de 4 horas anterior para mover el trailing stop una vez que la posición es rentable. |
| `EntryCandleType` | Tipo de vela (diaria por defecto) usada para los niveles de rompimiento. |
| `TrailCandleType` | Tipo de vela (4 horas por defecto) usada para evaluación intradiaria y trailing. |

## Notas sobre la conversión

- La versión de StockSharp activa las entradas y actualizaciones de trailing en velas de 4 horas completadas. El asesor experto MQL original reaccionaba a cada tick; usar velas mantiene la lógica robusta dentro de la API de alto nivel.
- Stop-loss, take-profit y salidas de trailing se ejecutan con órdenes de mercado cuando los respectivos niveles de precio son tocados dentro de la vela procesada.
- Las verificaciones de margen de la versión MQL se reemplazan con dimensionamiento basado en patrimonio para permanecer neutral a la plataforma mientras se respetan las restricciones de riesgo originales.
