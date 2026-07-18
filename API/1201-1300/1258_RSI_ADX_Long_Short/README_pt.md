# Estratégia RSI & ADX Comprado/Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera em ambas as direções usando RSI para sinais e ADX para confirmação de tendência.
Uma posição comprada é aberta quando o RSI cruza acima de 70 e o ADX está acima do limiar.
Uma posição vendida é aberta quando o RSI cruza abaixo de 30 e o ADX está acima do limiar.
As posições são encerradas em cruzamentos opostos do RSI.

## Detalhes

- **Critérios de entrada**: RSI cruza acima de 70 para comprados ou abaixo de 30 para vendidos com ADX acima do limiar
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamentos opostos do RSI
- **Stops**: Não
- **Valores padrão**:
  - `RsiLength` = 8
  - `AdxLength` = 20
  - `AdxThreshold` = 14
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: RSI, ADX
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
