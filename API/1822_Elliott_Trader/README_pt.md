# Estratégia Elliott Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia que abre posições em camadas quando o oscilador Estocástico atinge valores extremos em velas de quatro horas. Coloca uma ordem a mercado inicial seguida de uma grade de ordens limitadas. As posições são fechadas assim que uma meta de lucro é atingida e a tendência é confirmada por médias móveis e Bandas de Bollinger.

## Regras de entrada
- Usar o oscilador Estocástico (comprimento %K 21, suavização 3) em velas H4.
- Quando %K ≥ nível de **Sobrecompra**:
  - Vender a mercado.
  - Colocar até oito ordens `SellLimit` adicionais acima do preço atual em distâncias de pips configuradas.
- Quando %K ≤ nível de **Sobrevenda**:
  - Comprar a mercado.
  - Colocar até oito ordens `BuyLimit` adicionais abaixo do preço atual em distâncias de pips configuradas.

## Regras de saída
- O lucro realizado atinge **ProfitTarget** e o preço confirma a tendência:
  - Posições compradas saem quando o preço está acima da Banda de Bollinger inferior e a SMA de 200 períodos está acima da SMA de 55 períodos.
  - Posições vendidas saem quando o preço está abaixo da Banda de Bollinger superior e a SMA de 200 períodos está abaixo da SMA de 55 períodos.
- Ordens limitadas de compra pendentes são canceladas quando %K ≥ 90 e SMA de 200 períodos ≤ SMA de 55 períodos.
- Ordens limitadas de venda pendentes são canceladas quando %K ≤ 10 e SMA de 200 períodos ≥ SMA de 55 períodos.

## Parâmetros
- `StochLength` – período %K para o Estocástico.
- `OverboughtLevel` – nível para começar a vender.
- `OversoldLevel` – nível para começar a comprar.
- `ProfitTarget` – lucro realizado necessário para fechar posições abertas.
- `Order2Offset` … `Order9Offset` – distâncias em pips para ordens limitadas adicionais.
- `CandleType` – período das velas, padrão 4 horas.

## Indicadores
- StochasticOscillator
- BollingerBands
- SMA (200 e 55)
