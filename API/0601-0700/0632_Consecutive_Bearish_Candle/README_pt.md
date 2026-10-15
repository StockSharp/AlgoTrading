# Estratégia de Velas Baixistas Consecutivas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado após uma sequência de velas baixistas e sai quando o preço rompe acima da máxima anterior.

Esta abordagem de reversão à média compra após pressão de baixa excessiva, buscando uma recuperação quando os vendedores se esgotam.

## Detalhes

- **Critérios de entrada**: `N` velas baixistas consecutivas dentro da janela de tempo.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento acima da máxima anterior.
- **Stops**: Não.
- **Valores padrão**:
  - `Lookback` = 3
  - `CandleType` = TimeSpan.FromDays(1)
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: Price Action
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
