# Estratégia de Reversão à Média Consecutive Close High1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente vendida que conta fechamentos consecutivos acima da máxima anterior e vende quando a contagem atinge um limiar. A posição é encerrada quando o preço cai abaixo da mínima anterior. O filtro EMA 200 opcional confirma a tendência de baixa.

## Detalhes

- **Critérios de entrada**: fechamentos consecutivos acima da máxima anterior atingem o limiar
- **Comprado/Vendido**: Vendido
- **Critérios de saída**: fechamento abaixo da mínima anterior
- **Stops**: Não
- **Valores padrão**:
  - `Threshold` = 3
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Vendido
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
