# Estrategia de Vela XDPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del experto MQL5 original **Exp_XDPOCandle**. Construye velas sintéticas aplicando dos medias móviles exponenciales consecutivas a los precios de apertura y cierre. El color de la vela resultante (alcista, bajista o neutral) impulsa las decisiones de trading.

## Lógica de la estrategia

1. Cada vela de mercado entrante se suaviza dos veces:
   - El primer suavizado utiliza una EMA de longitud `FastLength`.
   - El segundo suavizado aplica otra EMA de longitud `SlowLength` al resultado del primero.
2. Si el cierre suavizado está por encima de la apertura suavizada, la vela se considera *alcista*.
3. Si el cierre suavizado está por debajo de la apertura suavizada, la vela se considera *bajista*.
4. La estrategia abre una posición larga cuando aparece una vela alcista tras una no alcista. Abre una posición corta cuando aparece una vela bajista tras una no bajista.
5. Las posiciones opuestas existentes se cierran automáticamente revirtiendo mediante órdenes de mercado.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `FastLength` | Longitud de la primera EMA aplicada a los precios. |
| `SlowLength` | Longitud de la segunda EMA aplicada al resultado de la primera EMA. |
| `CandleType` | El marco temporal y tipo de velas utilizadas para el cálculo. |

## Uso

1. Adjunte la estrategia a un instrumento dentro del entorno StockSharp.
2. Configure los parámetros si es necesario. Los valores predeterminados están ajustados para coincidir con la configuración del experto original.
3. Inicie la estrategia. Se suscribirá al tipo de vela especificado y operará en los cambios de color de las velas suavizadas.

## Notas

- La gestión de riesgos se maneja mediante `StartProtection()` con configuración predeterminada. Ajuste `Volume` y los parámetros de protección externamente según sea necesario.
- Este repositorio actualmente solo contiene la versión en C#; el port en Python no está disponible.
