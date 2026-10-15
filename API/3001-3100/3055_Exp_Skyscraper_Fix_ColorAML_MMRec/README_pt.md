# Estratégia Exp Skyscraper Fix Color AML MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Exp Skyscraper Fix Color AML MMRec é o port do StockSharp do consultor especialista MQL5 *Exp_Skyscraper_Fix_ColorAML_MMRec*. O robô original combina dois indicadores independentes — **Skyscraper Fix** e **Color AML** — e aplica a lógica de gestão monetária MMRec para reduzir o tamanho da ordem após perdas consecutivas. A implementação em C# mantém ambas as fontes de sinais e o dimensionamento adaptativo de posições enquanto usa a API de alto nível do StockSharp para roteamento de ordens.

## Fluxo de trading

1. **Módulo Skyscraper Fix** constrói um canal adaptativo a partir das velas concluídas de `SkyscraperCandleType`. Quando a cor do canal se torna teal (tendência &gt; 0), cada posição vendida pode ser fechada e, se a cor anterior não era teal, um novo trade comprado é aberto. Quando a cor se torna vermelha (tendência &lt; 0), a lógica é espelhada para trades vendidos. A classe auxiliar `SkyscraperFixIndicator` é reutilizada da estratégia `3040_Exp_Skyscraper_Fix_Duplex`.
2. **Módulo Color AML** processa velas de `ColorAmlCandleType`. O `ColorAmlIndicator` traduzido reproduz o nível de mercado adaptativo e emite um código de cor: `2` (altista), `0` (baixista) ou `1` (neutro). O módulo fecha o lado oposto sempre que uma cor altista ou baixista é detectada e abre uma nova posição se a cor mudou em relação à amostra atrasada anterior.
3. **Atraso de sinal** é controlado independentemente para ambos os módulos através de `SkyscraperSignalBar` e `ColorAmlSignalBar`. A estratégia mantém filas de saídas de indicadores e executa ordens somente após o número configurado de velas fechadas, correspondendo ao comportamento `CopyBuffer(..., shift, ...)` no consultor especialista.
4. **Gestão de risco** espelha as distâncias originais de stop/take-profit. Cada módulo define suas próprias distâncias protetoras em passos de preço (ticks). A estratégia as traduz em preços absolutos e, em cada vela concluída, verifica se o intervalo da barra tocou um stop-loss ou take-profit. Se sim, a posição é nivelada com uma ordem de mercado e todos os níveis protetores são limpos.
5. **Gestão monetária MMRec** rastreia perdas consecutivas separadamente para comprados Skyscraper, vendidos Skyscraper, comprados Color AML e vendidos Color AML. Quando a sequência de perdas para uma direção atinge o gatilho correspondente (`*LossTrigger`), o volume muda de `*Mm` para o valor reduzido `*SmallMm`. Uma vez que um trade lucrativo aparece, a sequência é redefinida para zero. Como a estratégia de exemplo é executada em uma única posição líquida, apenas o modo de gestão `Lot` tem efeito prático; outros modos voltam ao dimensionamento direto de lotes.

## Notas de implementação

- O código depende exclusivamente da API de alto nível do StockSharp: subscrições de velas alimentam ambos os indicadores e todas as decisões de trading são executadas através dos auxiliares `BuyMarket`, `SellMarket` e `ClosePosition`.
- Ordens protetoras são implementadas com saídas de mercado em vez de ordens stop/limit separadas. Isso evita conflitos quando ambos os módulos compartilham a mesma posição líquida.
- A gestão monetária usa dados de execução recebidos em `OnOwnTradeReceived` para determinar o resultado do trade anterior. O módulo que abriu a posição armazena seu identificador para que o contador de perdas correto seja atualizado quando a posição for fechada.
- O `ColorAmlIndicator` traduzido armazena velas e valores de suavização em cache para seguir o esquema de suavização exponencial original, incluindo o alpha dinâmico baseado em intervalos fractais e a lógica de codificação de cores (azul para AML crescente, vermelho para queda, cinza caso contrário).
- Números mágicos e configurações explícitas de deslizamento da versão MQL5 não são necessários no StockSharp e, portanto, são omitidos.

## Parâmetros

