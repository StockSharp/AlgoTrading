# Estrategia Alternante Cheduecoglioni
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto MQL5 "cheduecoglioni". Mantiene siempre al trader en el mercado alternando entre posiciones cortas y largas. Cada entrada está protegida con niveles fijos de take-profit y stop-loss definidos en pips y convertidos a desplazamientos de precio de acuerdo con la precisión del instrumento.

## Reglas de trading
- La estrategia escucha la serie de velas configurada (1 minuto por defecto) y solo reacciona una vez que una vela está completamente cerrada. Este evento reemplaza el bucle basado en ticks del asesor experto original.
- Cuando no hay posición abierta y ninguna orden de mercado pendiente de ejecución, la estrategia envía una orden de mercado en la dirección almacenada en el estado `_nextSide`. La primera operación después del inicio es una venta, coincidiendo con la implementación MQL5.
- Tan pronto como una posición se vuelve activa, el algoritmo espera a que se cierre ya sea por las órdenes de protección o intervención manual. Una vez que la posición vuelve a cero, la siguiente dirección se invierte, por lo que la siguiente operación será en la dirección opuesta.
- Las distancias de stop-loss y take-profit son aplicadas automáticamente por `StartProtection`, asegurando que cada operación lleve las distancias de riesgo-recompensa configuradas.

## Parámetros
- `Trade Volume` – volumen usado para cada entrada de mercado. Esto refleja el input `InpLots`.
- `Take Profit (pips)` – distancia en pips para la orden take-profit. La estrategia la convierte a distancia de precio absoluta usando el tamaño de pip detectado.
- `Stop Loss (pips)` – distancia en pips para el stop loss de protección, convertida con la misma lógica de tamaño de pip.
- `Candle Type` – marco temporal de las velas que impulsan el ciclo de decisión. Se puede suministrar cualquier `DataType` compatible.

## Detalles de implementación
- El tamaño de pip se deriva de `Security.PriceStep`. Para símbolos FX de 3 o 5 dígitos, el valor se multiplica por 10 para pasar del pip fraccionario al pip estándar, replicando el ajuste MQL.
- Un indicador de espera previene órdenes de mercado duplicadas mientras una orden anterior está pendiente de ejecución. Si el broker rechaza la orden, `OnOrderFailed` limpia el indicador para que la siguiente vela pueda reintentar.
- `OnPositionChanged` hace un seguimiento del lado de la posición activa y cambia `_nextSide` después de cada estado plano. Esto refleja la lógica MQL que abría el lado opuesto después de cada salida.
- Las órdenes de protección son gestionadas por `StartProtection` con salidas de mercado, coincidiendo con la asignación inmediata de stop-loss y take-profit que el asesor experto realizaba al colocar la orden.

## Notas
- La versión Python no se ha creado intencionadamente aún.
- La estrategia no modifica las pruebas unitarias.
