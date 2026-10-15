# Bago EA Estratégia Clássica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp fiel do MetaTrader especialista de `MQL/7656/Bago_ea.mq4`. Ele mantém a filosofia original de acompanhamento de tendências: as entradas são acionadas apenas quando médias móveis exponenciais e RSI rompem a zona neutra na mesma direção, enquanto o túnel de Vegas atua como um filtro espacial e como âncora para o rastreamento passo a passo.

## Lógica de negociação em detalhes

1. **Pilha de indicadores**
   - EMAs rápidos e lentos (`FastPeriod`/`SlowPeriod`, método compartilhado `MaMethod`, preço aplicado `MaAppliedPrice`).
   - EMAs do túnel Vegas com períodos fixos 144 e 169 usando as mesmas configurações para emular os envelopes do túnel.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) com o nível 50 clássico usado como filtro de confirmação.
   - Os dados da vela vêm de `CandleType`; o mesmo feed de vela alimenta todos os indicadores por meio do pipeline `Bind` de alto nível.
2. **Máquina entre estados**
   - Os cruzamentos EMA e RSI acima/abaixo de seus limites definem sinalizadores booleanos e contadores de barras. Cada estado expira após `CrossEffectiveBars` velas concluídas ou quando a cruz oposta aparece, exatamente como os temporizadores da versão MQL.
   - Sinalizadores de túnel adicionais rastreiam quando o preço de fechamento salta de um lado para o outro do túnel de Las Vegas, para que a lógica de rastreamento saiba qual regime aplicar.
3. **Portão da sessão**
   - A negociação é permitida apenas durante sessões de mercado selecionadas: Londres (07–16), Nova York (12–21) e Tóquio (00–08 mais o bar das 23:00). Essas janelas replicam as opções `extern bool` no EA original.
4. **Filtros de entrada**
   - **Longo**: requer sinalizadores EMA-up e RSI-up e um fechamento de alta acima do túnel em pelo menos `TunnelBandWidthPips`, mas não além de `TunnelSafeZonePips`, ou um fechamento de retração abaixo do túnel em `TunnelBandWidthPips` sinalizando um salto.
   - **Short**: condições espelhadas usando EMA-down/RSI-down e verificações de túnel simétrico.
   - Quando uma posição reversa é aberta, a estratégia a fecha no mercado antes de entrar na nova direção, imitando `OrderClose` de MetaTrader.
5. **Gerenciamento de posição e saída**
   - O stop loss inicial é colocado a `StopLossPips` de distância da entrada. Sempre que o preço estaciona ao redor do túnel de Las Vegas, a parada é realocada usando uma almofada extra `StopLossToFiboPips` para corresponder às compensações "fibo" do especialista.
   - As etapas finais correspondem aos níveis TP do EA. À medida que o preço se afasta do túnel, a estratégia primeiro estaciona a parada perto do túnel ±(`TrailingStepX` + `StopLossToFiboPips`) e gradualmente muda para um rastreamento puro de acompanhamento de preço de `TrailingStopPips`.
   - As saídas parciais (`PartialClose1Volume`, `PartialClose2Volume`) são executadas quando o movimento atinge `TrailingStep1Pips` e `TrailingStep2Pips`. O volume restante é gerenciado pelo trailing stop até que a terceira etapa (`TrailingStep3Pips`) seja atingida.
   - Qualquer cruzamento EMA/RSI oposto fecha imediatamente a posição completa.
6. **Tratamento de pedidos**
   - As ordens de parada são mantidas explicitamente por meio de chamadas `SellStop`/`BuyStop`. Cada vez que o stop precisa ser movido, a ordem anterior é cancelada e uma nova é submetida; isso reflete as chamadas `OrderModify` repetidas do código MQL.
   - Todos os cálculos de pip dependem do instrumento `PriceStep` e se ajustam automaticamente para cotações de 3 ou 5 dígitos multiplicando o passo por dez, assim como a conversão de pontos de MetaTrader.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `TradeVolume` | decimal | 3 | Volume total aberto em um novo sinal. |
| `StopLossPips` | decimal | 30 | Distância inicial de parada de proteção em pips. |
| `StopLossToFiboPips` | decimal | 20 | Buffer extra ao mover paradas nas faixas do túnel de Las Vegas. |
| `TrailingStopPips` | decimal | 30 | Distância do trailing stop quando o preço sai do túnel. |
| `TrailingStep1Pips` | decimal | 55 | Primeira camada de lucro derivada do nível TP1 do EA. |
| `TrailingStep2Pips` | decimal | 89 | Segunda camada de lucro (TP2). |
| `TrailingStep3Pips` | decimal | 144 | Terceira camada de lucro (TP3) antes de mudar para trailing puro. |
| `PartialClose1Volume` | decimal | 1 | Volume para fechar quando `TrailingStep1Pips` for alcançado. |
| `PartialClose2Volume` | decimal | 1 | Volume para fechar quando `TrailingStep2Pips` for alcançado. |
| `CrossEffectiveBars` | interno | 2 | Número de velas concluídas enquanto as bandeiras cruzadas permanecem válidas. |
| `TunnelBandWidthPips` | decimal | 5 | Zona neutra ao redor do túnel onde novas negociações são evitadas. |
| `TunnelSafeZonePips` | decimal | 120 | Distância máxima do túnel que ainda permite uma entrada de fuga. |
| `EnableLondonSession` | bool | verdade | Habilite a negociação entre 07:00 e 16:00 horário de troca. |
| `EnableNewYorkSession` | bool | verdade | Habilite a negociação entre 12h e 21h, horário de câmbio. |
| `EnableTokyoSession` | bool | falso | Ative a negociação entre 00h00 e 08h00 e na vela das 23h00. |
| `FastPeriod` | interno | 5 | Comprimento EMA rápido. |
| `SlowPeriod` | interno | 12 | Comprimento EMA lento. |
| `MaShift` | interno | 0 | Deslocamento horizontal das médias móveis. |
| `MaMethod` | `MovingAverageType` | Exponencial | Modo de cálculo EMA (mantido configurável para experimentação). |
| `MaAppliedPrice` | `AppliedPriceType` | Fechar | Preço da vela encaminhado aos EMAs. |
| `RsiPeriod` | interno | 21 | RSI período médio. |
| `RsiAppliedPrice` | `AppliedPriceType` | Fechar | Preço da vela encaminhado para RSI. |
| `CandleType` | `DataType` | Período H1 | Série de velas impulsionando a estratégia. |

## Notas de implementação

- A estratégia é executada inteiramente na assinatura de velas de alto nível API e mantém os valores dos indicadores em listas rolantes para imitar a indexação de barras (`Close[1]`, `Close[2]`) do script original.
- Temporizadores e sinalizadores de túnel reproduzem a máquina de estados finitos que determina se uma cruz ainda está “fresca”.
- `StartProtection()` é chamado em `OnStarted` para que os controles de risco integrados de StockSharp monitorem a posição aberta, assim como o hard stop-loss de MetaTrader.
- Os comentários embutidos são escritos em inglês e descrevem cada etapa da conversão para facilitar a manutenção.
