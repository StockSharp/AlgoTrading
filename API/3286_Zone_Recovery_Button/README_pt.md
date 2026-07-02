# Estratégia Zone Recovery Button
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **estratégia Zone Recovery Button** é uma conversão direta do expert advisor do MetaTrader "ZONE RECOVERY BUTTON VER1" (`MQL/25347`). O robô original dependia de botões BUY/SELL no gráfico para iniciar uma cesta com hedge. Nesta versão StockSharp, o painel manual é substituído por parâmetros, enquanto a lógica de recuperação, take-profits monetários/percentuais, trailing stop em moeda e proteção equity-stop são preservados.

Quando a estratégia recebe uma direção inicial, ela abre uma ordem a mercado inicial. Sempre que o preço atravessa a largura de zona configurada, o sistema empilha uma operação oposta com volume aumentado. A cesta é fechada quando o take-profit de referência é atingido, o lucro flutuante alcança o alvo monetário/percentual configurado, o trailing stop devolve lucro demais ou o limite de equity-stop é violado.

## Regras de negociação

1. **Direção inicial** - emula pressionar o botão BUY ou SELL. A estratégia abre a primeira ordem imediatamente quando recebe dados e tem permissão para negociar. Depois de fechar a cesta, ela pode reiniciar automaticamente com a mesma direção.
2. **Recuperação por zona** - em cada passo de recuperação, o algoritmo alterna a direção. Para ciclos comprados, ele vende quando o preço cai abaixo de `Base Price - Zone Width`, depois compra novamente quando o mercado retorna acima da base. Para ciclos vendidos, a lógica é espelhada.
3. **Escalonamento de volume** - cada hedge adicional multiplica o volume anterior ou adiciona um incremento fixo, reproduzindo as configurações "Lots"/"Multiply" do EA.
4. **Controles de take-profit** - a cesta é fechada por:
   - take-profit baseado em pips medido a partir do preço de referência;
   - alvo monetário na moeda da conta;
   - alvo percentual calculado a partir do valor atual do portfólio;
   - lógica trailing que trava ganhos quando o lucro flutuante excede um limite e depois devolve mais do que o drawdown permitido;
   - equity-stop emergencial que compara a perda flutuante atual com o maior equity observado durante o ciclo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | `TimeSpan.FromMinutes(5)` | Tipo de candle usado para monitorar movimentos de preço. |
| `StartDirection` | `Buy` | Direção inicial do ciclo (BUY/SELL/NONE). |
| `AutoRestart` | `true` | Reinicia automaticamente um novo ciclo depois que a cesta anterior fecha. |
| `TakeProfitPips` | `200` | Distância em pips entre o preço base e o alvo de take-profit em pips. |
| `ZoneRecoveryPips` | `10` | Distância em pips que aciona o próximo hedge na direção oposta. |
| `InitialVolume` | `0.01` | Volume (lotes) da primeira operação. |
| `UseVolumeMultiplier` | `true` | Se habilitado, cada hedge multiplica o volume anterior; caso contrário, `VolumeIncrement` é adicionado. |
| `VolumeMultiplier` | `2` | Multiplicador aplicado quando `UseVolumeMultiplier` é `true`. |
| `VolumeIncrement` | `0.01` | Incremento de volume quando `UseVolumeMultiplier` é `false`. |
| `MaxTrades` | `100` | Número máximo de operações na cesta. |
| `UseMoneyTakeProfit` | `false` | Habilita fechamento quando o lucro flutuante excede `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `40` | Alvo de lucro na moeda da conta. |
| `UsePercentTakeProfit` | `false` | Habilita fechamento quando o lucro flutuante excede `PercentTakeProfit` por cento do saldo. |
| `PercentTakeProfit` | `10` | Alvo de lucro em percentual do valor atual do portfólio. |
| `EnableTrailing` | `true` | Habilita trailing de lucro em moeda. |
| `TrailingProfitThreshold` | `40` | Nível de lucro que ativa trailing. |
| `TrailingDrawdown` | `10` | Drawdown permitido a partir do pico de lucro flutuante antes de fechar a cesta. |
| `UseEquityStop` | `true` | Habilita o equity stop emergencial. |
| `TotalEquityRiskPercent` | `1` | Perda flutuante máxima (em percentual do pico de equity) antes de zerar. |

## Observações

- A estratégia funciona com qualquer instrumento que forneça valores `PriceStep` e `StepPrice`. Esses parâmetros são necessários para converter distâncias em pips para unidades de preço e moeda.
- Como o StockSharp usa um modelo de posição líquida, a grade de hedge é simulada internamente. A estratégia mantém sua própria lista de passos de operação para reproduzir o cálculo de lucro do MetaTrader.
- A lógica trailing opera sobre o lucro flutuante da cesta ativa. Ela não usa trailing stops baseados em ordens.
