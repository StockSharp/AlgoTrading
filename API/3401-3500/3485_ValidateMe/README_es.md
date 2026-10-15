# Estrategia Validarme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia ValidateMe traslada el marco de validación básico del asesor experto MQL4 original. La lógica se centra en verificar la disponibilidad de fondos, verificar que las distancias para detener las pérdidas y obtener ganancias respeten las restricciones cambiarias y luego disparar una orden de mercado única en la dirección elegida. La estrategia monitorea continuamente los eventos de ejecución comercial y abre una nueva posición solo cuando no hay posiciones u órdenes activas presentes.

## Lógica de trading

1. La estrategia se suscribe a los datos de ticks de la seguridad configurada.
2. Cuando la estrategia está en línea, formada y se permite el comercio, verifica que no haya posiciones abiertas ni órdenes activas presentes.
3. Luego envía una orden de mercado en la dirección configurada (compra o venta) utilizando el tamaño de lote definido.
4. Un módulo de protección adjunta inmediatamente órdenes de toma de ganancias y de limitación de pérdidas calculadas a partir de distancias de pips, lo que garantiza el cumplimiento de los niveles de parada del corredor (ajustados por precios fraccionados).
5. Una vez que se cierra la posición, la estrategia espera el siguiente tick y repite la validación antes de enviar una nueva orden.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| **Obtener ganancias (pips)** | Distancia del precio de entrada al take-profit en pips. Debe ser mayor que cero. |
| **Detener pérdidas (pips)** | Distancia desde el precio de entrada hasta el stop-loss en pips. Debe ser mayor que cero. |
| **Muchos** | Volumen comercial en lotes utilizados para cada orden de mercado. |
| **Dirección** | Dirección de la orden de mercado (Compra o Venta). |

## Gestión del riesgo

* La estrategia utiliza `StartProtection` con compensaciones absolutas para registrar órdenes de obtención de beneficios y de limitación de pérdidas.
* El tamaño del pip se calcula a partir del paso del precio del valor y la precisión decimal para imitar el comportamiento de MetaTrader (los símbolos de 5 y 3 dígitos utilizan un tamaño de diez puntos).
* La estrategia activa nuevas órdenes solo si no hay órdenes existentes activas, evitando la acumulación de órdenes.

## Notas de uso

* Adjunte la estrategia a un valor y establezca el volumen y la dirección deseados.
* Configure las distancias de toma de ganancias y límite de pérdidas en pips de acuerdo con los requisitos del corredor.
* La estrategia no se basa en indicadores y pretende ser un marco de validación más que un sistema comercial completo.
* El control del riesgo de la cartera (por ejemplo, reducción máxima) se puede combinar externamente si es necesario.
