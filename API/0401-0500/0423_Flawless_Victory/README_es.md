# Estrategia Flawless Victory
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Flawless Victory es un sistema de momentum modular que combina osciladores con Bandas de Bollinger. Dependiendo de la versión seleccionada, puede operar con señales simples de RSI, aplicar objetivos fijos de take-profit y stop-loss, o exigir confirmación del Money Flow Index. El objetivo es explotar el agotamiento en los extremos de las bandas de volatilidad y aprovechar las oscilaciones de reversión a la media.

La versión 1 entra cuando el RSI sale de zonas de sobreventa o sobrecompra cerca de los extremos de Bollinger. La versión 2 añade control explícito del riesgo mediante objetivos basados en porcentajes. La versión 3 requiere que tanto el RSI como el MFI estén de acuerdo, filtrando reversiones débiles.

La estrategia funciona mejor en mercados intradía con límites de volatilidad claros.

## Detalles

- **Criterios de entrada**:
  - **Largo**: ver reglas de versión (RSI <30 cerca de la banda inferior; versión 3 también `MFI < 20`)
  - **Corto**: RSI >70 cerca de la banda superior (versión 3 también `MFI > 80`)
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - **Versión 1**: señal RSI opuesta
  - **Versión 2**: porcentajes de take-profit o stop-loss
  - **Versión 3**: combinación opuesta RSI/MFI
- **Stops**: Opcional en la versión 2
- **Valores predeterminados**:
  - `RSI_length` = 14
  - `MFI_length` = 14
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `TakeProfitPct` = 1.5
  - `StopLossPct` = 1.0
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, MFI, Bollinger Bands
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
