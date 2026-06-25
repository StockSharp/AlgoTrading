# Estrategia de Temporizador de Stop Global
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Temporizador de Stop Global es una capa de gestión de riesgo convertida del experto de MetaTrader `Exp_GStop_Tm`.
Vigila continuamente el valor del portafolio en cada vela terminada y detiene el trading una vez que se alcanza un objetivo de ganancia global
o un límite de pérdida. Además, puede restringir el trading a una ventana de tiempo definida por el usuario y forzar el cierre de todas las posiciones abiertas
siempre que la ventana esté cerrada.

## Cómo funciona

- Cuando la estrategia inicia, registra el saldo inicial del portafolio como punto de referencia.
- Cada vez que la serie de velas suscrita se cierra, la estrategia lee el valor actual del portafolio y calcula la
  diferencia respecto al saldo inicial.
- Dependiendo del `StopCalculationMode` seleccionado, la diferencia se convierte a porcentaje o se deja como moneda.
- Si la pérdida supera `StopLoss` o la ganancia supera `TakeProfit`, la estrategia entra en estado detenido, registra el
  evento y envía órdenes de mercado para cerrar cualquier posición restante.
- Cuando la ventana de trading opcional está habilitada y el tiempo actual sale de la ventana, la estrategia también intenta
  aplanar la posición. Una vez que el tamaño de la posición se convierte en cero, el indicador de stop se reinicia, permitiendo que el trading se reanude dentro
  de la siguiente ventana válida.

La estrategia nunca abre nuevas posiciones por sí sola. Está diseñada para supervisar otras estrategias o trades manuales y para
proteger la cuenta de una caída excesiva o para asegurar las ganancias de toda la cuenta.

## Lógica de la ventana de trading

La ventana de trading replica la lógica original del experto:

- Si la hora de inicio es menor que la hora de fin, el trading se permite entre el minuto de inicio (inclusive) y el minuto de fin (exclusivo) en el mismo día.
- Si la hora de inicio y fin son iguales, el trading se permite solo cuando el minuto actual está entre `StartMinute`
  (inclusive) y `EndMinute` (exclusivo).
- Si la hora de inicio es mayor que la hora de fin, la sesión se extiende pasada la medianoche. El trading se habilita desde el inicio
  hasta la medianoche y se reanuda desde la medianoche hasta el fin al día siguiente.

## Parámetros

- `StopCalculationMode` – elegir entre stops globales basados en porcentaje o en moneda.
- `StopLoss` – umbral de pérdida global. Tratado como porcentaje cuando el modo porcentual está activo, de lo contrario como moneda de la cuenta.
- `TakeProfit` – objetivo de ganancia global. Usa la misma unidad que `StopLoss`.
- `UseTradingWindow` – habilitar o deshabilitar el filtro de sesión.
- `StartHour` / `StartMinute` – hora de inicio de la ventana de trading permitida.
- `EndHour` / `EndMinute` – hora de cierre de la ventana de trading permitida.
- `CandleType` – serie de velas que define con qué frecuencia se evalúa el estado de la cuenta.

## Notas

- Dado que las verificaciones de stop ocurren al cierre de la vela, usar un marco temporal pequeño (por ejemplo, un minuto) cuando se requiere reacción rápida.
- La estrategia cierra solo la posición gestionada por esta instancia de estrategia. Ejecutar instancias separadas si múltiples
  valores necesitan supervisión individual.
- Usar junto a otras estrategias de trading adjuntándola como estrategia padre o ejecutándola en el mismo instrumento para
  proporcionar protección a nivel de cuenta.
