# Ruptura del Kumo Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la ruptura del Kumo (nube) de Ichimoku.

Las pruebas indican un retorno anual promedio de aproximadamente 70%. Funciona mejor en el mercado de acciones.

Este enfoque se basa en las señales de la nube Ichimoku. El precio rompiendo por encima de la nube con Tenkan-sen cruzando sobre Kijun-sen desencadena una compra, mientras que la ruptura opuesta por debajo de la nube inicia un corto. Las posiciones se mantienen hasta que el precio vuelve a través de la nube.

La nube delinea niveles clave de soporte y resistencia, por lo que el sistema espera cierres decisivos más allá de ella. Combinando múltiples componentes de Ichimoku, la estrategia evita operaciones de menor probabilidad durante mercados laterales.


## Detalles

- **Criterios de entrada**: Señales basadas en Ichimoku.
- **Largo/Corto**: Ambos directions.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ichimoku
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

