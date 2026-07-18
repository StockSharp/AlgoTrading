# Temporizador EA Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto StockSharp del robot MetaTrader TimerEA. Se centra en abrir y cerrar operaciones en fechas y horas programadas.
con órdenes pendientes opcionales, protección de seguimiento y manejo de equilibrio.

## Lógica de trading

- **Horario**
  - `OpenTime` activa la colocación de pedidos una vez que la primera vela terminada alcanza el minuto configurado.
  - `CloseTime` fuerza la liquidación de posiciones y, opcionalmente, cancela las órdenes pendientes restantes.
- **Modos de pedido**
  - Se pueden seleccionar entradas de mercado, stop o límite. Las órdenes pendientes se colocan a una distancia configurable (en pasos de precio) y pueden
caducan después del número de minutos especificado.
- **Control de dirección**
  - Los interruptores separados permiten habilitar operaciones largas y/o cortas. Cada lado envía una orden por ejecución.
- **Gestión de riesgos**
  - El volumen fijo o el tamaño basado en el equilibrio (usando `RiskFactor`) imita la selección de lote original.
  - Las distancias de stop-loss y take-profit se expresan en incrementos de precio y se recrean después de cada entrada.
  - La lógica de trailing stop mantiene el stop en un desplazamiento constante una vez que las ganancias exceden el buffer `BreakEvenSteps`. El sendero se activa
sólo cuando la parada ya está más allá del desplazamiento inicial más el `TrailingStep`.
- **Protecciones**
  - El requisito de equilibrio opcional evita el retraso hasta que se alcanza el umbral mínimo de beneficio.
  - Las órdenes pendientes que sobreviven a su vencimiento se cancelan automáticamente.

## Parámetros predeterminados

- Modo de pedido: Mercado.
- Abierto compra/venta: deshabilitado.
- Take Profit / Stop Loss: 10 pasos cada uno.
- Trailing stop y punto de equilibrio: desactivados.
- Distancia pendiente: 10 pasos con 60 minutos de vencimiento.
- Tamaño del lote: Volumen manual = 1,0 (factor de riesgo = 1,0 para el modo de equilibrio).
- Tipo de vela: intervalo de tiempo de 1 minuto.

## Notas

- La estrategia opera con velas terminadas y, por lo tanto, reacciona con hasta una barra de latencia.
- StockSharp utiliza un modelo de posición neta, por lo que no se admite la exposición larga y corta simultánea incluso si ambas alternancias están activadas.
habilitado.
- Los incrementos de precios se calculan con `Security.PriceStep`. Los instrumentos sin un paso configurado tratarán las distancias como precio bruto
puntos.
