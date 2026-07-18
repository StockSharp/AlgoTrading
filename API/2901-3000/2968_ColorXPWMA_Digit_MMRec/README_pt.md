# Estratégia Color XPWMA Digit MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Color XPWMA Digit MMRec** replica o assessor especializado MetaTrader `Exp_ColorXPWMA_Digit_MMRec`. Ela usa o indicador ColorXPWMA Digit para identificar pontos de inflexão de tendência e envolve a lógica do contador de gestão de dinheiro original. O indicador constrói uma média móvel ponderada por potência (PWMA) que é opcionalmente suavizada por um método de média móvel selecionado. A inclinação da linha suavizada é convertida em cores discretas: `2` para inclinação ascendente, `0` para inclinação descendente e `1` quando a direção é plana.

As decisões de trading são tomadas após as cores do indicador serem avaliadas em uma barra histórica configurável (`SignalBar`). Quando a cor anterior (`SignalBar + 1`) era altista (2), mas a barra em `SignalBar` não mantém mais a cor altista, a estratégia fecha posições vendidas e opcionalmente abre uma nova posição comprada. A lógica inversa se aplica quando a cor histórica era baixista (0), mas a barra mais recente não mantém mais essa cor baixista.

## Lógica do indicador
- **Média móvel ponderada por potência** – cada barra recebe um peso `(period - index)^power`. Potências maiores enfatizam as amostras mais recentes.
- **Suavização** – a série ponderada é passada por uma média móvel suavizadora. Os métodos suportados incluem SMA, EMA, SMMA, LWMA, Jurik, T3 e Kaufman AMA. As opções JurX, Parabólico e VIDYA são aproximadas com suavização exponencial porque o StockSharp não expõe implementações diretas.
- **Codificação de cor** – o sinal da inclinação suavizada define o buffer de cor que aciona entradas e saídas.
- **Arredondamento de dígitos** – o valor final pode ser arredondado para um número fixo de dígitos para corresponder ao comportamento original de "Digit".

## Regras de trading
1. **Falha de continuação altista**
   - Condição: a cor em `SignalBar + 1` é igual a `2` (altista) e a cor em `SignalBar` é diferente de `2`.
   - Ação: fechar vendidos ativos; se entradas compradas são permitidas, abrir uma nova posição comprada dimensionada pelo contador de gestão de dinheiro.
2. **Falha de continuação baixista**
   - Condição: a cor em `SignalBar + 1` é igual a `0` (baixista) e a cor em `SignalBar` é diferente de `0`.
   - Ação: fechar comprados ativos; se entradas vendidas são permitidas, abrir uma nova posição vendida dimensionada pelo contador.

As ordens são sempre executadas no fechamento da vela que produziu o sinal. Ao mudar de direção, a estratégia fecha o engajamento oposto e imediatamente abre a nova posição em uma única ordem a mercado.

## Contador de gestão de dinheiro
A estratégia mantém um histórico contínuo de resultados de operações fechadas para comprados e vendidos. Antes de abrir uma nova operação, inspeciona os resultados mais recentes de `BuyTotalTrigger` ou `SellTotalTrigger`:

- Se o número de operações perdedoras nessa janela atinge o respectivo gatilho de perda (`BuyLossTrigger` ou `SellLossTrigger`), o tamanho da posição é reduzido para `ReducedVolume`.
- Caso contrário, o `NormalVolume` padrão é usado.

Isso reproduz o comportamento das rotinas originais `BuyTradeMMRecounterS` e `SellTradeMMRecounterS`.

## Parâmetros
| Grupo | Parâmetro | Descrição |
| --- | --- | --- |
| Geral | `CandleType` | Período usado tanto para cálculos do indicador quanto para decisões de trading. |
| Indicador | `IndicatorPeriod` | Período da média móvel ponderada por potência. |
| Indicador | `IndicatorPower` | Expoente aplicado aos pesos. Valores maiores enfatizam as barras mais recentes. |
| Indicador | `SmoothingMethod` | Método de média móvel usado para suavização. JurX, ParMa e Vidya recorrem a uma média exponencial. |
| Indicador | `SmoothingLength` | Comprimento da média móvel de suavização. |
| Indicador | `SmoothingPhase` | Parâmetro de fase encaminhado para suavizadores que o suportam. |
| Indicador | `AppliedPrices` | Preço fonte usado pelo indicador (fechamento, abertura, alto, baixo, etc.). |
| Indicador | `RoundingDigits` | Número de dígitos decimais usados para arredondar a saída do indicador. |
| Lógica | `SignalBar` | Deslocamento histórico (em barras) usado ao ler o buffer de cor. |
| Permissões | `EnableBuyEntries` / `EnableSellEntries` | Permitir abertura de posições compradas/vendidas. |
| Permissões | `EnableBuyExits` / `EnableSellExits` | Permitir fechamento de comprados/vendidos. |
| Gestão de dinheiro | `NormalVolume` | Tamanho de ordem padrão. |
| Gestão de dinheiro | `ReducedVolume` | Tamanho de ordem aplicado após uma sequência de perdas. |
| Gestão de dinheiro | `BuyTotalTrigger`, `BuyLossTrigger` | Número de operações compradas recentes a inspecionar e limite de perda para mudar para o volume reduzido. |
| Gestão de dinheiro | `SellTotalTrigger`, `SellLossTrigger` | Mesma lógica para operações vendidas. |
| Gestão de risco | `StopLossPoints`, `TakeProfitPoints` | Distâncias de proteção opcionais (pontos) aplicadas através de `StartProtection` se não forem zero. |

## Notas práticas
- Mantenha `SignalBar = 1` para imitar o comportamento padrão do Assessor Especializado e garantir que os sinais sejam avaliados em velas completamente fechadas.
- A estratégia armazena apenas os resultados mais recentes necessários para o contador, evitando crescimento descontrolado de memória.
- Como o StockSharp executa ordens de forma assíncrona, a estratégia assume preenchimentos ao preço de fechamento da vela ao atualizar os contadores de perda. Isso reflete como o especialista MQL original funcionou com dados históricos.
- As opções de suavização JurX, ParMa e Vidya são aproximações que usam suavização exponencial internamente. Se precisar dos filtros proprietários originais, implemente classes de indicadores personalizadas e conecte-as à estratégia.
