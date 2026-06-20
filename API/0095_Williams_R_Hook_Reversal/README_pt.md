# Williams %R Hook Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Williams %R Hook Reversal acompanha o indicador Williams %R quando ele recua rapidamente de um extremo. Quando a leitura se move acima de -20 ou abaixo de -80 e depois engata em direção ao centro, o impulso anterior provavelmente está exausto.

Os testes indicam um retorno anual médio de aproximadamente 172%. Funciona melhor no mercado de câmbio.

A estratégia compra quando %R reverte para cima a partir da sobrevenda enquanto o preço pressiona novas mínimas, e vende quando engata para baixo a partir da sobrecompra durante novas máximas.

Um stop percentual ajustado controla o risco, e as operações encerram assim que %R engancha na direção oposta ou o stop é acionado.

## Detalhes

- **Critérios de entrada**: sinal do indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Williams %R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
