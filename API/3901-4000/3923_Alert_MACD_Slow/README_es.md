# Alerta MACD Estrategia lenta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Alerta MACD Lenta** reproduce el MetaTrader 4 experto `Alert_MACD_Slow.mq4`. Observa la línea principal MACD y dos promedios móviles exponenciales y genera alertas textuales cuando la pila de indicadores señala una posible ruptura. No se envían pedidos: la conversión se mantiene fiel al asesor original, que solo mostraba mensajes emergentes.

## Idea central

1. Suscríbase a la serie de velas seleccionada y alimente un MACD(3, 20, 9) junto con EMA rápidas y lentas (20 y 65 períodos).
2. Almacene en caché los valores MACD de las cuatro velas completadas anteriores para evaluar las transiciones de pendiente utilizadas por el código MQL.
3. Almacene los máximos y mínimos de las dos últimas velas para emular los filtros de ruptura `High[1]/High[2]` y `Low[1]/Low[2]`.
4. Cuando el EMA rápido permanece por encima (o por debajo) del EMA lento y el cierre de la vela rompe los máximos (o mínimos) memorizados mientras que MACD sube (o baja) por debajo de la línea cero, registre el mensaje de alerta respectivo.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `MacdFastPeriod` | `3` | Longitud rápida de EMA dentro del cálculo de MACD. |
| `MacdSlowPeriod` | `20` | Longitud lenta de EMA utilizada por el MACD. |
| `MacdSignalPeriod` | `9` | Período de suavizado de señal del MACD. |
| `QuickEmaPeriod` | `20` | Período del rápido seguimiento de tendencias EMA (`Ma_Quick`). |
| `SlowEmaPeriod` | `65` | Período del filtro de tendencia EMA lenta (`Ma_Slow`). |
| `CandleType` | `TimeFrame(30m)` | La fuente de la vela pasó a la cadena del indicador; elija un período de tiempo que coincida con su gráfico. |

## Detalles de la lógica de alerta

- **MACD memoria de pendiente**: la estrategia cambia los valores MACD anteriores internamente en lugar de llamar a `GetValue`, satisfaciendo las pautas de conversión y preservando las comparaciones originales (`Macd_1 > Macd_2`, etc.).
- **Comprobación de ruptura**: los precios de cierre por encima de los máximos anteriores o por debajo de los mínimos anteriores se tratan como un indicador de las comprobaciones de oferta/demanda de MetaTrader, que utilizó la cotización en vivo contra los extremos históricos de las velas.
- **Filtro de tendencias**: la alerta se activa solo cuando el EMA rápido está en el lado correcto del EMA lento, coincidiendo con los filtros largos/cortos en el experto MQL.
- **Registro**: las alertas se envían a través de `AddInfoLog`. Incluyen los cuatro valores MACD almacenados en caché y los niveles de ruptura para facilitar la depuración y las pruebas retrospectivas.
- **Sin operaciones**: debido a que el asesor de origen nunca realizó operaciones, la conversión StockSharp mantiene la estrategia plana y se centra únicamente en la señalización.

## Uso típico

1. Adjunte la estrategia a un símbolo, configure el tipo de vela en el período de tiempo deseado y mantenga los períodos de indicador predeterminados o ajústelos para experimentar.
2. Inicie la estrategia y espere hasta que se formen los indicadores MACD y EMA (se necesitan varias velas porque MACD requiere historial).
3. Mire el diario: cuando aparezca una configuración alcista verá `SET UP LONG`, mientras que las configuraciones bajistas producirán `SET UP SHORT_VALUE`. El sufijo refleja el texto de alerta original.
4. Utilice los diagnósticos impresos para decidir si actuar manualmente o encadenar la estrategia con una automatización personalizada.

## Clasificación

- **Categoría**: Alertas/Confirmación de ruptura de tendencia
- **Dirección comercial**: Ninguna (solo señal)
- **Estilo de ejecución**: Basado en eventos en velas terminadas
- **Requisitos de datos**: Serie de velas compatible con el `CandleType` elegido
- **Complejidad**: Moderada (múltiples filtros de indicador, pero manejo de estado sencillo)
- **Gestión de Riesgos**: No aplicable (no hay posiciones abiertas)

Este puerto mantiene el comportamiento de alerta del experto MQL mientras aprovecha las suscripciones de StockSharp, enlaces de indicadores y utilidades de registro.
