# Estrategia Twenty 200 Pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el experto original **20/200 pips** de MQL5. Examina velas horarias y compara dos precios de apertura históricos (`Open[t1]` y `Open[t2]`). Cuando la diferencia entre estas aperturas supera un delta configurable durante una hora específica, la estrategia entra en una única operación para la sesión y se basa en niveles fijos de take-profit y stop-loss.

## Lógica de trading
1. Suscribirse a velas horarias (configurable) y alimentar el precio de apertura a dos indicadores `Shift` para recuperar las aperturas en los índices requeridos.
2. Durante cada vela completada, restablecer la bandera "puede operar" una vez que la hora actual es mayor que la hora de trading configurada. Esto refleja el restablecimiento diario en el asesor experto original.
3. Cuando la hora coincide con la hora de trading configurada y no hay posición abierta, comparar los precios de apertura almacenados:
   - Si `Open[t1] > Open[t2] + delta`, enviar una orden de **venta** de mercado.
   - Si `Open[t1] + delta < Open[t2]`, enviar una orden de **compra** de mercado.
4. Después de enviar una orden, la estrategia prohíbe nuevas entradas hasta el próximo restablecimiento diario. Las órdenes de take-profit y stop-loss de protección se gestionan mediante `StartProtection`.

## Parámetros
- `TakeProfit` – distancia en puntos de precio para la orden de take-profit (por defecto 200 puntos).
- `StopLoss` – distancia en puntos de precio para la orden de stop-loss (por defecto 2000 puntos).
- `TradeHour` – hora del día en que se realiza la verificación de entrada (por defecto 18).
- `FirstOffset` – índice del precio de apertura más antiguo (corresponde a `Open[t1]` en el script MQL, por defecto 7).
- `SecondOffset` – índice del precio de apertura más reciente (`Open[t2]`, por defecto 2).
- `DeltaPoints` – diferencia mínima en puntos entre las dos aperturas para activar una operación (por defecto 70).
- `Volume` – tamaño de la orden usado para entradas de mercado (por defecto 0.1).
- `CandleType` – período de tiempo usado para los cálculos (por defecto velas de 1 hora).

## Notas de implementación
- Los indicadores `Shift` se procesan manualmente para acceder a los precios de apertura históricos sin mantener colecciones personalizadas.
- La estrategia llama a `StartProtection` una vez durante `OnStarted` para emular los niveles de stop-loss/take-profit definidos en el experto MQL.
- Los comentarios en inglés se incluyen directamente en el código para facilitar el mantenimiento y la revisión.
- Solo se permite una operación por día porque `_canTrade` se limpia justo después de colocar una orden y se restaura solo después de que haya pasado la hora de trading configurada.

## Uso
1. Adjuntar la estrategia a un instrumento y configurar los parámetros según el instrumento objetivo.
2. Asegurarse de que el instrumento tenga un `PriceStep` válido; se usa para convertir parámetros basados en puntos en distancias de precio absolutas.
3. Iniciar la estrategia. Esperará hasta la hora configurada y actuará en la próxima vela completada si se cumplen las condiciones del precio de apertura.
