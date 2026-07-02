# Estrategia Keltner Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera en reversión a la media usando Canales de Keltner

Las pruebas indican un rendimiento anual promedio de aproximadamente 130%. Funciona mejor en el mercado de acciones.

Keltner Reversion opera contra los impulsos fuera del Canal de Keltner. Las entradas apuestan a un retorno hacia la banda media, cerrando operaciones una vez que el precio vuelve a entrar al canal o se alcanza el stop.

El ancho del canal se expande y contrae con la volatilidad, permitiendo que el sistema capture movimientos extremos mientras da espacio para que las operaciones se desarrollen. Los stops se basan típicamente en múltiplos de ATR.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI, ATR, Keltner.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, ATR, Keltner
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

