# Estratégia Stochastic RSI OHLC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói barras OHLC a partir do indicador Stochastic RSI e opera em mudanças de momentum. Calcula o RSI para os preços máximo, mínimo e de fechamento e aplica um oscilador estocástico a cada série. Uma posição comprada é aberta quando o Stochastic RSI sobe de um pivô e cruza acima do nível de entrada comprado. Uma posição vendida é aberta quando cai de um pivô e cruza abaixo do nível de entrada vendido.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Stochastic RSI vira para cima e qualquer um dos últimos três valores supera `LongEntry` após um pivô baixo.
  - **Vendido**: Stochastic RSI vira para baixo e qualquer um dos últimos três valores cai abaixo de `ShortEntry` após um pivô alto.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RSI Length` = 14
  - `K Length` = 14
  - `D Length` = 3
  - `LongEntry` = 30
  - `ShortEntry` = 60
  - `LongPivot` = 2
  - `ShortPivot` = 98
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: RSI, Stochastic
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
