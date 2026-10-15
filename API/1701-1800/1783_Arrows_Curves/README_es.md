# Estrategia Arrows & Curves
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión del asesor experto MQL5 **Exp_Arrows_Curves**.
Construye un canal de precio dinámico utilizando máximos y mínimos recientes y reacciona a
las rupturas. La estrategia puede abrir o cerrar posiciones dependiendo de los
permisos del usuario y la dirección de la tendencia.

## Lógica de la estrategia
- Calcular el máximo más alto y el mínimo más bajo durante el período configurado.
- Expandir el rango en un porcentaje para formar las líneas exteriores del canal.
- Crear líneas de stop interiores usando un porcentaje adicional.
- Cuando el precio rompe por encima del canal superior, ir largo; cuando rompe por debajo
  del canal inferior, ir corto.
- Las líneas de stop interiores activan la salida de la posición cuando se cruza el lado opuesto del
  canal.

## Parámetros
- `SspPeriod` – período de retroceso para máximos y mínimos.
- `Channel` – porcentaje de expansión para las líneas principales del canal.
- `StopChannel` – porcentaje adicional utilizado para las líneas de stop interiores.
- `CandleType` – período temporal de las velas.
- `BuyPosOpen` / `SellPosOpen` – permitir abrir posiciones largas/cortas.
- `BuyPosClose` / `SellPosClose` – permitir cerrar posiciones largas/cortas.

## Indicadores
- Highest
- Lowest

## Notas
La estrategia opera únicamente en velas completadas. La gestión de stop loss y take profit
no está incluida; las salidas se basan en los cruces del canal.
