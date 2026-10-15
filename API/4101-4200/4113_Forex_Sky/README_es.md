# Estrategia Forex Cielo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Forex Sky** es una adaptación directa del MetaTrader asesor experto `Forex_SKY.mq4`. Negocia MACD cambios de impulso y se limita estrictamente a una única posición por día de negociación. La implementación StockSharp mantiene los umbrales originales MACD y el control de seguridad que evita más de un pedido por vela.

La estrategia se suscribe al marco temporal definido por `CandleType` (velas de 15 minutos por defecto) y evalúa el clásico MACD (26/12/9) al cierre de cada vela completa.

## Lógica de trading
- **Entrada larga** – Realice una compra en el mercado cuando:
  - La línea principal actual MACD está por encima de cero;
  - También supera `+0.00009` para confirmar el impulso;
  - Al menos una de las tres lecturas anteriores de MACD fue menor o igual a cero (capturando un giro alcista desde territorio negativo).
- **Entrada corta**: realice una venta de mercado cuando se cumpla alguna de las siguientes condiciones:
  - La línea principal MACD está por debajo de cero, cae por debajo de `-0.0004`, al menos una de las últimas tres lecturas no fue negativa y el valor de hace cuatro barras fue al menos `+0.001`.
  - **O** el valor de hace cuatro barras era `≥ +0.003`, lo que autoriza inmediatamente una operación corta como en el código MetaTrader original.
- **Gestión de posiciones**: el algoritmo nunca abre más de una orden por vela (`Time0` guardia) y nunca negocia más de una vez por día calendario (`CheckTodaysOrders` guardia). Las órdenes de salida de protección son manejadas por el ayudante StockSharp `StartProtection`, por lo que todas las paradas y objetivos permanecen sincronizados con el volumen actual.

No existe una lógica de liquidación autónoma más allá de las órdenes de protección: se espera que las posiciones se cierren mediante toma de ganancias, stop-loss o intervención manual, reflejando el comportamiento del asesor experto original.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `FastPeriod` | 12 | Longitud rápida EMA del indicador MACD. |
| `SlowPeriod` | 26 | Longitud lenta de EMA del indicador MACD. |
| `SignalPeriod` | 9 | Longitud de la señal EMA del indicador MACD. |
| `TakeProfitPoints` | 100 | Distancia a la orden de toma de ganancias expresada en puntos del instrumento. Convertido a precio multiplicando por el paso del precio del valor. |
| `StopLossPoints` | 3000 | Distancia a la orden stop-loss en puntos del instrumento. |
| `TradeVolume` | 0.1 | Tamaño base de la orden del mercado (lotes). |
| `CandleType` | plazo de 15 minutos | Plazo que alimenta los cálculos y decisiones comerciales de MACD. |

### Cálculo del punto del instrumento
`TakeProfitPoints` y `StopLossPoints` se especifican exactamente igual que la versión MetaTrader: `Point` en MQL4 corresponde a `Security.PriceStep` en StockSharp. Para un par de divisas de cinco dígitos (`PriceStep = 0.00001`), la configuración predeterminada se traduce en:
- Take-profit: `100 × 0.00001 = 0.001` unidades de precio.
- Stop-loss: `3000 × 0.00001 = 0.03` unidades de precio.

## Gestión del riesgo
`StartProtection` instala automáticamente las órdenes de obtención de ganancias y de limitación de pérdidas después de completar una entrada. Están vinculados a la dirección comercial y utilizan órdenes de mercado cuando se activan, coincidiendo con el comportamiento MetaTrader. Establezca cualquiera de los parámetros en `0` para desactivar la orden de protección correspondiente.

## Notas de migración
- El búfer de historial MACD mantiene los últimos cuatro valores completados en los campos de clase, por lo que no se requieren llamadas de indicador con índices desplazados.
- La limitación del comercio diario y la restricción de un solo comercio por barra replican `CheckTodaysOrders()` y `Time0` de la fuente original.
- Todos los comentarios se reescribieron en inglés y la lógica se basa en StockSharp enlaces de alto nivel (`Bind`) para el procesamiento de indicadores.

## Consejos de uso
- Ajuste `CandleType` al período del gráfico que desea emular; el script original hereda el período de tiempo del gráfico automáticamente.
- Dado que solo se permite una operación por día, elija mercados con oscilaciones intradiarias significativas o considere aumentar los umbrales de MACD cuando utilice instrumentos de mayor volatilidad.
- Supervise el reloj/zona horaria de la plataforma para asegurarse de que el límite del día coincida con su sesión de negociación, ya que el contador de límite se reinicia según la fecha de apertura de la vela.
