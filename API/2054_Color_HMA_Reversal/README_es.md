# Estrategia de Reversión Color HMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en cambios de pendiente de la Hull Moving Average. Cierra posiciones contra la nueva dirección y abre posiciones a favor de la tendencia cuando el HMA revierte.

## Parámetros
- `HmaPeriod` — período para la Hull Moving Average.
- `CandleType` — tipo de velas a utilizar.
- `BuyOpen`, `SellOpen` — permitir apertura de posiciones largas/cortas.
- `BuyClose`, `SellClose` — permitir cierre de posiciones largas/cortas.

## Señales
- **Reversión alcista**: el HMA anterior estaba bajando y el valor actual sube → cerrar cortos y abrir un largo.
- **Reversión bajista**: el HMA anterior estaba subiendo y el valor actual baja → cerrar largos y abrir un corto.

La estrategia utiliza órdenes de mercado y opera con el volumen especificado en `Strategy.Volume`.
