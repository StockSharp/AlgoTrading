# Estrategia Operador de Línea Cruzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia emula el experto original de MetaTrader "Cross Line Trader" reaccionando a las interacciones de precio con líneas sintéticas definidas por el usuario. En lugar de escuchar objetos de gráfico manuales, la versión StockSharp recibe todas las descripciones de líneas a través de un único parámetro, las analiza al inicio y monitorea continuamente las velas terminadas. Cuando el apertura de una vela se mueve a través de una línea activa, la estrategia coloca una orden de mercado en la dirección correspondiente y desactiva esa línea para que no pueda volver a dispararse.

## Lógica de trading
1. La estrategia se suscribe al tipo de vela seleccionado en el parámetro **Candle Type** y solo procesa velas en estado `Finished` para evitar el ruido intrabarra.
2. Las líneas sintéticas se crean a partir del parámetro **Line Definitions**. Cada línea mantiene su propio estado (activa/expirada, número de barras procesadas y geometría).
3. Para líneas **Trend** u **Horizontal**, el algoritmo compara la apertura de la vela anterior con la siguiente relativa a la trayectoria de precio de la línea:
   - Una señal larga ocurre cuando la apertura anterior está por debajo de la línea y la apertura actual se mueve por encima.
   - Una señal corta ocurre cuando la apertura anterior está por encima de la línea y la apertura actual se mueve por debajo.
4. Las líneas **Vertical** funcionan como disparadores temporizados. Una vez que ha transcurrido el número configurado de barras, la estrategia abre una posición inmediatamente a la apertura de la vela actual.
5. La dirección se resuelve según **Direction Mode**:
   - `FromLabel` compara cada etiqueta de línea con **Buy Label** y **Sell Label**.
   - `ForceBuy` y `ForceSell` tratan todas las líneas en la misma dirección independientemente de las etiquetas.
6. Cada disparador exitoso envía una orden de mercado con el volumen de **Trade Volume**, registra la activación y marca la línea como inactiva.
7. Las distancias opcionales de stop-loss y take-profit se aplican en cada nueva vela evaluando el último precio de entrada frente a los máximos y mínimos de la vela.

## Formato de definición de líneas
La cadena **Line Definitions** usa punto y coma para separar entradas. Cada entrada debe seguir:

```
Name|Type|Label|BasePrice|SlopePerBar|Length|Ray
```

- **Name** – identificador que se muestra en los registros. Cualquier cadena sin punto y coma.
- **Type** – `Horizontal`, `Trend` o `Vertical` (sin distinción de mayúsculas).
- **Label** – texto libre usado cuando **Direction Mode** es `FromLabel`.
- **BasePrice** – precio inicial de la línea en la primera vela procesada. Requerido para cada línea no vertical (decimal, cultura invariante).
- **SlopePerBar** – cambio de precio por vela para una línea de tendencia. Use `0` para líneas horizontales.
- **Length** – el significado depende del tipo de línea:
  - Para líneas de tendencia u horizontales sin ray, define cuántas barras está el ancla derecha desde el inicio. Después de este conteo, la línea expira automáticamente.
  - Para líneas ray, el valor se ignora porque la línea se extiende indefinidamente.
  - Para líneas verticales, especifica cuántas barras esperar antes de disparar. El valor mínimo aceptado es `1`.
- **Ray** – `true` mantiene la línea activa indefinidamente a la derecha, `false` la restringe a la longitud especificada.

Ejemplo:

```
TrendLine|Trend|Buy|1.1000|0.0005|8|false;HorizontalSell|Horizontal|Sell|1.1050|0|0|true;VerticalImpulse|Vertical|Buy|0|0|1|false
```

El ejemplo crea una línea de tendencia de compra ascendente, un nivel horizontal de venta que nunca expira y un disparador vertical único para la próxima vela.

## Parámetros
- **Candle Type** – tipo de dato de mercado usado para los cálculos. Por defecto el marco temporal de 1 minuto.
- **Trade Volume** – tamaño de la orden para nuevas entradas. Debe ser positivo.
- **Direction Mode** – determina cómo se selecciona el lado de entrada (`FromLabel`, `ForceBuy`, `ForceSell`).
- **Buy Label** / **Sell Label** – valores de etiqueta para identificar líneas cuando **Direction Mode** es `FromLabel`.
- **Line Definitions** – cadena bruta que describe cada línea sintética (ver formato arriba).
- **Stop Loss Offset** – distancia en unidades de precio para salidas de protección en posiciones largas y cortas (0 deshabilita la comprobación).
- **Take Profit Offset** – distancia de precio para objetivos de beneficio (0 deshabilita la comprobación).

## Gestión de riesgo
La estrategia no coloca órdenes de stop o take profit por separado. En cambio, monitorea cada vela terminada:
- Las posiciones largas se cierran si el mínimo de la vela supera `EntryPrice - StopLossOffset` o el máximo excede `EntryPrice + TakeProfitOffset`.
- Las posiciones cortas se cierran si el máximo de la vela supera `EntryPrice + StopLossOffset` o el mínimo cae por debajo de `EntryPrice - TakeProfitOffset`.

Si ambos offsets son cero, la posición solo se cerrará por la señal opuesta o intervención manual.

## Notas de implementación
- Todos los comentarios en el código fuente están en inglés para mantener la coherencia con las directrices del proyecto.
- La estrategia ignora silenciosamente las definiciones de línea inválidas; asegúrese de que el formato sea correcto para evitar perder disparadores.
- Reiniciar la estrategia limpia el estado interno, por lo que los contadores de líneas y los temporizadores de activación comienzan de nuevo desde la primera vela procesada.
- El enfoque se centra en los precios de apertura de las velas, igual que el EA original, y no reaccionará a los toques intrabarra.

## Uso
1. Configurar el instrumento de trading y el tipo de vela deseado.
2. Ajustar **Line Definitions** para describir cada línea manual con la que desea operar.
3. Configurar **Direction Mode** para confiar en etiquetas o forzar el trading unidireccional.
4. Opcionalmente establecer offsets de stop-loss y take-profit para salidas automáticas.
5. Iniciar la estrategia y monitorear los registros: cada línea disparada se reporta junto con su dirección y precio de activación.
