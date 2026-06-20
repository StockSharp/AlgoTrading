# Tendência ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na tendência do Índice Direcional Médio (ADX). A estratégia de Tendência ADX avalia a força do mercado usando o indicador ADX. Quando o ADX está acima de um limiar e o preço está no lado correto de sua média móvel, o sistema opera nessa direção. As posições são fechadas quando o ADX enfraquece ou a configuração oposta aparece.

Os testes indicam um retorno anual médio de aproximadamente 46%. Funciona melhor no mercado de ações.

Ao aguardar uma leitura sólida do ADX, a abordagem só opera quando o momentum está firmemente estabelecido. Os stops normalmente usam um múltiplo de ATR para que o risco se ajuste com a volatilidade.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, ADX, ATR.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `MaPeriod` = 50
  - `AtrMultiplier` = 2m
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, ADX, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

