# Estrategia de cruce MA rápido y lento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Fast Slow MA Crossover** reproduce el comportamiento del MetaTrader 4 asesor experto original `_HPCS_FastSlowMACrosssover_MT4_EA_V01_WE`. La estrategia observa dos promedios móviles exponenciales (EMA) calculados en la serie de velas seleccionadas y emite operaciones cuando el promedio rápido cruza el lento dentro de una ventana de negociación intradiaria configurable. Las salidas protectoras de toma de ganancias y stop-loss se expresan en pips, por lo que el comportamiento coincide con la implementación MQL que se basa en los dígitos del corredor para escalar los precios.

## Lógica de trading

1. Suscríbase al tipo de vela configurado (predeterminado: velas de 1 minuto).
2. Calcule dos EMA:
   - Período EMA rápida (predeterminado **14**).
   - Período EMA lenta (predeterminado **21**).
3. Evalúe cada vela terminada:
   - Verifique que el tiempo de cierre de la vela caiga dentro de la ventana de negociación permitida.
   - Detecta un **cruce alcista** cuando el EMA rápido cruza por encima del EMA lento.
   - Detecta un **cruce bajista** cuando el EMA rápido cruza por debajo del EMA lento.
4. Ejecutar órdenes:
   - Cierre la exposición opuesta si hay una posición inversa abierta.
   - Ingrese una orden de mercado con el volumen configurado (parámetro **Volumen comercial**).
   - Guarde el precio de cierre de la vela como ancla de entrada para los cálculos de riesgo.
5. Administre posiciones abiertas utilizando máximos y mínimos de velas:
   - Cierre una posición larga si el precio se mueve **Stop Loss (pips)** por debajo de la entrada.
   - Cierre una posición larga si el precio sube **Take Profit (pips)** por encima de la entrada.
   - Aplique la lógica simétrica para posiciones cortas (parada por encima de la entrada, objetivo por debajo de la entrada).

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Período MA rápido** | Longitud de la EMA rápida utilizado para la detección de cruce. |
| **Período MA lento** | Duración del EMA lenta. |
| **Obtener ganancias (pips)** | Distancia, en pips, utilizada para calcular los objetivos de ganancias a corto y largo plazo. |
| **Detener pérdidas (pips)** | Distancia, en pips, utilizada para calcular los precios de parada de protección. |
| **Hora de inicio** | Inicio de la ventana de negociación diaria (inclusive). |
| **Tiempo de parada** | Fin de la ventana de negociación diaria (inclusive). |
| **Tipo de vela** | Serie de velas utilizadas para alimentar los indicadores. |
| **Volumen comercial** | Volumen de órdenes de mercado para cada señal. |

## Notas

- El tamaño del pip se deriva del paso del precio del valor y la precisión decimal. Cuando el instrumento utiliza 5 o 3 dígitos decimales, la estrategia multiplica el paso del precio por **10** para que coincida con el cálculo de MetaTrader pip.
- El filtro de tiempo admite sesiones nocturnas. Cuando la **Hora de inicio** es posterior a la **Hora de finalización**, las operaciones permanecen activas hasta la medianoche y se reanudan desde la medianoche hasta la hora de finalización.
- Solo se permite una señal por vela, lo que garantiza que el comportamiento coincida con el EA original que protegía contra múltiples envíos por barra.
- Las órdenes de salida protectoras se ejecutan mediante la lógica estratégica en lugar de las órdenes en reposo. Esto refleja el enfoque EA donde los niveles de límite de pérdidas y toma de ganancias se definían en el momento del envío de la orden.
