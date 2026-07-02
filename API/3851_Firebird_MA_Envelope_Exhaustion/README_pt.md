# Estratégia de exaustão de envelope Firebird MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o especialista em reversão de envelope Firebird v0.60. Ele mede uma média móvel simples e a compensa em uma porcentagem para formar os envelopes superior e inferior. Quando o preço ultrapassa a faixa superior a estratégia vende, e quando a faixa inferior rompe ela compra. As posições adicionais são calculadas em média apenas se o preço se mover pelo menos um passo configurável de pip além da entrada anterior. O stop loss total é compartilhado entre todas as entradas, evitando que tendências descontroladas entrem repetidamente na mesma direção.

## Detalhes

- **Critérios de entrada**:
  - Calcule um SMA na abertura da vela ou no ponto médio alto/baixo.
  - Envelope superior = SMA × (1 + Porcentagem/100); envelope inferior = SMA × (1 − Porcentagem/100).
  - Entre em posição curta em um fechamento acima da banda superior (a menos que uma parada recente bloqueie as posições vendidas), entre em posição comprada em um fechamento abaixo da banda inferior (a menos que as posições compradas estejam bloqueadas).
  - As negociações de média são permitidas quando o preço se move `PipStep` pips (opcionalmente escalonado por potência) além do preenchimento mais recente.
- **Longo/Curto**: Longo e curto.
- **Critérios de saída**:
  - Take Profit compartilhado ao preço médio de entrada ± `TakeProfit` pips.
  - Stop loss compartilhado ao preço médio de entrada ∓ `StopLoss / position count` pips.
  - A bandeira de bloqueio impede a reentrada na mesma direção até que um sinal oposto seja acionado após uma parada.
- **Stops**: Sim, stop loss e takeprofit agregados.
- **Valores padrão**:
  - `MaLength` = 10
  - `Percent` = 0,3
  - `TradeOnFriday` = verdadeiro
  - `UseHighLow` = falso (use aberturas)
  - `PipStep` = 30
  - `IncreasementPower` = 0
  - `TakeProfit` = 30
  - `StopLoss` = 200
  - `TradeVolume` = 1
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: SMA envelopes
  - Paradas: Sim
  - Complexidade: Média
  - Prazo: Qualquer
  - Sazonalidade: filtro opcional de sexta-feira
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto devido à média
