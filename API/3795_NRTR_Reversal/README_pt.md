# Estratégia de reversão NRTR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
## Visão geral
A estratégia de reversão NRTR é uma versão StockSharp do MetaTrader 4 especialista "NRTR_Revers". O sistema original traça uma linha Noise Reduction Trailing Range (NRTR) derivada do Average True Range (ATR) e inverte posições sempre que o preço quebra de forma convincente essa barreira adaptativa. A versão StockSharp mantém o comportamento de posição única do consultor especialista, espelha o cálculo de deslocamento baseado em ATR e gerencia saídas por meio do módulo de proteção integrado.

## Lógica de negociação
1. Assine a série de velas principal configurada por `CandleType` e processe apenas velas concluídas, replicando a verificação do contador `Bars` de MetaTrader.
2. Alimente um indicador `AverageTrueRange` com período `Period`. O valor ATR mais recente é traduzido de unidades de preço em "pontos" (etapas de preço) antes de ser multiplicado por `AtrMultiplier / 10`, assim como a expressão MQL `MathRound(k * (iATR / Point) / 10)`.
3. Mantenha um cache contínuo de velas recentes para reconstruir o pivô NRTR. A mínima mais baixa (para uma tendência de alta) ou a máxima mais alta (para uma tendência de baixa) nas últimas `Period` velas se torna o pivô base.
4. Mude o pivô pelo deslocamento baseado em ATR para formar a linha final:
   - Tendência de alta: `line = lowestLow - offset`.
   - Tendência de baixa: `line = highestHigh + offset`.
5. Detecte uma reversão sempre que uma das condições for atendida:
   - **Rompimento de fechamento:** o último fechamento da vela cruza a linha em mais de `offset` pontos.
   - **Expansão de intervalo:** as velas `Period / 2` mais recentes se estendem além da linha em pelo menos `ReverseDistancePoints` pontos. Isso reproduz o teste de reversão secundária do código MQL que analisou mais atrás na história.
6. Quando a direção mudar, envie uma ordem de mercado (`BuyMarket` ou `SellMarket`) com volume `TradeVolume + |Position|`. Isso fecha a exposição oposta e abre a nova posição, correspondendo ao comportamento MetaTrader de fechar e reverter imediatamente.
7. As saídas são delegadas ao gerenciador de risco iniciado por `StartProtection`, que converte as distâncias configuradas de stop-loss e take-profit dos pontos em unidades de preço específicas da corretora.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 15 minutos | Série de velas usadas para cálculos. |
| `TakeProfitPoints` | `decimal` | `4000` | Distância de take-profit expressa em etapas de preço do instrumento. Defina como zero para desativar. |
| `StopLossPoints` | `decimal` | `4000` | Distância de stop-loss em etapas de preço. Defina como zero para desativar. |
| `TrailingStopPoints` | `decimal` | `0` | Parâmetro reservado para módulos finais externos. Não usado dentro da estratégia. |
| `TradeVolume` | `decimal` | `0.1` | Volume base (lotes) espelhado da configuração MetaTrader. |
| `Period` | `int` | `3` | Número de velas usadas para calcular o pivô NRTR. |
| `ReverseDistancePoints` | `int` | `100` | Distância de fuga adicional em pontos necessários para confirmação. |
| `AtrMultiplier` | `decimal` | `3.0` | Multiplicador aplicado a ATR antes de construir o deslocamento. |

## Gestão de risco
- A estratégia chama `StartProtection` com `UnitTypes.Step`, então as distâncias dos pontos configurados são automaticamente convertidas em compensações de preço absoluto com base em `Security.PriceStep`.
- Se o stop-loss e o take-profit forem zero, `StartProtection()` ainda será chamado para ativar o monitoramento da posição de StockSharp, replicando as verificações de segurança usadas pelo EA.
- `TrailingStopPoints` é exposto para fins de integridade, mas deixado para extensões futuras, porque o especialista original não implementou uma função final, apesar de declarar o parâmetro.

## Detalhes de implementação
- A estratégia depende exclusivamente do API (`SubscribeCandles().BindEx(...)`) de alto nível com ligações de indicadores; nenhum loop de indicador manual ou chamadas `GetValue` proibidas são usadas.
- Uma estrutura `CandleSnapshot` compacta mantém apenas valores altos/baixos/fechados de velas recentes, evitando armazenamento pesado de `ICandleMessage` enquanto ainda reproduz as janelas de lookback NRTR.
- A conversão de ATR em pontos respeita a fórmula MetaTrader dividindo ATR pela etapa do instrumento antes de aplicar o multiplicador e o arredondamento.
- O corte do histórico mantém o cache em `Period * 3` velas para corresponder às necessidades de lookback originais sem crescimento descontrolado.

## Diferenças do especialista MetaTrader
- O fechamento da ordem é simplificado: em vez de iterar cada negociação e chamar `OrderClose`, a porta StockSharp envia uma única ordem de mercado que lisonjeia a posição existente e estabelece a nova direção.
- Números mágicos, slippage e parâmetros específicos do ticket são omitidos porque StockSharp gerencia pedidos de maneira diferente.
- As anotações do gráfico são opcionais; quando uma área do gráfico está disponível, a série ATR e as próprias negociações são plotadas para fins de depuração.

## Dicas de uso
- Alinhe `TradeVolume` com a etapa do lote de bolsa (`Security.VolumeStep`) antes de ativar a negociação ao vivo.
- Sintonize `Period`, `AtrMultiplier` e `ReverseDistancePoints` juntos. Períodos mais curtos exigem distâncias reversas menores para evitar negociações excessivas.
- Defina as distâncias de parada/alvo de acordo com o tamanho do tick do instrumento. Em instrumentos com `PriceStep` grande, reduza os deslocamentos padrão de 4.000 pontos para níveis realistas.

## Indicadores
- `AverageTrueRange(Period)` calculado com base nos preços máximo/mínimo/fechamento.
