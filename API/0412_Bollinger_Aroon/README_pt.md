# Estratégia de Bollinger Aroon
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Bollinger Aroon busca retrocessos dentro de uma forte tendência de alta.
Quando o preço se estende abaixo da banda inferior de Bollinger, mas o valor do
Aroon Up permanece elevado, o sistema assume que a tendência está intacta e busca
uma reversão à média. Opera apenas comprado, buscando capturar o rebote após
uma queda temporária.

A configuração é acionada após uma vela concluída fechar abaixo da banda inferior
enquanto *Aroon Up* supera o nível de confirmação. A posição permanece aberta até
que a leitura do Aroon caia abaixo de um limite de stop ou o preço suba até a
banda superior. A largura das bandas se adapta à volatilidade, permitindo que a
estratégia opere em mercados calmos e ativos igualmente.

Backtests em grandes pares de cripto mostram que a abordagem se destaca durante
tendências fortes com sacudidas ocasionais. Como as entradas exigem tanto expansão
de volatilidade quanto uma leitura persistente de Aroon Up, os sinais falsos são
reduzidos em comparação com uma reversão simples de Bollinger.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento abaixo da banda inferior E `Aroon Up` > nível de confirmação.
  - **Vendido**: não utilizado.
- **Critérios de saída**:
  - Fechamento toca a banda superior OU `Aroon Up` < nível de stop.
- **Stops**: Baseado em indicador; sem stop fixo por padrão.
- **Valores padrão**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `AroonLength` = 288
  - `AroonConfirmation` = 90
  - `AroonStop` = 70
- **Filtros**:
  - Categoria: Reversão à média dentro da tendência
  - Direção: Somente comprado
  - Indicadores: Bollinger Bands, Aroon
  - Complexidade: Moderado
  - Nível de risco: Médio
