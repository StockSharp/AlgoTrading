# Estratégia Long e Short com Múltiplos Indicadores
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza RSI, Rate of Change e uma média móvel selecionável para gerar sinais comprados e vendidos. Aplica um stop trailing baseado em ATR para as saídas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: RSI entre sobrevendido e sobrecomprado, ROC > 0 e preço acima da MA.
  - Vendido: Tendência de baixa confirmada, ROC < 0 e preço abaixo da MA.
- **Comprado/Vendido**: Comprado e vendido.
- **Critérios de saída**:
  - Stop trailing baseado em ATR ou condições de stop por indicador.
- **Stops**: Stop trailing ATR.
- **Valores padrão**:
  - `RsiLength` = 5
  - `RsiOverbought` = 70
  - `RsiOversold` = 44
  - `RocLength` = 4
  - `MaLength` = 24
  - `MaTypeParam` = TEMA
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `BearishMaLength` = 200
  - `BearishTrendDuration` = 5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado & Vendido
  - Indicadores: RSI, ROC, MA, ATR
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
