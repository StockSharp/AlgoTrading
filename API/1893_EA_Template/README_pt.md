# Estratégia EA Template
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia tem origem num modelo de EA do MetaTrader. Ela analisa o vela finalizada anterior e abre uma posição na direção do corpo da vela. Uma vela de alta aciona uma operação comprada, enquanto uma vela de baixa aciona uma vendida. O modo de reversão inverte a interpretação da vela para que a estratégia negocie contra a cor da barra.

A estratégia suporta tamanho de posição fixo ou cálculo baseado em capital. Os níveis de stop-loss e take-profit são definidos em pontos a partir do preço de entrada. A negociação é ignorada quando o spread excede o limite permitido.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: fechamento da vela anterior > abertura e `ReverseTrade` desabilitado.
  - **Vendido**: fechamento da vela anterior < abertura e `ReverseTrade` desabilitado.
  - Quando `ReverseTrade` está habilitado, os sinais são invertidos.
  - O spread deve estar abaixo de `SpreadLimit` pontos.
- **Critérios de saída**:
  - Cor de vela oposta ou ativação de stop-loss/take-profit.
- **Dimensionamento de posição**:
  - Tamanho fixo `Lots` ou tamanho baseado em capital usando `RiskPercent` quando `UseMoneyManagement` é true.
- **Stops**:
  - `StopLoss` e `TakeProfit` em pontos relativos ao preço de entrada.
- **Comprado/Vendido**: Ambas as direções.
- **Indicadores**: Nenhum.
- **Nível de risco**: Médio.

Os parâmetros permitem ajustar o tipo de vela, o modo de reversão, as regras de gestão de capital e os limites de risco.
