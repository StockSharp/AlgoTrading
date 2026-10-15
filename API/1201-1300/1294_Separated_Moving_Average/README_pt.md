# Média Móvel Separada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Constrói médias móveis separadas para fechamentos de alta e de baixa. Uma posição comprada é aberta quando a média de alta sobe acima da de baixa, e uma posição vendida é aberta no cruzamento inverso. A estratégia suporta SMA, EMA ou HMA e pode operar com preços Heikin Ashi.

## Detalhes

- **Critérios de entrada**: Média de alta cruzando acima da média de baixa.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `MaType` = MaType.SMA
  - `Length` = 20
  - `UseHeikinAshi` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA, HMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

