# Estrategia ProffessorV3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

Esta estrategia es una conversión completa del experto de MetaTrader *ProffessorV3* a la
API de alto nivel de StockSharp. Mantiene el concepto original de combinar el filtrado de
régimen ADX con una cuadrícula de órdenes de protección y promediado.

- **Indicador**: Average Directional Index (ADX) de 14 períodos con valores +DI/-DI.
- **Modos**: régimen plano (ADX por debajo del umbral) y régimen de tendencia (ADX por encima del umbral).
- **Órdenes**: abre una posición de mercado y rodea el precio con órdenes pendientes
  para cubrir, piramidear o revertir a la media.
- **Salida**: cierra todas las posiciones y órdenes pendientes cuando se alcanza el nivel de
  ganancia o pérdida configurado.
- **Horario**: opera solo dentro del rango de horas seleccionado.

## Lógica de Trading

### Detección de régimen
1. Suscribirse al tipo de vela configurado y calcular los valores ADX.
2. Retrasar la señal ADX el número configurado de velas cerradas (`BarOffset`)
   para replicar el uso original de `CopyBuffer(handle, shift)`.
3. Cuando no hay posición abierta, evaluar los últimos valores ADX retrasados:
   - *Plano alcista*: `ADX < AdxFlatLevel` y `+DI > -DI`.
   - *Plano bajista*: `ADX < AdxFlatLevel` y `+DI < -DI`.
   - *Tendencia alcista*: `ADX ≥ AdxFlatLevel` y `+DI > -DI`.
   - *Tendencia bajista*: `ADX ≥ AdxFlatLevel` y `+DI < -DI`.

### Colocación de órdenes
Para cada modo, la estrategia abre una posición de mercado con el volumen base y
luego coloca una cuadrícula simétrica alrededor del precio actual. Las distancias de la
cuadrícula se expresan en "puntos" exactamente como en el código MQL y se escalan
automáticamente por el paso de precio del instrumento.

- **Plano alcista**: entrada larga al mercado, sell-stop de protección bajo el bid, buy limits
  bajo el ask y sell limits sobre el bid para capturar oscilaciones.
- **Plano bajista**: entrada corta al mercado, buy-stop de protección sobre el ask, buy limits
  en retrocesos y sell limits más altos para recargar cortos.
- **Tendencia alcista**: entrada larga al mercado, sell-stops para cobertura y buy-stops
  para piramidar en ruptura.
- **Tendencia bajista**: entrada corta al mercado, sell-stops para seguir la tendencia y
  buy-stops para limitar reversiones.

El espaciado de la cuadrícula se calcula con la misma fórmula que el original: cada nivel
añade `GridStep + GridDeltaIncrement * level / 2`. El volumen para cada orden pendiente
se ajusta con `LotMultiplier` y `LotAddition`, luego se normaliza al paso de volumen del
exchange y sus límites.

### Gestión de salida
- El beneficio no realizado se calcula desde el precio promedio de la posición estratégica
  y el cierre de la última vela.
- Si el beneficio supera `ProfitTarget` o cae por debajo de `LossLimit` (cuando este
  último es distinto de cero), la estrategia cierra la posición neta y cancela todas las
  órdenes pendientes.
- El trading se omite fuera del intervalo `[StartHour, EndHour)`, coincidiendo con el
  asistente `Time()` original.

## Notas de Implementación

- Los precios bid/ask para órdenes pendientes se aproximan desde el cierre del último
  candle más/menos la mitad del paso de precio. Esto refleja la lógica basada en ticks
  en un entorno impulsado por velas.
- Los valores de punto se escalan por el paso de precio del símbolo y se ajustan para
  cotizaciones de tres y cinco dígitos exactamente como la variable MQL `m_adjusted_point`.
- La normalización de volumen y precio respeta el paso, mínimo y máximo del símbolo
  antes de enviar cualquier orden.
- La estrategia procesa solo velas terminadas para evitar señales prematuras.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen base de la orden de mercado. |
| `LotMultiplier` | Multiplicador aplicado al volumen de cada orden pendiente. |
| `LotAddition` | Volumen adicional añadido a las órdenes pendientes después del multiplicador. |
| `MaxLevels` | Número máximo de niveles de cuadrícula por lado. |
| `GridDeltaIncrement` | Incremento añadido al espaciado de la cuadrícula a medida que los niveles se profundizan (puntos). |
| `GridInitialOffset` | Distancia a la primera orden de protección (puntos). |
| `GridStep` | Distancia base entre niveles consecutivos (puntos). |
| `ProfitTarget` | Nivel de beneficio no realizado que activa el cierre de todo. |
| `LossLimit` | Nivel de pérdida no realizada que activa el cierre de todo (0 deshabilita). |
| `AdxFlatLevel` | Umbral ADX que separa los regímenes plano y de tendencia. |
| `BarOffset` | Número de velas cerradas usadas para retrasar los valores ADX. |
| `StartHour` | Hora en que se abre la ventana de trading (UTC). |
| `EndHour` | Hora en que se cierra la ventana de trading (UTC). |
| `CandleType` | Serie de velas usada para los cálculos. |
