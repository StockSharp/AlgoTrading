# Estrategia Sistema Abierto de Pivotes de Zona Horaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión al API de alto nivel de StockSharp del experto de MetaTrader `Exp_TimeZonePivotsOpenSystem`. Reproduce la lógica original que ancla un canal de precio simétrico al precio de apertura diario en una hora configurable y reacciona cuando las velas completadas rompen por encima o por debajo de esa banda. Todas las órdenes se envían como órdenes de mercado y la protección opcional de stop-loss / take-profit se configura a través de `StartProtection`.

## Cómo funciona

1. Se suscribe al marco temporal de velas configurado, registra el paso de precio del instrumento y configura stops protectores si las distancias son mayores que cero.
2. Rastrea la primera vela de cada día cuyo tiempo de apertura coincide con `StartHour`. El precio de apertura de esa vela se convierte en el ancla de la sesión y define las bandas superior e inferior a `OffsetPoints` pasos de precio por encima y por debajo del ancla.
3. Calcula una señal de cinco estados para cada vela finalizada, imitando el buffer codificado por colores del indicador personalizado original:
   - `0` / `1`: la vela cerró por encima de la banda superior (ruptura alcista, con el índice reflejando la dirección de la vela).
   - `2`: la vela terminó dentro de la banda (neutral).
   - `3` / `4`: la vela cerró por debajo de la banda inferior (ruptura bajista).
4. Mantiene un historial deslizante de señales. La vela ubicada `SignalBar` pasos atrás sirve como barra de confirmación y la vela inmediatamente antes debe ser neutral para desencadenar una entrada, recreando la lógica de MetaTrader que espera una barra después del breakout.
5. Cuando aparece una confirmación alcista, la estrategia opcionalmente cierra posiciones cortas y, si está plana y se permite, abre una nueva posición larga. Las confirmaciones bajistas se comportan simétricamente para las operaciones cortas.
6. Después de abrir una nueva posición, la estrategia pospone nuevas entradas en la misma dirección hasta que comience la siguiente vela después de la barra de confirmación, evitando órdenes duplicadas en la misma sesión.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `CandleType` | Marco temporal de velas para los cálculos de ruptura. | `H1` |
| `OrderVolume` | Volumen usado para nuevas posiciones. | `0.1` |
| `StartHour` | Hora (0-23) cuyo precio de apertura ancla las bandas diarias. | `0` |
| `OffsetPoints` | Semiancho de la banda en pasos de precio (unidades de tick). | `100` |
| `SignalBar` | Número de velas cerradas entre la barra actual y la confirmación del breakout. Debe ser ≥ 1 en esta conversión. | `1` |
| `StopLossPoints` | Distancia del stop protector en pasos de precio. | `1000` |
| `TakeProfitPoints` | Distancia del objetivo de beneficio en pasos de precio. | `2000` |
| `EnableLongEntry` | Permitir abrir posiciones largas después de señales alcistas. | `true` |
| `EnableShortEntry` | Permitir abrir posiciones cortas después de señales bajistas. | `true` |
| `CloseLongOnBearishBreak` | Cerrar posiciones largas existentes en confirmaciones bajistas. | `true` |
| `CloseShortOnBullishBreak` | Cerrar posiciones cortas existentes en confirmaciones alcistas. | `true` |

## Notas

- El bloque de gestión de dinero de la versión de MetaTrader se reemplaza por el parámetro explícito `OrderVolume` típico de las estrategias StockSharp.
- Los parámetros de stop-loss y take-profit se convierten de distancias en puntos a desplazamientos de precio absolutos usando el paso de precio actual del instrumento.
- La implementación S# mantiene solo una posición neta (larga, corta o plana) exactamente como el original MQL, y omitirá nuevas entradas mientras haya una posición abierta.
