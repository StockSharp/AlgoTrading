# Estrategia de etiquetas de beneficios
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de etiquetas de beneficios** convierte al MetaTrader 5 asesor experto *Etiquetas de beneficios (54352)* en la API de alto nivel de StockSharp. La estrategia monitorea los cruces de la media móvil exponencial triple (TEMA) para abrir posiciones y dibuja etiquetas de ganancias en el gráfico después de cerrar una posición. Cuando la tendencia gira hacia arriba, el algoritmo abre una posición larga, y cuando la tendencia gira hacia abajo, abre una posición corta. Si una posición opuesta todavía está activa, la estrategia primero la cierra e imprime la etiqueta de beneficio realizado.

Las velas se procesan a través de una suscripción `SubscribeCandles` y el indicador está vinculado a través de `Bind` para mantener la implementación en pleno nivel alto. Las velas terminadas actualizan los valores de TEMA y desencadenan decisiones comerciales.

## Reglas de trading

1. **Cruce alcista**: cuando el valor actual de TEMA se mueve por encima del valor anterior mientras que las lecturas anteriores muestran una pendiente descendente, la estrategia abre una posición larga si no hay ninguna larga activa actualmente.
2. **Cruce bajista**: cuando el TEMA baja de la misma manera, abre una posición corta si no hay ningún corto activo.
3. **Inversión de posición**: si existe una posición opuesta en el momento de una nueva señal, la estrategia cierra la posición abierta antes de realizar una nueva orden.
4. **Etiquetas de ganancias**: una vez que la posición está completamente cerrada, el PnL realizado se calcula y se muestra en el gráfico usando `DrawText`.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Marco de tiempo utilizado para la suscripción de velas. |
| `TemaPeriod` | `6` | Período de la Media Móvil Exponencial Triple. |
| `TradeVolume` | `0.1` | Volumen presentado con cada orden de mercado. |
| `PlacingTrade` | `false` | Habilita o deshabilita la colocación de pedidos en vivo. |
| `LabelOffset` | `0` | Compensación vertical aplicada a la etiqueta de ganancias por encima del precio comercial. |

## Notas

- La estrategia se basa únicamente en velas terminadas y no accede directamente a los buffers del indicador.
- Los niveles protectores de stop-loss y take-profit de la versión MQL no se replican; Las posiciones se invierten cuando llega una señal opuesta.
- Las etiquetas de ganancias utilizan la moneda de seguridad siempre que esté disponible y, de lo contrario, recurren a valores brutos.
