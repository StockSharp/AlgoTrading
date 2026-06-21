# Estratégia MACFibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o sistema de trading MACFibo. Aguarda um cruzamento entre a EMA de 5 períodos e a SMA de 20 períodos. Após o cruzamento, o algoritmo mede o swing desde o fechamento da barra do cruzamento (ponto A) até a extremidade mais recente (ponto B) e constrói níveis de expansão de Fibonacci. As posições são abertas ao preço de mercado com take profit e stop loss derivados desses níveis. Uma saída opcional fecha operações perdedoras quando a EMA rápida cruza a SMA média na direção oposta.

## Detalhes

- **Condições de entrada:**
  - **Comprado:** EMA de 5 cruza acima da SMA de 20. O ponto B é a mínima mais baixa desde que o movimento de queda começou.
  - **Vendido:** EMA de 5 cruza abaixo da SMA de 20. O ponto B é a máxima mais alta desde que o movimento de alta começou.
- **Condições de saída:**
  - Take profit no nível de 161,8% de Fibonacci ou na distância mínima de take profit.
  - Stop loss no nível de 38,2% de Fibonacci ou na distância máxima de stop loss.
  - Fechamento opcional se EMA de 5 cruzar SMA de 8 contra a posição e a operação estiver perdendo.
- **Filtros:**
  - Opera apenas entre as horas de início e fim configuradas.
  - O trading na segunda ou sexta-feira pode ser desabilitado.
- **Parâmetros:**
  - `FastLength` – comprimento da EMA rápida.
  - `MidLength` – comprimento da SMA média para saída protetora.
  - `SlowLength` – comprimento da SMA lenta para detecção de tendência.
  - `MinTakeProfit` – take profit mínimo em unidades de preço.
  - `MaxStopLoss` – stop loss máximo em unidades de preço.
  - `StartHour` / `EndHour` – janela de tempo de trading permitida.
  - `FridayTrade` / `MondayTrade` – habilitar trading nesses dias.
  - `CloseAtFastMid` – fechar operações perdedoras no cruzamento rápido-médio.
  - `CandleType` – tipo de vela para cálculos.
