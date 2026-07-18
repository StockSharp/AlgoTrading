# Estrategia de volumen adaptable MA2CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MA2CCI es una adaptación directa del asesor experto MetaTrader 4 distribuido originalmente como "MA2CCI.mq4". El sistema combina un cruce de promedio móvil simple rápido/lento (SMA) con una confirmación de línea cero del índice de canales de productos básicos (CCI). Cada cruce validado abre una única posición de mercado e inmediatamente coloca una parada protectora basada en el rango verdadero promedio (ATR). El tamaño de la posición sigue la lógica original de gestión del dinero al escalar el tamaño de la orden en relación con el capital y reducirlo después de rachas de operaciones perdedoras.

## Indicadores y datos
- **Rápido SMA (FMa)** y **Lento SMA (SMa)** en el período de tiempo configurado para detectar cambios de tendencia.
- **Índice del canal de productos básicos (CCI)** con el mismo flujo de precios para confirmar la dirección del impulso a través de cruces de línea cero.
- **Rango verdadero promedio (ATR)** para medir la volatilidad reciente y derivar la distancia de stop-loss.
- Las **velas** del período de tiempo elegido (predeterminado, 15 minutos) proporcionan la serie de entrada para todos los indicadores.

## Reglas de trading
- **Entrada larga**: El SMA rápido cruza por encima del SMA lento mientras que CCI cruza de negativo a positivo en la misma barra, no hay ninguna posición abierta y se permite operar. Se envía una orden de compra de mercado y se arma un stop-loss en `close − ATR × AtrMultiplier`.
- **Entrada corta**: El SMA rápido cruza por debajo del SMA lento mientras que CCI cruza de positivo a negativo, no hay ninguna posición abierta. Se coloca una orden de venta de mercado con un límite de pérdidas en `close + ATR × AtrMultiplier`.
- **Salida de posiciones largas**: si el SMA rápido vuelve a cruzar por debajo del SMA lento, toda la posición larga se cierra en el mercado. También se anula la parada de protección.
- **Salida para cortos**: Si el SMA rápido vuelve a cruzar por encima del SMA lento, la posición corta se cubre en el mercado y se cancela el stop.
- **Stop-loss**: cada nueva posición restaura un stop de volatilidad que refleja la lógica MetaTrader. Las paradas se recalculan solo con nuevas entradas y se almacenan como órdenes condicionales separadas.

## Dimensionamiento de posiciones
- El tamaño del lote base comienza desde el parámetro `BaseVolume` (lote predeterminado 0,1).
- Si `RiskFraction` es positivo, la estrategia calcula un tamaño adicional usando `equity × RiskFraction / 1000`, imitando la fórmula original `AccountFreeMargin`, y usa el máximo entre ambos valores.
- Después de dos o más operaciones perdedoras consecutivas, el tamaño del lote se reduce en `volume × losses / DecreaseFactor`, replicando el control de reducción de `DcF`.
- Los volúmenes están normalizados al `VolumeStep` del instrumento.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `FastMaPeriod` | 4 | Período de retrospectiva rápido SMA. |
| `SlowMaPeriod` | 8 | Período de retrospectiva lento SMA. |
| `CciPeriod` | 4 | Período del índice del canal de productos básicos. |
| `AtrPeriod` | 4 | Período promedio de rango verdadero utilizado para la distancia de parada. |
| `AtrMultiplier` | 1.0 | Multiplicador aplicado a ATR antes de colocar el stop-loss. |
| `BaseVolume` | 0.1 | Tamaño mínimo de operación antes de los ajustes de riesgo. |
| `RiskFraction` | 0,02 | Fracción de capital arriesgada por operación (por 1000 unidades monetarias). |
| `DecreaseFactor` | 3 | Divisor que controla qué tan rápido se reduce el tamaño después de las pérdidas. |
| `CandleType` | velas de 15 minutos | Plazo utilizado para indicadores y señales. |

## Notas
- Las notificaciones por correo electrónico del asesor experto original (`SndMl`) se omiten intencionadamente.
- Solo se puede abrir una posición a la vez, lo que coincide con el comportamiento MetaTrader del código fuente.
- Las paradas protectoras se recrean cada vez que la posición cambia o se cierra para evitar que las órdenes huérfanas permanezcan en el libro.
