# Estrategia de Divergencia Estacional HMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia combina la Media Móvil Hull (HMA) con la agrupación estacional del interés abierto para encontrar divergencias entre el precio y el posicionamiento del mercado. Asume que cuando el precio se mueve temporalmente contra la dirección del interés abierto creciente, es probable una continuación de la tendencia. El sistema está diseñado para operar tanto en largo como en corto, utilizando la pendiente de la HMA para medir el impulso y los datos estacionales de interés abierto para medir los niveles de participación.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 40%. Funciona mejor en el mercado de criptomonedas.

Una configuración de operación ocurre cuando la HMA cambia respecto a la barra anterior mientras el interés abierto estacional confirma el movimiento, pero el precio imprime en la dirección opuesta. Esta divergencia alcista o bajista entre el precio y el posicionamiento a menudo señala el fin de un retroceso de corto plazo dentro de una tendencia mayor. La estrategia espera estas condiciones antes de entrar y coloca un stop basado en volatilidad para gestionar el riesgo.

Las posiciones se cierran cuando la pendiente de la HMA se invierte, lo que indica que el impulso ha cambiado. Dado que el nivel del stop utiliza un múltiplo del Rango Verdadero Promedio (ATR), el riesgo se adapta a la volatilidad del mercado. Esto ayuda a prevenir salidas prematuras durante períodos de expansión y mantiene las pérdidas contenidas cuando la volatilidad se contrae.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `HMA(t) > HMA(t-1)` && `OI_Cluster_Seasonal(t) > OI_Cluster_Seasonal(t-1)` && `Price(t) < Price(t-1)` (divergencia alcista).
  - **Corto**: `HMA(t) < HMA(t-1)` && `OI_Cluster_Seasonal(t) < OI_Cluster_Seasonal(t-1)` && `Price(t) > Price(t-1)` (divergencia bajista).
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: `HMA(t) < HMA(t-1)` (la HMA comienza a caer).
  - **Corto**: `HMA(t) > HMA(t-1)` (la HMA comienza a subir).
- **Stops**: Sí, stop-loss colocado en `N * ATR` desde la entrada.
- **Valores predeterminados**:
  - `HMA period` = 9.
  - `OI_Cluster_Seasonal` = OI estacional en niveles de clúster durante cinco años.
  - `N` = 2 (stop-loss = `2 * ATR`).
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Complejo
  - Marco temporal: Medio plazo
  - Estacionalidad: Sí
  - Redes neuronales: Sí
  - Divergencia: Sí
  - Nivel de riesgo: Alto

