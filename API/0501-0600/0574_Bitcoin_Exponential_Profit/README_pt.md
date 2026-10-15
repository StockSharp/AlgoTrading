# Estratégia de Lucro Exponencial de Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra comprada quando a EMA rápida cruza acima da EMA lenta. O tamanho da posição é calculado a partir de um percentual de risco do patrimônio da conta. As saídas ocorrem em um cruzamento de EMA para baixo, stop-loss, take-profit ou trailing stop.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida cruza acima da EMA lenta → comprado.
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - EMA rápida cruza abaixo da EMA lenta.
  - Stop-loss no percentual de risco.
  - Take-profit = risco × multiplicador de retorno.
  - Trailing stop a partir do preço mais alto.
- **Stops**: SL, TP, trailing stop
- **Valores padrão**:
  - Comprimento da EMA rápida = 9
  - Comprimento da EMA lenta = 21
  - Percentual de risco = 1
  - Multiplicador de retorno = 2
  - Percentual do trailing stop = 0.5
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: EMA
  - Stops: SL & TP & Trailing
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
