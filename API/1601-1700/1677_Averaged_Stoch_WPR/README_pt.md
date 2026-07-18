# Estratégia Média Stoch & WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o oscilador Stochastic com Williams %R para detectar condições extremas do mercado.
Uma posição comprada é aberta quando o valor do Stochastic cai abaixo de 0.1 e Williams %R está abaixo de -90, sinalizando forte pressão de sobrevenda.
Uma posição vendida é aberta quando o Stochastic sobe acima de 99.9 e Williams %R excede -5, indicando fortes condições de sobrecompra.

A estratégia funciona com qualquer instrumento e período suportado pelo tipo de vela selecionado. Pode operar tanto em posições compradas quanto vendidas e oferece um stop-loss percentual opcional para gerenciamento de risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Stochastic < 0.1 e Williams %R < -90.
  - **Vendido**: Stochastic > 99.9 e Williams %R > -5.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou stop-loss acionado.
- **Stops**: Stop-loss percentual opcional.
- **Indicadores**:
  - Oscilador Stochastic (período padrão 26).
  - Williams %R (período padrão 26).

## Parâmetros

- `StochPeriod` – período de cálculo do Stochastic.
- `WprPeriod` – período de cálculo do Williams %R.
- `StopLossPercent` – tamanho do stop-loss percentual.
- `CandleType` – tipo de vela usado para cálculos de indicadores.
