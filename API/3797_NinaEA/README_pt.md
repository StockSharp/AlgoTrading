# Estratégia de Nina EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Nina EA é uma seguidora de tendência de uma posição convertida do especialista MetaTrader 4 "NinaEA". O robô original usa um indicador personalizado chamado **NINA** e negocia sempre que a diferença entre os buffers de alta e de baixa do indicador ultrapassa acima ou abaixo de zero. Na versão StockSharp, o indicador personalizado é substituído pelo indicador integrado **SuperTrend**, que também publica buffers separados de alta e baixa. Uma mudança na direção da SuperTrend serve como proxy de cruzamento zero: quando a tendência se torna de alta, a estratégia compra, e quando se torna de baixa, ela vende.

A estratégia sempre mantém no máximo uma posição aberta. Um sinal oposto fecha imediatamente a posição existente e estabelece uma nova negociação na nova direção. Um stop-loss opcional expresso em faixas de preço pode ser habilitado para imitar a entrada "StopLoss" original.

## Lógica de negociação
1. Assine a série de velas configurada e calcule o SuperTrend com o período ATR e o multiplicador fornecidos.
2. Espere até que a estratégia e o indicador sejam formados antes de reagir aos sinais.
3. Em cada vela concluída:
   - Se um preço stop protetor for tocado, saia da posição aberta no mercado.
   - Se o SuperTrend mudar de baixa para alta, feche qualquer exposição curta e compre com o volume configurado.
   - Se o SuperTrend mudar de alta para baixa, feche qualquer exposição longa e venda com o volume configurado.
   - Armazene a direção atual do SuperTrend para detectar a próxima virada.

A lógica replica o comportamento do especialista MetaTrader, onde `nina = Buffer0 - Buffer1` e uma mudança de sinal acionam saídas e novas entradas.

## Gestão de Posição e Risco
- Apenas uma única posição pode estar ativa por vez; todas as negociações invertem a direção em vez de empilhar vários pedidos.
- Um stop-loss opcional em pontos de preço é calculado a partir do preço de preenchimento. Para uma negociação longa, o stop é colocado abaixo da entrada e, para uma negociação curta, é colocado acima da entrada. Definir o parâmetro como zero desativa a parada.
- `StartProtection()` é chamado para que as proteções StockSharp integradas possam ser configuradas, se desejado.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Volume` | `0.1` | Volume de pedidos usado para cada nova entrada. |
| `AtrPeriod` | `10` | Período ATR passado para o cálculo do SuperTrend (mapeia o `PeriodWATR` original). |
| `AtrMultiplier` | `1` | Multiplicador ATR para SuperTrend (mapeia o `Kwatr` original). |
| `StopLossPoints` | `0` | Distância de stop-loss opcional em faixas de preço. Zero mantém o stop desabilitado, idêntico ao código MetaTrader que enviou ordens de mercado sem preço stop. |
| `CandleType` | `TimeFrame(1 minute)` | Série de velas que alimenta o indicador e a lógica de negociação. |

## Notas de conversão
- O especialista MetaTrader confiou no indicador `NINA` personalizado. Seus dois buffers foram interpretados como linhas SuperTrend de alta/baixa porque apenas sua diferença e sinal importavam para a negociação. SuperTrend expõe as mesmas informações por meio de seu sinalizador `IsUpTrend`, o que o torna um substituto de alto nível adequado, sem necessidade de manipulação manual de buffer.
- A lógica de fechamento da ordem reflete o loop `OrdersTotal()` do script original: uma mudança de tendência primeiro lisonjeia a posição atual e depois abre uma negociação na nova direção.
- As entradas MetaTrader não utilizadas (`highlow`, `cbars`, `from`, `maP`, `SMAspread`, `Slippage`) são omitidas porque não influenciam as regras de negociação no arquivo original.

## Dicas de uso
1. Anexe a estratégia a um título e configure o período de vela que corresponde ao seu teste MetaTrader.
2. Ajuste o período ATR e o multiplicador para replicar o comportamento do indicador original.
3. Aumente `StopLossPoints` se desejar um limite de risco rígido; caso contrário, deixe-o em zero para saídas puramente baseadas em sinal.
