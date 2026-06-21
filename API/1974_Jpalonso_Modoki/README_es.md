# Estrategia Jpalonso Modoki
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Jpalonso Modoki opera un canal de precio construido a partir de una media móvil simple.
Las envolventes superior e inferior se calculan aplicando una desviación porcentual a la media móvil.
El sistema va largo cuando el precio toca la banda inferior o cuando permanece en la mitad superior del canal.
Va corto en las situaciones opuestas. Take-profit y stop-loss fijos protegen la posición.

## Detalles

- **Criterios de entrada**: Precio por debajo de la envolvente inferior o entre la línea media y la banda superior para largos; precio por encima de la envolvente superior o entre la línea media y la banda inferior para cortos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o niveles de stop.
- **Stops**: Sí, take-profit y stop-loss en puntos.
- **Valores predeterminados**:
  - `CandleType` = 1 minuto
  - `SmaPeriod` = 200
  - `Deviation` = 0.35%
  - `TakeProfit` = 127 puntos
  - `StopLoss` = 77 puntos
- **Filtros**:
  - Categoría: Canal
  - Dirección: Ambos
  - Indicadores: SMA, Envelopes
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
