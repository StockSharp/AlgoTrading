# Estrategia Stufic Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina la detección de tendencia usando dos medias móviles con señales de momentum del oscilador Estocástico.
Compra cuando la media móvil rápida está por encima de la media móvil lenta y la línea %K del Estocástico cruza por encima de la línea %D por debajo de un umbral de sobreventa.
Vende cuando la media móvil rápida está por debajo de la media móvil lenta y %K cruza por debajo de %D por encima de un umbral de sobrecompra.

## Lógica
- Detecta la tendencia del mercado comparando una media móvil rápida y una lenta.
- Utiliza el oscilador Estocástico para encontrar reversiones de momentum en niveles extremos.
- Abre una posición larga cuando la tendencia es alcista y el oscilador sale de la zona de sobreventa con un cruce alcista.
- Abre una posición corta cuando la tendencia es bajista y el oscilador sale de la zona de sobrecompra con un cruce bajista.
- Las posiciones se cierran o revierten en señales opuestas. Se aplica un porcentaje de stop-loss mediante la protección integrada.

## Parámetros
- **FastMaPeriod** – período de la media móvil rápida.
- **SlowMaPeriod** – período de la media móvil lenta.
- **StochKPeriod** – período para la línea %K del Estocástico.
- **StochDPeriod** – período de suavizado para la línea %D.
- **OverboughtLevel** – umbral superior para el oscilador Estocástico.
- **OversoldLevel** – umbral inferior para el oscilador Estocástico.
- **StopLossPercent** – distancia del stop-loss expresada como porcentaje del precio de entrada.
- **CandleType** – serie de velas utilizada para los cálculos.

## Indicadores
- Media Móvil Simple (rápida y lenta).
- Oscilador Estocástico.

## Uso
Adjunte la estrategia a un instrumento. Configure los parámetros para adaptarse al marco temporal y nivel de riesgo deseados. Inicie la estrategia para comenzar a operar. El algoritmo gestiona automáticamente las posiciones en base a las condiciones descritas.
