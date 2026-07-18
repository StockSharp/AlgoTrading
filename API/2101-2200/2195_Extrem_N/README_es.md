# Estrategia Extrem N
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Extrem N opera reversiones basadas en nuevos máximos y mínimos detectados en una ventana deslizante.

La estrategia se basa en el indicador de Canal de Donchian para marcar los extremos de precio. Cuando una barra establece un nuevo máximo relativo al período de retrospección y la siguiente barra establece un nuevo mínimo, se abre una posición larga. Se abre una posición corta cuando un nuevo mínimo es seguido por un nuevo máximo. Las señales opuestas cierran las posiciones existentes.

- **Condiciones de entrada**:
  - Largo: la barra anterior estableció un nuevo máximo y la barra actual estableció un nuevo mínimo.
  - Corto: la barra anterior estableció un nuevo mínimo y la barra actual estableció un nuevo máximo.
- **Condiciones de salida**:
  - Las posiciones largas se cierran con una señal de entrada corta.
  - Las posiciones cortas se cierran con una señal de entrada larga.
- **Parámetros**:
  - `Period` – período de retrospección de Donchian (por defecto 9).
  - `CandleType` – marco temporal de procesamiento (por defecto 4 horas).
  - `BuyPosOpen` – permitir abrir posiciones largas (por defecto true).
  - `SellPosOpen` – permitir abrir posiciones cortas (por defecto true).
  - `BuyPosClose` – permitir cerrar posiciones largas (por defecto true).
  - `SellPosClose` – permitir cerrar posiciones cortas (por defecto true).
- **Indicadores**: Canal de Donchian.
