# Estratégia de Momentum BTCUSD Após Dias Anormais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia mede o retorno diário como `(close - open) / open` e o compara com uma média móvel e um desvio padrão ao longo de um período configurável. Se o retorno exceder o limiar superior, abre uma posição comprada; se cair abaixo do limiar inferior, abre uma posição vendida. Todas as posições são encerradas no fechamento do dia seguinte.

## Detalhes

- **Critérios de entrada**:
  - Retorno > média + k × desvio padrão → comprado.
  - Retorno < média - k × desvio padrão → vendido.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Fechar todas as posições no fechamento do dia seguinte.
- **Stops**: Nenhum
- **Valores padrão**:
  - Período de lookback = 5
  - Limiar de retorno anormal (k) = 1.6
  - Capital por operação = 1000
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
