# Robot comercial AIS2 20005 (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

AIS2 Trading Robot 20005 es un asesor experto en ruptura intradiaria escrito originalmente para MetaTrader 4. El puerto recrea su lógica de marcos de tiempo múltiples además de la estrategia de alto nivel de StockSharp API. La estrategia espera rupturas de impulso por encima o por debajo del punto medio de la vela de marco temporal superior anterior, aplica distancias dinámicas de toma de ganancias y stop-loss derivadas del rango de esa vela y administra posiciones con un marco temporal secundario más rápido que impulsa un stop dinámico.

La conversión se centra en la transparencia y el control manual: las posiciones se abren con órdenes de mercado, los niveles de protección se aplican dentro de la propia estrategia y una pausa comercial configurable evita reingresos rápidos. El tamaño de las posiciones basado en acciones refleja la lógica de "reserva" original, permitiendo a los usuarios asignar una fracción del valor de la cartera a cada operación manteniendo intacto un colchón de capital.

## Lógica principal

1. **Análisis del marco de tiempo principal** – En cada vela finalizada del marco de tiempo principal (predeterminado 15 minutos), la estrategia calcula:
   - Punto medio de la vela `(High + Low) / 2`.
   - Distancias de obtención de beneficios y stop-loss basadas en rangos (`range * TakeFactor` y `range * StopFactor`).
   - Aproximación del diferencial actual, buffers de parada/congelación y un paso de seguimiento mínimo.
2. **Condiciones de ruptura**: las entradas largas requieren tanto un cierre por encima del punto medio como que el precio de venta actual rompa el máximo anterior más el diferencial. Los pantalones cortos reflejan la condición de los mínimos. Las órdenes se bloquean si las distancias de parada/objetivo calculadas no cumplen con las restricciones a nivel de corredor.
3. **Gestión de riesgos**: el tamaño de la posición se deriva del capital de la cartera: `OrderReserve` define la fracción negociable, mientras que `AccountReserve` mantiene una parte intacta. Si el capital disponible o los límites del corredor no permiten la operación, se omite la configuración.
4. **Gestión comercial**: el período de tiempo más rápido (predeterminado 1 minuto) actualiza continuamente la distancia de seguimiento. A medida que el precio avanza, el stop migra a favor de la operación una vez que el rango secundario lo justifica. Alcanzar el objetivo o el stop da como resultado una salida inmediata del mercado.
5. **Barreras operativas**: un temporizador de enfriamiento (`TradingPauseSeconds`) replica la pausa comercial MQL original. La estrategia también se suscribe al libro de órdenes para capturar valores de oferta/demanda en vivo; cuando no está disponible, vuelve a cerrar la vela.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `PrimaryCandleType` | Se utiliza un marco de tiempo más alto para generar señales de entrada. | velas de 15 minutos |
| `SecondaryCandleType` | Plazo más bajo para los cálculos del trailing stop. | velas de 1 minuto |
| `TakeFactor` | Multiplicador aplicado al rango de velas principal para construir una distancia de toma de ganancias. | 1.7 |
| `StopFactor` | Multiplicador aplicado al rango de vela principal para construir una distancia de stop-loss. | 1.7 |
| `TrailFactor` | Multiplicador aplicado al rango de velas secundarias para actualizaciones finales. | 0,5 |
| `AccountReserve` | Fracción del capital mantenido en reserva (no utilizado para negociar). | 0,20 |
| `OrderReserve` | Fracción del capital total asignado por operación antes de los colchones. | 0,04 |
| `BaseVolume` | Volumen de operaciones de respaldo cuando no se puede calcular el tamaño del riesgo. | 1 lote |
| `StopBufferTicks` | Se agregaron ticks adicionales a las verificaciones de cumplimiento a nivel de parada del corredor. | 0 |
| `FreezeBufferTicks` | Marcas adicionales que evitan que las actualizaciones frecuentes se detengan cerca de los niveles de congelación. | 0 |
| `TrailStepMultiplier` | Multiplicador aplicado al diferencial al validar los pasos finales. | 1 |
| `TradingPauseSeconds` | Enfriamiento entre operaciones consecutivas. | 5 segundos |

Todos los parámetros numéricos exponen `SetCanOptimize()` (cuando sea significativo) para que puedan participar en StockSharp escenarios de optimización.

## Notas de uso

- Adjunte la estrategia a un valor y asegúrese de que los datos del libro de pedidos/Nivel 1 estén disponibles para una detección precisa del diferencial. Sin cotizaciones activas, la lógica aún se ejecuta mediante cierres de velas, pero las validaciones de parada se vuelven conservadoras.
- Establece `PrimaryCandleType`/`SecondaryCandleType` en los períodos de tiempo que existen en tu feed de datos. El puerto utiliza `SubscribeCandles` y vincula los controladores a través del nivel alto de StockSharp API.
- El trailing stop es virtual (gestionado internamente); no se envían órdenes de parada al corredor. Si necesita paradas del lado del servidor, extienda el código para registrar órdenes de protección después de las entradas.
- `StartProtection()` se llama al arrancar para que el motor liquide posiciones inesperadas si es necesario.

## Diferencias con el original EA

- La versión MetaTrader manipuló variables globales en todo el terminal; este puerto mantiene los parámetros dentro de la estrategia y los expone a través de contenedores `StrategyParam`.
- Las modificaciones de órdenes fueron reemplazadas por salidas directas del mercado porque StockSharp maneja la lógica de parada/objetivo dentro del propio algoritmo.
- Los cálculos de riesgo operan sobre el capital de la cartera proporcionado por StockSharp en lugar de consultas de saldo de cuenta desde MT4.

## Archivos

- `CS/Ais2TradingRobot20005Strategy.cs` – Implementación de estrategia utilizando API de alto nivel de StockSharp.
- `README.md` – Descripción en inglés (este archivo).
- `README_zh.md` – Traducción al chino.
- `README_ru.md` – traducción al ruso.
