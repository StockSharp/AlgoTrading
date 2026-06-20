# ADX DI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada nos indicadores ADX e Movimento Direcional

Os testes indicam um retorno anual médio de aproximadamente 103%. Funciona melhor no mercado de ações.

ADX DI foca no cruzamento de +DI e -DI com ADX em alta. Um cruzamento altista de +DI sobre -DI combinado com ADX forte abre posições compradas, enquanto o oposto abre vendidas. As posições fecham com enfraquecimento do ADX ou cruzamento oposto.

Essa combinação ajuda a evitar operar em cada cruzamento de DI ao exigir confirmação do ADX. O sistema visa capturar tendências sustentáveis em vez de oscilações de curto prazo.


## Detalhes

- **Critérios de entrada**: Sinais baseados em ADX, ATR.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

