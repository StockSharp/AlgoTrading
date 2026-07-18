# Estrategia de Seguimiento de Tendencia con Parabolic Compra Venta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina Parabolic SAR con cruces de medias móviles.
Las entradas largas ocurren cuando el precio está por encima de una SMA de tendencia larga, la EMA rápida cruza por encima de la EMA lenta y el SAR es alcista.
Las entradas cortas usan las condiciones opuestas.
El stop loss se coloca en la SMA de tendencia y el take profit usa una ratio riesgo/recompensa.

## Detalles

- **Entrada**:
  - **Largo**: precio > SMA de tendencia, EMA rápida cruza por encima de EMA lenta, SAR alcista
  - **Corto**: precio < SMA de tendencia, EMA rápida cruza por debajo de EMA lenta, SAR bajista
- **Salida**:
  - stop en SMA de tendencia
  - take profit = riesgo/recompensa * distancia desde entrada hasta SMA de tendencia
- **Indicadores**: Parabolic SAR, SMA, EMA
- **Marco temporal**: configurable
- **Tipo**: Seguimiento de tendencia