### Módulo Skyscraper Fix

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `SkyscraperCandleType` | Velas H4 | Período usado para calcular o canal Skyscraper Fix. |
| `SkyscraperLength` | 10 | ATR lookback usado para definir o passo do canal adaptativo. |
| `SkyscraperKv` | 0.9 | Multiplicador aplicado ao tamanho de passo baseado em ATR. |
| `SkyscraperPercentage` | 0 | Offset percentual aplicado à linha média. |
| `SkyscraperMode` | HighLow | Fonte de preço para o envelope (high/low ou close). |
| `SkyscraperSignalBar` | 1 | Número de velas fechadas para atrasar sinais do Skyscraper. |
| `SkyscraperEnableLongEntry` | true | Permitir entradas compradas quando o canal se torna altista. |
| `SkyscraperEnableShortEntry` | true | Permitir entradas vendidas quando o canal se torna baixista. |
| `SkyscraperEnableLongExit` | true | Fechar posições compradas em sinais baixistas do Skyscraper. |
| `SkyscraperEnableShortExit` | true | Fechar posições vendidas em sinais altistas do Skyscraper. |
| `SkyscraperBuyLossTrigger` | 2 | Perdas compradas consecutivas necessárias para mudar para o volume reduzido. |
| `SkyscraperSellLossTrigger` | 2 | Perdas vendidas consecutivas necessárias para mudar para o volume reduzido. |
| `SkyscraperSmallMm` | 0.01 | Volume da ordem usado após atingir o gatilho de perdas. |
| `SkyscraperMm` | 0.1 | Volume padrão da ordem para sinais do Skyscraper. |
| `SkyscraperMmMode` | Lot | Modo de gestão monetária (apenas `Lot` afeta o port em C#). |
| `SkyscraperStopLossTicks` | 1000 | Distância de stop-loss em passos de preço. Valor 0 desabilita o stop. |
| `SkyscraperTakeProfitTicks` | 2000 | Distância de take-profit em passos de preço. Valor 0 desabilita o alvo. |

### Módulo Color AML

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `ColorAmlCandleType` | Velas H4 | Período usado pelo indicador Color AML. |
| `ColorAmlFractal` | 6 | Janela fractal para cálculos de intervalo AML. |
| `ColorAmlLag` | 7 | Lag de suavização para a média exponencial AML. |
| `ColorAmlSignalBar` | 1 | Número de velas fechadas para atrasar sinais do Color AML. |
| `ColorAmlEnableLongEntry` | true | Permitir entradas compradas quando AML se torna altista (cor 2). |
| `ColorAmlEnableShortEntry` | true | Permitir entradas vendidas quando AML se torna baixista (cor 0). |
| `ColorAmlEnableLongExit` | true | Fechar posições compradas em cores AML baixistas. |
| `ColorAmlEnableShortExit` | true | Fechar posições vendidas em cores AML altistas. |
| `ColorAmlBuyLossTrigger` | 2 | Perdas compradas consecutivas antes de mudar para o volume reduzido. |
| `ColorAmlSellLossTrigger` | 2 | Perdas vendidas consecutivas antes de mudar para o volume reduzido. |
| `ColorAmlSmallMm` | 0.01 | Volume da ordem usado após atingir o gatilho de perdas. |
| `ColorAmlMm` | 0.1 | Volume padrão da ordem para sinais do Color AML. |
| `ColorAmlMmMode` | Lot | Modo de gestão monetária (apenas `Lot` afeta o port em C#). |
| `ColorAmlStopLossTicks` | 1000 | Distância de stop-loss em passos de preço. Definir como 0 para desabilitar. |
| `ColorAmlTakeProfitTicks` | 2000 | Distância de take-profit em passos de preço. Definir como 0 para desabilitar. |

## Uso

1. Vincule a estratégia a um portfólio e ao instrumento que deseja negociar. O ativo deve fornecer as séries de velas definidas por `SkyscraperCandleType` e `ColorAmlCandleType`.
2. Ajuste os parâmetros de gestão monetária se seu corretor usar um passo de lote diferente. Como apenas o dimensionamento direto de lotes é aplicado, configure `*Mm` e `*SmallMm` adequadamente.
3. Opcionalmente, modifique as distâncias de stop-loss e take-profit (em ticks) para cada módulo. Definir uma distância como zero desabilita a proteção correspondente.
4. Inicie a estratégia. Ela subscreverá ambos os fluxos de velas, calculará os indicadores e gerenciará entradas e saídas automaticamente de acordo com as regras acima.

O README reflete o comportamento de `CS/ExpSkyscraperFixColorAmlMmrecStrategy.cs` e deve ser usado como documentação de referência para esta implementação do StockSharp.
