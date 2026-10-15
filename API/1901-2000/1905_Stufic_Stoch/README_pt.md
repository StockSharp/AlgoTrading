# Estratégia Stufic Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina a detecção de tendência usando duas médias móveis com sinais de momentum do oscilador Estocástico.
Compra quando a média móvel rápida está acima da média móvel lenta e a linha %K do Estocástico cruza acima da linha %D abaixo de um limite de sobrevenda.
Vende quando a média móvel rápida está abaixo da média móvel lenta e %K cruza abaixo de %D acima de um limite de sobrecompra.

## Lógica
- Detecta a tendência do mercado comparando uma média móvel rápida e uma lenta.
- Usa o oscilador Estocástico para encontrar reversões de momentum em níveis extremos.
- Abre uma posição comprada quando a tendência é de alta e o oscilador sai da zona de sobrevenda com um cruzamento de alta.
- Abre uma posição vendida quando a tendência é de baixa e o oscilador sai da zona de sobrecompra com um cruzamento de baixa.
- As posições são fechadas ou revertidas em sinais opostos. Uma porcentagem de stop-loss é aplicada usando a proteção integrada.

## Parâmetros
- **FastMaPeriod** – período da média móvel rápida.
- **SlowMaPeriod** – período da média móvel lenta.
- **StochKPeriod** – período para a linha %K do Estocástico.
- **StochDPeriod** – período de suavização para a linha %D.
- **OverboughtLevel** – limite superior para o oscilador Estocástico.
- **OversoldLevel** – limite inferior para o oscilador Estocástico.
- **StopLossPercent** – distância do stop-loss expressa como porcentagem do preço de entrada.
- **CandleType** – série de velas utilizada para cálculos.

## Indicadores
- Média Móvel Simples (rápida e lenta).
- Oscilador Estocástico.

## Uso
Anexe a estratégia a um instrumento. Configure os parâmetros para corresponder ao período e nível de risco desejados. Inicie a estratégia para começar a negociar. O algoritmo gerencia automaticamente as posições com base nas condições descritas.
