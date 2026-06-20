# Estratégia de Exaustão ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um aumento repentino no Average True Range indica uma expansão da volatilidade que pode desaparecer rapidamente. Esta estratégia busca leituras de ATR que ultrapassem uma média móvel por um multiplicador configurável. Combinada com um candle de reversão, visa capturar a contração subsequente.

Os testes indicam um retorno anual médio de aproximadamente 139%. Funciona melhor no mercado de ações.

Cada barra atualiza o ATR e sua própria média. Se o ATR exceder a média pelo multiplicador e o candle fechar na direção oposta ao movimento anterior, uma operação é aberta. O stop-loss também usa um múltiplo do ATR, ancorando o risco aos níveis de volatilidade atuais.

As posições tipicamente dependem do stop para a saída, buscando uma retração rápida após o pico de volatilidade se dissipar.

## Detalhes

- **Critérios de entrada**: Pico de ATR acima da média com candle de reversão.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss.
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
  - `AtrMultiplier` = 1.5
  - `MaPeriod` = 20
  - `StopLoss` = 2%
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: ATR, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

