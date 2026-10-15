# Estrategia Bull & Bear Candle Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La estrategia reacciona a velas alcistas y bajistas fuertes y abre posiciones de mercado en la misma dirección. Usa una secuencia martingala independiente para cada lado: las posiciones largas escalan volumen con el *Bull Multiplier*, mientras que las cortas usan el *Bear Multiplier*. Las distancias protectoras de stop-loss y take-profit también se configuran por separado para cada dirección, permitiendo control preciso del comportamiento asimétrico que expone el experto MQL original.

## Lógica de negociación
1. Suscribirse al tipo de vela configurado (por defecto, 1 minuto) y esperar solo velas completadas.
2. Cuando no hay posición abierta:
   - **Configuración alcista:** si `Close > Open` y el tamaño del cuerpo de vela supera el filtro alcista, comprar a mercado.
   - **Configuración bajista:** si `Close < Open` y el tamaño del cuerpo supera el filtro bajista, vender a mercado.
3. Cada entrada establece órdenes de stop-loss y take-profit convertidas desde distancias en pips al paso de precio del instrumento.
4. Cuando una posición se cierra, el PnL realizado se compara con la referencia previa:
   - Un resultado negativo multiplica el volumen martingala correspondiente.
   - Un resultado positivo o break-even reinicia ese lado al volumen inicial.
5. Se ignoran señales nuevas mientras una posición está abierta, reproduciendo el comportamiento de una sola operación del EA fuente.

## Gestión monetaria
- Los ciclos martingala largos y cortos se siguen de forma independiente, por lo que una racha perdedora larga no afectará la siguiente operación corta, y viceversa.
- Los volúmenes se alinean con `VolumeStep` del instrumento para evitar rechazos.
- `StartProtection(useMarketOrders: true)` habilita el manejo de órdenes protectoras de StockSharp para los niveles stop y take adjuntos.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **Initial Volume** | Volumen base que inicia cada ciclo martingala en ambas direcciones. |
| **Bull Multiplier** | Multiplicador aplicado a la siguiente operación alcista después de una posición larga perdedora. |
| **Bear Multiplier** | Multiplicador aplicado a la siguiente operación bajista después de una posición corta perdedora. |
| **Bull Stop Loss** | Distancia de stop-loss en pips para operaciones alcistas. Se convierte a precio usando el paso del instrumento. |
| **Bull Take Profit** | Distancia de take-profit en pips para operaciones alcistas. |
| **Bear Stop Loss** | Distancia de stop-loss en pips para operaciones bajistas. |
| **Bear Take Profit** | Distancia de take-profit en pips para operaciones bajistas. |
| **Bull Body Filter** | Cuerpo mínimo de vela alcista en pips requerido para disparar una compra. |
| **Bear Body Filter** | Cuerpo mínimo de vela bajista en pips requerido para disparar una venta. |
| **Candle Type** | Marco temporal usado para generar señales (por defecto, 1 minuto). |

## Notas de uso
- Asegúrese de que el instrumento conectado exponga valores válidos de `PriceStep` y `VolumeStep`. La estrategia usa 0.0001 por defecto cuando no se proporciona `PriceStep`.
- La lógica martingala depende del PnL realizado, por lo que el cierre manual de posiciones seguirá actualizando correctamente la secuencia.
- La optimización puede centrarse en filtros de cuerpo y combinaciones de multiplicadores para equilibrar capacidad de respuesta frente a drawdown.
