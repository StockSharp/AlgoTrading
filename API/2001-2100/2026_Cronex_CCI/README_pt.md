# Estratégia Cronex CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento do Índice de Canal de Mercadoria Cronex. O indicador suaviza o CCI através de duas médias móveis exponenciais para criar uma linha rápida e uma lenta.

A estratégia abre uma posição comprada quando a linha rápida cruza abaixo da linha lenta e fecha qualquer posição vendida. Uma posição vendida é aberta quando a linha rápida cruza acima da linha lenta e fecha qualquer posição comprada.

Esta abordagem contrária tenta capturar reversões após mudanças de momentum. Funciona em períodos mais altos, como candles de 4 horas.

## Detalhes

- **Critérios de entrada**: Cruzamentos das linhas CCI rápida e lenta suavizadas.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `CciPeriod` = 25
  - `FastPeriod` = 14
  - `SlowPeriod` = 25
  - `CandleType` = TimeSpan.FromHours(4)
  - `EnableLongEntry` = true
  - `EnableShortEntry` = true
  - `EnableLongExit` = true
  - `EnableShortExit` = true
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: CCI, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Swing (4h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
