# Laguerre CCI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el filtro Laguerre, el Índice de Canal de Materias Primas (CCI) y una media móvil exponencial.

## Descripción general
- El filtro Laguerre resalta los extremos de sobrecompra y sobreventa en una escala de 0-1.
- El CCI confirma el momentum en la misma dirección.
- La pendiente de la EMA filtra las operaciones según la tendencia predominante.

## Reglas de entrada
- **Largo** cuando el valor de Laguerre es 0, la EMA sube y el CCI está por debajo del umbral negativo `CciLevel`.
- **Corto** cuando el valor de Laguerre es 1, la EMA baja y el CCI está por encima del umbral positivo `CciLevel`.

## Reglas de salida
- Cerrar posiciones largas cuando Laguerre supera 0.9.
- Cerrar posiciones cortas cuando Laguerre cae por debajo de 0.1.

## Parámetros
- `LagGamma` – valor gamma para el filtro Laguerre.
- `CciPeriod` – período para el cálculo del CCI.
- `CciLevel` – nivel absoluto del CCI usado para entradas.
- `MaPeriod` – período para la media móvil.
- `TakeProfit` – take profit en unidades de precio absolutas (opcional).
- `StopLoss` – stop loss en unidades de precio absolutas (opcional).
- `CandleType` – tipo de vela usado para los indicadores.

La estrategia procesa solo velas finalizadas y usa los vínculos de API de alto nivel de StockSharp para los indicadores.
