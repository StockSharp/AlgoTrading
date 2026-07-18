# Estratégia TTM Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia TTM Squeeze procura períodos de compressão de preços quando as Bollinger
Bands se contraem dentro dos Keltner Channels. Este "squeeze" sinaliza uma possível
expansão de volatilidade. Durante o squeeze, a estratégia monitora um oscilador de
momentum de regressão linear e RSI para avaliar a direção. Quando o squeeze se libera
e o momentum vira, posições são tomadas na direção do movimento.

O método busca rompimentos explosivos de ranges tranquilos. As operações são filtradas
de modo que as configurações compradas requerem momentum subindo de abaixo de zero com
RSI acima de 30, enquanto as vendidas precisam de momentum caindo de território positivo
com RSI abaixo de 70. Um parâmetro opcional de take-profit pode fechar operações
automaticamente em um ganho predefinido.

## Detalhes

- **Critérios de entrada**:
  - Squeeze desativado (Bollinger Bands fora dos Keltner Channels).
  - **Comprado**: Momentum < 0 e subindo, RSI > 30.
  - **Vendido**: Momentum > 0 e caindo, RSI < 70.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto ou take-profit se habilitado.
- **Stops**: Nenhum por padrão, take-profit opcional.
- **Valores padrão**:
  - `SqueezeLength` = 20
  - `RsiLength` = 14
  - `UseTP` = False
  - `TpPercent` = 1.2
- **Filtros**:
  - Categoria: Rompimento de volatilidade
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, RSI, Regressão linear
  - Stops: Opcional
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
