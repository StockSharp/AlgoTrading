# Estrategia de cruce de divisas simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Puerto de alto nivel del asesor experto MetaTrader 4 *simplefx2.mq4* (Simple FX 2.0).
- Intercambia cruces entre una media móvil simple rápida y una lenta en velas terminadas.
- Mantiene solo una posición abierta y cambia cuando se invierte la tendencia dominante.

## Lógica de trading
1. Cree velas utilizando el parámetro de marco de tiempo configurable.
2. Calcule dos promedios móviles simples (rápido y lento) sobre los precios de cierre de velas.
3. Confirme una tendencia alcista cuando tanto la vela actual como la anterior muestren la MA rápida por encima de la MA lenta. Confirme una tendencia bajista cuando ambas velas muestren la MA rápida por debajo de la MA lenta.
4. Cuando la tendencia confirmada difiere del estado de tendencia almacenado, cierre cualquier posición opuesta y abra inmediatamente una orden de mercado en la nueva dirección utilizando el volumen configurado.
5. Se pueden habilitar protecciones opcionales de stop-loss y take-profit expresadas en incrementos de precios. Utilizan el servicio de protección integrado de StockSharp para emular la configuración de riesgo MT4.

La estrategia solo procesa velas terminadas, nunca ticks intrabar, para mantenerse cerca del comportamiento original del asesor experto. Se proporciona un registro de cada nueva entrada para que cada decisión cruzada pueda ser auditada.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimización |
| --- | --- | --- | --- |
| `ShortPeriod` | Longitud de la media móvil simple rápida. | 50 | 10 → 150 paso 5 |
| `LongPeriod` | Longitud de la media móvil simple lenta. | 200 | 50 → 400 paso 10 |
| `Volume` | Volumen de órdenes enviadas con cada operación de mercado. | 0.1 | 0,1 → 2 paso 0,1 |
| `StopLossPoints` | Distancia de parada protectora en pasos de precio del instrumento (0 desactiva). | 0 | — |
| `TakeProfitPoints` | Distancia objetivo de ganancias en pasos de precio del instrumento (0 desactiva). | 0 | — |
| `CandleType` | Plazo de vela utilizado para el análisis. | 1 hora | — |

## Notas y diferencias con la versión MT4
- El archivo de persistencia MT4 (`simplefx.dat`) no es necesario; la última dirección de la tendencia es rastreada en la memoria por el estado de la estrategia.
- Las opciones de deslizamiento, comentario de orden, número mágico y color de flecha del asesor experto original no están expuestas porque StockSharp maneja el enrutamiento de manera diferente.
- Las distancias de stop-loss y take-profit se interpretan en **escalones de precio** (tics del instrumento). Ajústelos para que coincidan con la definición de pips de su corredor.
- Sólo puede haber una posición abierta en cualquier momento; la estrategia se basa en `ClosePosition()` antes de cambiar de dirección, lo que garantiza un cambio limpio entre operaciones largas y cortas.

## Uso
1. Adjunte la estrategia a un valor/instrumento y establezca el período de tiempo de vela deseado.
2. Configure períodos de media móvil y parámetros de riesgo.
3. Iniciar la estrategia; se suscribirá a velas, gestionará el estado de la tendencia y enviará órdenes de mercado cuando se confirme un cruce en dos velas consecutivas.
