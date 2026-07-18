# Estratégia de identificação do ciclo Ciclope
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia transporta o consultor especialista MetaTrader **Cyclops v1.2** junto com seu indicador proprietário *CycleIdentifier* para o alto nível de StockSharp API. O algoritmo suaviza os preços de fechamento com uma média móvel suavizada (SMMA), mede a volatilidade recente por meio de uma faixa real média de retrospectiva longa e marca pontos de inflexão do ciclo quando o preço se afasta o suficiente da oscilação mais recente. As principais reversões de ciclo geram novas entradas, enquanto as reversões menores oferecem sinais de saída opcionais.

Um filtro de atraso zero configurável valida a inclinação da série suavizada. O filtro pode funcionar diretamente em dados de preços suavizados ou em um RSI estilo Wilder derivado da mesma série. Confirmação adicional está disponível através de um indicador Momentum clássico, e a negociação pode ser limitada a uma janela específica de dia/hora da semana.

## Lógica de sinal

- **Detecção de ciclo** – A máquina de estado interna rastreia os últimos máximos e mínimos do preço suavizado. Quando o preço ultrapassa o limite adaptativo (intervalo médio × *Comprimento*), a estratégia marca um ciclo menor. Um múltiplo maior (*MajorCycleStrength*) é necessário para sinalizar um ciclo principal.
- **Entradas** – Principais ciclos de alta (`MajorBuy`) abrem posições longas; os principais ciclos de baixa (`MajorSell`) abrem posições vendidas. As posições ativas são fechadas automaticamente antes de reverter para o lado oposto.
- **Saídas opcionais** – Quando *UseExitSignal* está ativado, as negociações lucrativas podem fechar no sinal de ciclo menor correspondente (`MinorSellExit` para posições longas, `MinorBuyExit` para posições curtas) se nenhum ciclo principal oposto estiver presente.
- **Filtro de atraso zero** – Se *UseCycleFilter* estiver ativado, um filtro de suavização de atraso zero deve confirmar a inclinação (aumentando para posições compradas, caindo para posições vendidas). A origem do filtro é selecionada por *CycleFilterMode* (preço suavizado ou RSI).
- **Filtro Momentum** – Com *UseMomentumFilter* ativado, as entradas exigem `Momentum ≥ MomentumTriggerLong` para posições compradas e `Momentum ≤ MomentumTriggerShort` para operações curtas.

## Gestão comercial

- **Metas fixas** – *TakeProfitPips* e *StopLossPips* definem saídas fixas opcionais em pips de instrumento.
- **Break-even** – Quando *BreakEvenTrigger* pips de lucro são alcançados, o stop é puxado para a entrada ± um pip.
- **Trailing** – *TrailingStopTrigger* ativa um trailing stop que segue o preço em *TrailingStopPips* assim que a distância de disparo for alcançada.
- **Controle de sessão** – Se *UseTimeRestriction* for verdadeiro, novas posições serão permitidas somente antes de `DayEnd` (0=domingo) e até `HourEnd` (inclusive) desse dia. As negociações existentes ainda são gerenciadas posteriormente.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume de pedidos usado para entradas. |
| `PriceActionFilter` | Comprimento da média móvel suavizada aplicada ao preço de fechamento. |
| `Length` | Multiplicador aplicado à faixa média para detectar ciclos menores. |
| `MajorCycleStrength` | Multiplicador que separa as oscilações maiores das menores. |
| `UseCycleFilter` | Ativa a confirmação da inclinação com atraso zero. |
| `CycleFilterMode` | Seleciona entrada de atraso zero: preço suavizado (`Sma`) ou RSI (`Rsi`). |
| `FilterStrengthSma` | Comprimento do filtro de atraso zero quando o preço suavizado é usado. |
| `FilterStrengthRsi` | Duração e RSI período quando o filtro depende de valores RSI. |
| `UseMomentumFilter` | Ativa ou desativa a confirmação do impulso. |
| `MomentumPeriod` | Comprimento do indicador de impulso. |
| `MomentumTriggerLong` | Momentum mínimo necessário para entradas longas. |
| `MomentumTriggerShort` | Momentum máximo permitido para entradas curtas. |
| `UseExitSignal` | Permite saídas baseadas em ciclos menores quando rentáveis. |
| `UseTimeRestriction` | Limita a negociação à janela configurada de dia da semana/hora. |
| `DayEnd` | Último dia da semana em que novas entradas são permitidas. |
| `HourEnd` | Última hora do último dia de negociação para novas entradas. |
| `BreakEvenTrigger` | Lucro em pips necessário para ativar o ponto de equilíbrio. |
| `TrailingStopTrigger` | Lucro em pips necessário para iniciar o trailing. |
| `TrailingStopPips` | Distância em pips mantida pelo trailing stop. |
| `TakeProfitPips` | Distância fixa de lucro em pips. |
| `StopLossPips` | Distância fixa de stop-loss em pips. |
| `CandleType` | Prazo principal que alimenta a estratégia. |

## Diferenças em relação ao original EA

- O intervalo médio é estimado com um intervalo médio verdadeiro de 250 períodos multiplicado por *Comprimento*, fornecendo um comportamento equivalente ao intervalo contínuo alto/baixo usado em MQL.
- A confirmação do impulso usa o valor real do indicador (o script MQL comparado com o multiplicador pip `bm`, desativando efetivamente o filtro).
- A suavização de atraso zero é implementada com os mesmos coeficientes recursivos, mas expressos em aritmética decimal. O modo RSI usa um Wilder RSI cujo período é igual a *FilterStrengthRsi*.

## Notas de uso

1. Selecione o instrumento e vincule o parâmetro `CandleType` ao período de tempo desejado.
2. Defina as configurações de risco e sessão para corresponder ao ambiente do seu corretor.
3. Habilite *UseCycleFilter* ou *UseMomentumFilter* quando uma confirmação mais rigorosa for necessária; desative-os para entradas mais rápidas, porém mais barulhentas.
4. A estratégia mantém no máximo uma posição aberta. Os sinais de ciclo oposto fecham a posição atual antes que uma nova seja avaliada.
