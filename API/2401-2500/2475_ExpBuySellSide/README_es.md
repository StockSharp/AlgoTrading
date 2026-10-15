# Estrategia ExpBuySellSide
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el Expert Advisor de MetaTrader **ExpBuySellSide** a la API de StockSharp. Combina un sistema de stops basado en ATR con un filtro de tendencia simplificado Step Up/Down.

El módulo ATR calcula niveles de stop dinámicos alrededor de cada vela. Cuando el precio rompe por encima de la banda superior, el mercado se considera en fase alcista; romper por debajo de la banda inferior indica una fase bajista.

El módulo Step Up/Down compara una SMA muy rápida con una SMA más lenta y comprueba si el diferencial entre ellas se está ampliando. Un diferencial creciente en la dirección del cruce confirma la tendencia.

Una operación se abre solo cuando **ambos** módulos apuntan en la misma dirección. Las posiciones existentes pueden cerrarse opcionalmente cuando aparece una señal contraria.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cierra por encima de la banda superior ATR **y** la SMA rápida se aleja de la SMA lenta hacia arriba.
  - **Corto**: el precio cierra por debajo de la banda inferior ATR **y** la SMA rápida se aleja de la SMA lenta hacia abajo.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Aparece una señal contraria y la opción *Close Opposite* está habilitada.
  - Stop manual mediante protección de posición.
- **Stops**: Basados en bandas `ATR * Multiplier`.
- **Valores predeterminados**:
  - `ATR Period` = 5.
  - `ATR Multiplier` = 2.5.
  - `Fast SMA` = 2.
  - `Slow SMA` = 30.
  - `Candle Type` = marco temporal de 1 hora.
  - `Close Opposite` = true.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

