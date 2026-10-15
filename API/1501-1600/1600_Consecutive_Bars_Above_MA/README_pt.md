# Estratégia de Barras Consecutivas Acima da MA Somente Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente vendida que conta fechamentos consecutivos acima de uma média móvel e vende em rompimentos acima da máxima anterior. Sai quando o preço cai abaixo da mínima anterior. Um filtro EMA 200 opcional impõe a tendência de baixa.

## Detalhes

- **Critérios de entrada**: Limiar de fechamentos consecutivos acima da MA e fechamento > máxima anterior
- **Comprado/Vendido**: Vendido
- **Critérios de saída**: Fechamento abaixo da mínima anterior
- **Stops**: Não
- **Valores padrão**:
  - `Threshold` = 3
  - `MaType` = SMA
  - `MaLength` = 5
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoria: Padrão
  - Direção: Vendido
  - Indicadores: MA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
