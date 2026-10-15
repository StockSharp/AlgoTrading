# OzymandiaTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Ozymandias. El indicador combina ATR con medias móviles de máximos y mínimos para construir un canal dinámico. Cuando la dirección cambia de bajista a alcista, la estrategia compra y cierra posiciones cortas. Un cambio a bajista vende y cierra posiciones largas. Los parámetros opcionales de take profit y stop loss gestionan el riesgo.

## Detalles

- **Criterios de entrada**: Cambio de dirección del indicador Ozymandias.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stops configurados.
- **Stops**: Take profit y stop loss.
- **Valores predeterminados**:
  - `Length` = 2
  - `CandleType` = TimeSpan.FromHours(4)
  - `TakeProfitPoints` = 2000
  - `StopLossPoints` = 1000
  - `BuyEntry` = true
  - `SellEntry` = true
  - `BuyExit` = true
  - `SellExit` = true
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Ozymandias (ATR + MA)
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
