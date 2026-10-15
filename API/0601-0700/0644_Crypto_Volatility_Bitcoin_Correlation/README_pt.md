# Correlação de Volatilidade Crypto com Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em posição comprada quando a volatilidade do Bitcoin sobe junto com o índice BVOL7D e o preço está acima da sua EMA. Sai quando o preço cai abaixo da EMA.

## Detalhes

- **Critérios de entrada**: VIXFix maior que o valor anterior, BVOL7D maior que o valor anterior, fechamento acima da EMA.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento abaixo da EMA.
- **Stops**: Não.
- **Valores padrão**:
  - `VixFixLength` = 22
  - `EmaLength` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Somente comprado
  - Indicadores: Highest, EMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
