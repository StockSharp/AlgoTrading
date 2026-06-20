# Estratégia EMA50 Crossover DCA Mensal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

EMA50 Crossover DCA Mensal compra quando o preço fecha acima da EMA de 50 períodos e acumula posições adicionais a cada mês. Os valores de DCA não investidos são armazenados como dinheiro e implantados assim que a tendência é retomada.

A estratégia vende quando o preço cai abaixo da EMA, encerrando a posição.

## Detalhes

- **Critérios de entrada**: fechamento > EMA(50)
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: o preço cruza abaixo de EMA(50)
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 1 semana
  - `DcaAmount` = 100000
  - `StartDate` = 1980-01-01
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
