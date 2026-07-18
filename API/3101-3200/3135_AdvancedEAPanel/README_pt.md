# Estratégia de Advanced EA Panel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do StockSharp do utilitário **Advanced EA Panel** do MQL5. O assessor especialista original fornecia um painel de trading manual com análises de múltiplos períodos, gestão de pivôs e botões de negociação rápida. A implementação em C# recria essas capacidades analíticas dentro de uma estratégia automatizada para que permaneçam disponíveis sem um painel de controle no gráfico.

## Principais funcionalidades

- Agrega nove períodos (M1 … MN1) e rastreia votos de EMA(3/6/9), SMA(50/200), CCI(14) e RSI(21) para cada horizonte.
- Calcula níveis de pivô floor-trader, Woodie ou Camarilla em uma série de velas configurável.
- Monitora a volatilidade com um feed ATR e registra cada mudança significativa.
- Mantém um painel de risco interno calculando a distância do stop, distância de recompensa e relação risco/recompensa ao vivo para a posição ativa.
- Suporta execução automática de ordens quando o voto multi-período excede um limiar configurável. Negociações opostas são achatadas antes de reverter, exatamente como ao pressionar os botões do painel.
- Aproveita `StartProtection` para que os guardas de stop-loss e take-profit sobrevivam a reinicializações, refletindo a lógica de proteção do painel original.

## Lógica de negociação

1. Cada assinatura de período produz valores de indicadores para EMA(3/6/9), SMA(50/200), CCI(14) e RSI(21). Um voto altista é adicionado quando o fechamento está acima das médias móveis, CCI está acima de +100 e RSI está acima de 60. Votos baixistas são produzidos para as condições opostas. Leituras neutras não contribuem para a pontuação.
2. A pontuação total nos períodos prontos é comparada com `DirectionalThreshold`. Pontuações ≥ limiar geram um sinal de **Compra**; pontuações ≤ –limiar geram um sinal de **Venda**.
3. Quando o trading automático está habilitado, a estratégia:
   - Fecha a posição oposta com `ClosePosition()` antes de enviar a ordem de reversão.
   - Envia uma ordem de mercado dimensionada de acordo com `Volume`, arredondada para o `Security.VolumeStep` mais próximo.
   - Depende de `StartProtection` para anexar brackets de stop-loss/take-profit expressos em pips.
4. O ATR da série de velas primária é registrado. Qualquer mudança além da precisão de arredondamento imprime um novo relatório de volatilidade.
5. Os níveis de pivô são recalculados sempre que o período de pivô produz uma vela finalizada. O log mostra PP, R1–R4 e S1–S4 para que possam ser usados como níveis discricionários ou exportados para dashboards.

## Parâmetros

| Nome | Descrição | Grupo | Padrão |
| --- | --- | --- | --- |
| `Volume` | Volume de trading em lotes. Arredondado para `VolumeStep` antes de enviar ordens. | Negociação | 1.0 |
| `StopLossPips` | Distância da entrada ao stop-loss expressa em steps de preço. `0` desabilita o stop. | Risco | 50 |
| `TakeProfitPips` | Distância da entrada ao take-profit em steps de preço. `0` desabilita o take. | Risco | 100 |
| `VolatilityPeriod` | Comprimento de lookback ATR usado para registro de volatilidade. | Volatilidade | 14 |
| `PrimaryCandleType` | Tipo de vela que impulsiona cálculos ATR e desenho no gráfico. | Geral | Velas de 15 minutos |
| `PivotCandleType` | Tipo de vela usado para recálculo de níveis de pivô. | Geral | Velas de 1 hora |
| `DirectionalThreshold` | Pontuação absoluta necessária para acionar um sinal de Compra/Venda. | Sinais | 3 |
| `AutoTradingEnabled` | Habilita a execução automática de sinais detectados. | Sinais | true |
| `PivotFormula` | Preset de pivô (`Classic`, `Woodie`, `Camarilla`). | Geral | Classic |

## Gestão de risco

- `StartProtection` anexa brackets baseados em preço calculados de `StopLossPips` e `TakeProfitPips` (convertidos para preço absoluto usando `PriceStep`).
- `_entryPrice`, `_stopPrice` e `_takePrice` são atualizados em preenchimentos para que a estratégia possa registrar risco, recompensa e relação risco/recompensa em pips.
- Se o trading automático estiver desabilitado, o monitor de risco ainda funciona para entradas manuais executadas fora da estratégia.

## Diferenças em relação ao painel MQL5

- O EA original exibia botões e linhas arrastáveis no gráfico; a versão StockSharp expõe a mesma análise através de logs e parâmetros de estratégia. Todos os comentários dentro do código explicam como estender ou conectar os resultados a uma UI se necessário.
- O gerenciamento de posição está automatizado. Clicar em **Comprar**, **Vender**, **Reverter** ou **Fechar** é substituído por `RequestExecution`, `SendOrder` e `ClosePosition()` em reação à pontuação multi-período.
- Points of interest, edições manuais de abas e manipulação de objetos do gráfico não estão portados. Em vez disso, os pivôs são recalculados programaticamente e registrados. Os traders podem consumir o log ou estender a estratégia para desenhar objetos se desejado.
- Volatilidade, métricas de risco e pivôs persistem entre reinicializações porque são recalculados a partir de dados ao vivo em vez de depender de objetos do gráfico.

## Notas de uso

1. Anexe a estratégia a um símbolo e certifique-se de que o conector fornece todos os tipos de velas listados em `PanelTimeFrames`. Dados ausentes atrasarão a geração de sinais até que pelo menos uma vela por período seja finalizada.
2. Ajuste `DirectionalThreshold` para controlar a sensibilidade. Limiares mais altos exigem mais concordância entre períodos antes de negociar.
3. Configure `AutoTradingEnabled = false` para usar o módulo como dashboard informativo enquanto coloca ordens manualmente de outra ferramenta.
4. A classe adiciona renderização de gráfico padrão para velas primárias, ATR e negociações próprias. Remova ou estenda essas chamadas se uma visualização personalizada for necessária.

## Resumo de conversão

- **Ações de UI → Métodos de estratégia.** Manipuladores de botões do painel (`EAPanelClickHandler`, `T0ClickHandler`, etc.) são mapeados para helpers de execução de ordens que preservam o fluxo de compra/venda/reversão/fechamento.
- **Fórmulas de pivô.** Os seletores de MQL5 permitiam fórmulas independentes por nível; este port mantém as combinações preset (`Classic`, `Woodie`, `Camarilla`) que o painel oferecia via seus botões de seleção rápida.
- **Rastreamento de indicadores.** Handles de indicadores nativos do MQL5 são substituídos por `ExponentialMovingAverage`, `SimpleMovingAverage`, `CommodityChannelIndex` e `RelativeStrengthIndex` do StockSharp com callbacks `Bind`.
- **Painel de risco.** Todos os cálculos de risco/recompensa que anteriormente eram renderizados em caixas de edição agora são registrados e podem ser consumidos por qualquer componente de monitoramento.

A estratégia, portanto, preserva a intenção do Advanced EA Panel—consciência situacional centralizada com lógica de reação rápida—enquanto se apresenta como uma estratégia StockSharp totalmente automatizada pronta para otimização ou monitoramento discricionário.
