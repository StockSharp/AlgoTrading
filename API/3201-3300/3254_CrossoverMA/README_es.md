# Estrategia de CrossoverMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un puerto de StockSharp del asesor experto MetaTrader 5 **CrossoverMA.mq5**. El robot original espera a que una vela cruce una media móvil y solo abre una posición cuando la media se inclina en la misma dirección que el rompimiento. La versión de StockSharp mantiene el mismo comportamiento aprovechando la API de alto nivel para suscripciones de velas, gestión de indicadores y renderizado automático de gráficos.

## Lógica de trading

1. Suscribirse a la serie de velas configurada y calcular una media móvil simple (SMA) sobre el precio de cierre de la vela.
2. Cuando se recibe una vela terminada, medir:
   - Las distancias de apertura y cierre de la vela desde la SMA.
   - La pendiente de la SMA comparando el valor actual con el anterior.
3. Generar señales:
   - **Rompimiento alcista** – la vela abre por debajo de la SMA, cierra por encima de ella, y la SMA está subiendo. La estrategia cierra cualquier exposición corta y abre/extiende una posición larga.
   - **Rompimiento bajista** – la vela abre por encima de la SMA, cierra por debajo de ella, y la SMA está cayendo. La estrategia cierra cualquier exposición larga y abre/extiende una posición corta.
4. Ignorar señales duplicadas que no cambien el lado de la posición actual.

El puerto mantiene la regla de MetaTrader de que solo se procesan velas terminadas y que se requiere una vela extra antes del primer trade (para medir la pendiente de la SMA).

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| ---- | ----------- | ------- | ----- |
| `Candle Type` | Marco temporal utilizado para construir velas. | Marco temporal de 1 minuto | Se puede seleccionar cualquier tipo de dato de vela compatible con StockSharp. |
| `MA Length` | Número de velas completadas incluidas en la SMA. | 12 | Coincide con el período predeterminado del experto MetaTrader. |
| `Trade Volume` | Volumen de orden de mercado para entradas. | 1 | La estrategia cierra la exposición opuesta antes de abrir una nueva posición. |

Todos los parámetros están disponibles para optimización en StockSharp Designer o Runner.

## Notas de implementación

- La estrategia se basa en `SubscribeCandles` y `Bind` para que los valores del indicador se transmitan directamente al método de procesamiento sin gestión manual del historial.
- La SMA se almacena en un campo privado para dibujarla en el área del gráfico cuando hay una disponible.
- Las señales se procesan solo cuando `IsFormedAndOnlineAndAllowTrading()` devuelve `true`, asegurando que la estrategia respete el estado global de trading.
- Las reversiones de posición siguen la plantilla de MetaTrader: cerrar la exposición actual primero, luego abrir el nuevo lado con el volumen de trade configurado.

## Archivos

- `CS/CrossoverMaStrategy.cs` – implementación en C# de la estrategia convertida.
- `README.md` – documentación en inglés.
- `README_zh.md` – documentación en chino.
- `README_ru.md` – documentación en ruso.

## Diferencias de portabilidad

- Las clases de gestión de dinero, stop de seguimiento y otros marcos de MetaTrader se omiten porque StockSharp gestiona el dimensionamiento de posiciones y el riesgo externamente. El parámetro `Trade Volume` reemplaza la configuración de lotes fijos del experto original.
- MetaTrader usaba series de datos separadas para los precios de apertura y cierre de velas. Las velas de StockSharp ya incluyen ambos precios, por lo que no se requieren indicadores adicionales.
- La inicialización, validación y gestión del ciclo de vida del indicador son manejadas automáticamente por StockSharp, eliminando el extenso código de plantilla de la versión MQL.
