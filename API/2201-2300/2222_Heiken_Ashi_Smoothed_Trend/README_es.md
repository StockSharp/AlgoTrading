# Estrategia de Tendencia Heiken Ashi Suavizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza velas Heiken-Ashi suavizadas con EMA para detectar reversiones de tendencia. Una vela alcista que cambia de rojo a verde abre una posición larga y cierra cualquier corta. Por el contrario, una vela bajista que cambia de verde a rojo abre una posición corta y cierra cualquier larga.

- **Indicadores**: Heikin-Ashi (con suavizado EMA)
- **Reglas de entrada**:
  - Entrar largo cuando la vela Heikin-Ashi suavizada se vuelve alcista.
  - Entrar corto cuando la vela suavizada se vuelve bajista.
- **Reglas de salida**:
  - Revertir la posición en señal opuesta.
- **Parámetros**:
  - `EmaLength` – período de suavizado de la EMA.
  - `CandleType` – marco temporal de las velas.

El algoritmo recalcula la apertura y el cierre suavizados para cada vela terminada y cambia la posición en consecuencia.
