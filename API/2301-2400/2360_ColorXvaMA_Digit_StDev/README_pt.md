# Estratégia ColorXvaMA Digit StDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia negocia com base em quão longe o preço se desvia de uma média móvel exponencial (EMA). Dois multiplicadores de desvio (K1 e K2) definem bandas internas e externas calculadas a partir do desvio padrão do preço.

Quando o preço sobe acima da EMA por K2 desvios padrão, a estratégia entra em uma posição comprada. Quando o preço cai abaixo da EMA por K2 desvios padrão, ela entra em uma posição vendida. As posições existentes são fechadas quando o desvio retorna dentro da banda interna definida por K1.

## Parâmetros
- **EMA Length** – período da média móvel exponencial.
- **StdDev Length** – período para o cálculo do desvio padrão.
- **Deviation K1** – multiplicador da banda interna usado para fechar posições.
- **Deviation K2** – multiplicador da banda externa usado para abrir posições.
- **Candle Type** – período das velas.

## Indicadores
- Exponential Moving Average
- StandardDeviation

## Como funciona
1. Subscrever velas do período escolhido.
2. Calcular EMA e desvio padrão do preço.
3. Calcular o desvio do preço em relação à EMA.
4. Entrar comprado/vendido quando o desvio supera ±K2×StdDev.
5. Sair quando o desvio retorna dentro de ±K1×StdDev.

Esta abordagem busca capturar fortes desvios da média e sair na reversão.
