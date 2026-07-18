# Estrategia de Umbral RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convierte el experto *Exp_RSI* de MetaTrader a StockSharp. La estrategia abre y cierra posiciones cuando el Índice de Fuerza Relativa (RSI) cruza niveles predefinidos de sobrecompra y sobreventa.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RSI cruza por encima de `RSI Low Level`.
  - **Corto**: RSI cruza por debajo de `RSI High Level`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Señal inversa o parámetros de stop.
- **Stops**: Take Profit y Stop Loss en unidades de precio absolutas.
- **Valores predeterminados**:
  - `RSI Period` = 14
  - `RSI High Level` = 60
  - `RSI Low Level` = 40
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
