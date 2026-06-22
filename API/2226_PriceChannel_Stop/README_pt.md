# PriceChannel Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Price Channel Stop.

O indicador calcula a máxima mais alta e a mínima mais baixa no período dado para formar um canal de Donchian. Os níveis de stop são construídos dentro do canal usando o fator `Risk`. Quando o preço fecha acima do stop superior, a tendência muda para altista; ao fechar abaixo do stop inferior, a tendência muda para baixista. A estratégia abre posições nessas reversões e, opcionalmente, fecha posições opostas.

## Detalhes

- **Critérios de entrada**: O preço cruza os níveis de stop.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ChannelPeriod` = 5
  - `Risk` = 0.10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Canal de Donchian
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
