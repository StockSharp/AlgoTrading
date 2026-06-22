# Estrategia Lossless MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruzamientos entre una Media Móvil Simple (SMA) rápida y una lenta.
Opcionalmente evita realizar pérdidas moviendo las posiciones perdedoras al punto de equilibrio cuando aparece la señal opuesta.

## Cómo Funciona

1. **Indicadores**
   - SMA rápida
   - SMA lenta
2. **Entradas**
   - **Largo** cuando `SMA rápida > SMA lenta` y la dirección actual no es larga.
   - **Corto** cuando `SMA rápida < SMA lenta` y la dirección actual no es corta.
   - Se permiten entradas adicionales si `Close Losses` está deshabilitado y el número de operaciones abiertas está por debajo de `Max Deals`.
3. **Salidas**
   - En un cruzamiento opuesto.
   - Si `Close Losses` está habilitado, la posición se cierra inmediatamente.
   - Si `Close Losses` está deshabilitado y la operación está en pérdida, se coloca una orden limitada al precio de entrada para salir en el punto de equilibrio.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ------ | ----------- | -------------- |
| `FastLength` | Período de la SMA rápida. | `10` |
| `SlowLength` | Período de la SMA lenta. | `30` |
| `MaxDeals` | Número máximo de operaciones simultáneas. | `5` |
| `CloseLosses` | Cerrar operaciones con pérdidas inmediatamente. | `true` |
| `Volume` | Volumen de la orden. | `1` |
| `CandleType` | Velas para los cálculos. | `1-minute` |

## Notas

La estrategia utiliza órdenes de mercado para entradas y salidas. Cuando `CloseLosses` está deshabilitado, intenta proteger las posiciones colocando una orden limitada al precio de entrada en lugar de cerrar con pérdida.
