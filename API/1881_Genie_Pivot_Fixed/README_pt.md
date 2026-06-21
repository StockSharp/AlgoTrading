# Estratégia Genie Pivot Fixo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o sistema de scalping de reversão em pontos pivô "Genie" originalmente escrito em MQL4. Ele verifica os últimos oito candles para detectar reversões repentinas em pontos pivô. Uma operação comprada é acionada quando sete mínimas consecutivas diminuem e o candle atual forma uma mínima mais alta enquanto fecha acima da máxima anterior. Uma operação vendida é acionada quando sete máximas consecutivas aumentam e o candle atual forma uma máxima mais baixa enquanto fecha abaixo da mínima anterior.

A estratégia usa um tamanho de posição fixo (Strategy.Volume) e aplica tanto um stop trailing quanto um take profit medidos em unidades de preço absolutas. Esses parâmetros podem ser otimizados e permitem capturar reversões rápidas enquanto protegem os lucros abertos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Low[7] > Low[6] > ... > Low[1]` && `Low[1] < Low[0]` && `High[1] < Close[0]`.
  - **Vendido**: `High[7] < High[6] < ... < High[1]` && `High[1] > High[0]` && `Low[1] > Close[0]`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop trailing ou take profit é atingido.
- **Stops**:
  - Take-profit: distância absoluta do ponto de entrada.
  - Stop trailing: distância absoluta, que acompanha à medida que a operação se move a favor.
- **Valores padrão**:
  - `TakeProfit` = 500.
  - `TrailingStop` = 200.
  - `CandleType` = 1 minuto.
- **Filtros**:
  - Categoria: Reversão.
  - Direção: Ambos.
  - Indicadores: Nenhum.
  - Stops: Sim.
  - Complexidade: Simples.
  - Período: Curto prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
