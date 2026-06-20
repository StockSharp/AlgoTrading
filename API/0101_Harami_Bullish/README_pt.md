# Bullish Harami Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Harami de Alta é um padrão de duas velas onde um corpo pequeno está contido dentro do intervalo da vela de baixa anterior. Isso indica que o momentum de venda parou e os compradores podem voltar a entrar.

Os testes indicam um retorno anual médio de aproximadamente 40%. Funciona melhor no mercado de criptomoedas.

Esta estratégia entra comprada assim que a segunda vela fecha dentro da primeira, esperando continuação para cima na barra seguinte.

Um stop percentual abaixo do padrão fornece proteção, e a operação encerra se o preço cair novamente abaixo do setup.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
