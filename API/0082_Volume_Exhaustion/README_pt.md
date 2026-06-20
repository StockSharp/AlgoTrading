# Estratégia de Exaustão de Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Picos bruscos de volume frequentemente sinalizam o fim de um movimento quando traders se apressam a fechar ou abrir posições. Esta estratégia mede o volume atual em relação a uma média para detectar exaustão. Combinada com a direção do candle e um filtro de média móvel, pode identificar entradas de reversão com precisão.

Os testes indicam um retorno anual médio de aproximadamente 133%. Funciona melhor no mercado de criptomoedas.

Cada candle atualiza o volume médio. Se o volume da nova barra exceder essa média por um multiplicador definido e o candle fechar na direção oposta à tendência predominante, o sistema entra em uma operação. Um stop baseado em ATR protege a posição.

A operação é tipicamente encerrada pelo stop-loss, pois a estratégia antecipa uma reversão rápida após o pico de volume.

## Detalhes

- **Critérios de entrada**: Pico de volume acima da média com candle contrário à tendência.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 2.0
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Volume, MA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

