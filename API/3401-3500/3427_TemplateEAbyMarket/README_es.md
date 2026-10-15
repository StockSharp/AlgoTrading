# PlantillaEAbyMarket Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
TemplateEAbyMarket es una adaptación directa StockSharp del asesor experto original MetaTrader 4 *TemplateEAbyMarket.mq4*. La estrategia utiliza el indicador de divergencia de convergencia de media móvil (MACD) para detectar cambios de impulso. Cuando la línea principal MACD cruza la línea de señal mientras ambos componentes están en la misma zona positiva o negativa, la estrategia abre una posición de mercado en la dirección del cruce. Las salidas se gestionan exclusivamente a través de órdenes de protección (takeprofit y stop loss) configuradas a través del asistente integrado `StartProtection`.

La versión StockSharp mantiene el comportamiento del programa MQL: solo abre nuevas posiciones sin intentar cerrar automáticamente el lado opuesto. Una vez que se cubre una posición, la operación se deja gestionar mediante niveles de protección o intervención manual.

## Lógica de trading
1. Suscríbase al tipo de vela seleccionado por el usuario (predeterminado: período de tiempo de 15 minutos).
2. Calcule MACD (26/12/9 de forma predeterminada) en cada vela terminada.
3. Realice un seguimiento de la posición relativa de las líneas principal y de señal MACD para detectar un evento de cruce:
   - **Configuración alcista:** la vela anterior tenía la línea principal debajo de la línea de señal, la vela actual cierra con la línea principal por encima de la línea de señal y ambas líneas están por encima de cero. Se envía una orden de compra de mercado con `OrderVolume` si la exposición actual es inferior a `MaxOrders * OrderVolume`.
   - **Configuración bajista:** la vela anterior tenía la línea principal por encima de la línea de señal, la vela actual cierra con la línea principal por debajo de la línea de señal y ambas líneas están por debajo de cero. Se envía una orden de venta de mercado con `OrderVolume` sujeta al mismo límite de exposición.
4. Los niveles de protección `takeProfit` y `stopLoss` se activan una vez al inicio. La estrategia no cierra automáticamente posiciones opuestas; El riesgo es controlado por el módulo de protección o por el usuario.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| `MacdFastPeriod` | Longitud rápida de EMA para el cálculo de MACD. |
| `MacdSlowPeriod` | Longitud lenta de EMA para el cálculo de MACD. |
| `MacdSignalPeriod` | Longitud de la señal EMA para el cálculo MACD. |
| `CandleType` | Tipo de vela (marco de tiempo) que alimenta el indicador. |
| `OrderVolume` | Volumen presentado con cada orden de mercado. |
| `MaxOrders` | Número máximo de pedidos simultáneos, expresado como múltiplos de `OrderVolume`. La estrategia verifica `abs(Position) < MaxOrders * OrderVolume` antes de enviar un nuevo pedido. |
| `TakeProfitPoints` | Distancia de obtención de beneficios en puntos de precio. El valor `0` deshabilita la toma de ganancias. |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. El valor `0` desactiva el stop loss. |

## Notas
- Las configuraciones de deslizamiento y números mágicos de la versión MQL se omiten intencionalmente porque se manejan de manera diferente en StockSharp.
- Asegúrese de que el conector proporcione metadatos de pasos de precios adecuados; `StartProtection` interpreta distancias en puntos de precio de instrumentos.
- La plantilla es intencionalmente minimalista y no gestiona rellenos parciales ni entradas de pirámide más allá del límite `MaxOrders`.
