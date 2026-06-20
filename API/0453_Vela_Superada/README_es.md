# Estrategia Vela Superada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Vela Superada opera un patrón de reversión de dos velas. Una configuración
alcista ocurre cuando una vela bajista es seguida inmediatamente por una vela alcista que
cierra por encima de la apertura anterior. Las operaciones se filtran con una EMA a corto
plazo, RSI y tendencia MACD para evitar señales contra la tendencia. Se pueden habilitar
tanto el lado largo como el corto.

La estrategia emplea niveles de take profit y stop loss basados en porcentajes y ajusta
dinámicamente un trailing stop una vez que el precio se mueve favorablemente. Esto permite
capturar movimientos extendidos mientras se protege contra reversiones.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Vela anterior bajista, vela actual alcista, cierre y cierre anterior por encima de la EMA, RSI < 65, MACD subiendo.
  - **Corto**: Vela anterior alcista, vela actual bajista, cierre y cierre anterior por debajo de la EMA, RSI > 35, MACD bajando.
- **Largo/Corto**: Configurable (largo por defecto).
- **Criterios de salida**:
  - Trailing stop o señal opuesta.
- **Stops**: Stop loss y take profit basados en porcentaje.
- **Valores predeterminados**:
  - `EmaLength` = 10
  - `RsiLength` = 14
  - `ShowLong` = True
  - `ShowShort` = False
  - `TpPercent` = 1.2
  - `SlPercent` = 1.8
- **Filtros**:
  - Categoría: Patrón + indicadores
  - Dirección: Ambos
  - Indicadores: EMA, RSI, MACD
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
