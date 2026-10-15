# Estrategia Batman ATR con Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un enfoque de stop trailing basado en ATR inspirado en el Asesor Experto original "Batman".
Rastrea niveles dinámicos de soporte y resistencia derivados del indicador **Average True Range (ATR)** y reacciona cuando el precio cruza estos niveles.

## Lógica

1. Calcular el ATR con el período configurable.
2. Determinar soporte y resistencia:
   - `support = price - ATR * factor`
   - `resistance = price + ATR * factor`
3. Mantener el soporte o resistencia más cercano dependiendo de la tendencia actual.
4. Cuando el precio supera la resistencia, abrir una posición **larga**.
5. Cuando el precio cae por debajo del soporte, abrir una posición **corta**.

El precio puede ser el precio de cierre o el precio típico `(high + low + close) / 3`.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `ATR Period` | Período del indicador ATR. |
| `ATR Factor` | Multiplicador aplicado al valor ATR para construir las líneas de stop. |
| `Use Typical Price` | Si está habilitado, usa `(High + Low + Close)/3` en lugar del precio de cierre. |
| `Candle Type` | Tipo de velas usadas para los cálculos. |

## Notas

- La estrategia usa la API de alto nivel con `SubscribeCandles` y `Bind`.
- `StartProtection()` se llama al inicio para garantizar la seguridad de la posición.
- El trading se realiza solo en velas completadas.
