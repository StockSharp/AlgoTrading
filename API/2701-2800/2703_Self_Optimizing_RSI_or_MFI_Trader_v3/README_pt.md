# Operador RSI ou MFI Autoótimo v3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia porta o consultor especialista de MetaTrader "Self Optimizing RSI or MFI Trader" para a API de alto nível do StockSharp. Em cada candle finalizado o algoritmo realiza backtesting de uma janela deslizante de barras históricas e encontra os limites de sobrecompra e sobrevenda mais lucrativos para o oscilador selecionado. As negociações ao vivo são realizadas apenas quando o valor atual do oscilador cruza o limiar de melhor desempenho na mesma direção que a vantagem histórica, opcionalmente sem exigir um cruzamento no modo "agressivo". As saídas de posição dependem de stops e alvos baseados em ATR ou de distância fixa com uma etapa de ponto de equilíbrio opcional.

## Dados de mercado
- Funciona com qualquer instrumento que forneça candles OHLC e volume (MFI requer volume).
- Usa o período especificado pelo parâmetro `CandleType`. O padrão são candles de 15 minutos, mas você pode anexar qualquer período suportado pelo adaptador da plataforma.

## Indicadores
- **Relative Strength Index (RSI)** ou **Money Flow Index (MFI)** dependendo do parâmetro `IndicatorChoice`. Ambos compartilham o mesmo comprimento de médias.
- **Average True Range (ATR)** para dimensionamento de stop-loss/take-profit baseado em ATR quando `UseDynamicTargets` está habilitado.

## Lógica de negociação
1. Manter um histórico contínuo de `OptimizingPeriods` + 1 candles finalizados com seus valores de oscilador e preços de fechamento.
2. Para cada nível inteiro entre `IndicatorBottomValue` e `IndicatorTopValue` a estratégia simula negociações na janela histórica:
   - Simulação vendida: contar quantas vezes o oscilador cruzou abaixo do nível e se um stop-loss ou take-profit vendido teria sido atingido primeiro.
   - Simulação comprada: contar quantas vezes o oscilador cruzou acima do nível e quão lucrativas as negociações teriam sido.
3. Escolher o limiar que entregou o maior lucro simulado para cada direção. Se `TradeReverse` estiver habilitado, as pontuações de lucratividade são trocadas para que a direção oposta seja favorecida.
4. Quando o oscilador ao vivo cruza o melhor nível na direção lucrativa (ou imediatamente quando `UseAggressiveEntries` é verdadeiro) a estratégia abre uma posição, respeitando `OneOrderAtATime`.
5. Gerenciamento de saída:
   - Os níveis de stop-loss e take-profit são calculados a partir de múltiplos de ATR (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`) ou de distâncias fixas em pontos (`StaticStopLossPoints`, `StaticTakeProfitPoints`).
   - `UseBreakEven` move o stop para o preço de entrada mais `BreakEvenPaddingPoints` assim que o lucro não realizado atingir `BreakEvenTriggerPoints`.
   - As posições são fechadas quando os preços de stop-loss ou take-profit são cruzados.

## Gestão de risco
- **Dimensionamento dinâmico:** quando `UseDynamicVolume` é verdadeiro a estratégia arrisca `RiskPercent` do valor atual do portfólio. O cálculo converte a distância do stop em risco monetário usando o `PriceStep` e `StepPrice` do instrumento.
- **Dimensionamento estático:** quando desabilitado, `BaseVolume` lotes são negociados em cada entrada.
- **Proteção de ponto de equilíbrio:** garante que as negociações vencedoras sejam protegidas assim que lucro suficiente tiver sido acumulado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OptimizingPeriods` | Número de barras usadas para a otimização contínua em amostra (padrão 144). |
| `IndicatorChoice` | Escolhe RSI ou MFI como o oscilador principal. |
| `IndicatorPeriod` | Período de médias para o oscilador e ATR. |
| `IndicatorTopValue` / `IndicatorBottomValue` | Limites de busca para níveis de limiar (tipicamente 0–100). |
| `UseAggressiveEntries` | Se verdadeiro, permite entradas sem um cruzamento confirmado. |
| `TradeReverse` | Troca as pontuações de lucratividade para negociar o lado historicamente perdedor. |
| `OneOrderAtATime` | Impede a abertura de uma nova posição enquanto outra está ativa. |
| `UseDynamicTargets` | Alterna entre stops/alvos baseados em ATR e de ponto fixo. |
| `StopLossAtrMultiplier`, `TakeProfitAtrMultiplier` | Multiplicadores de ATR para saídas dinâmicas. |
| `StaticStopLossPoints`, `StaticTakeProfitPoints` | Distâncias em pontos para saídas fixas. |
| `UseBreakEven`, `BreakEvenTriggerPoints`, `BreakEvenPaddingPoints` | Configurar o comportamento do stop de ponto de equilíbrio. |
| `UseDynamicVolume`, `RiskPercent`, `BaseVolume` | Controlar a lógica de dimensionamento de posição. |
| `CandleType` | Período para otimização e negociação. |

## Notas de implementação
- A estratégia usa o pipeline `SubscribeCandles().Bind(...)` do StockSharp, portanto só é executada em candles completados.
- `OneOrderAtATime` deve permanecer habilitado ao negociar em uma conta de netting, porque a implementação rastreia uma única posição agregada.
- As saídas baseadas em ATR requerem um valor ATR válido; a estratégia ignorará a negociação até que o indicador esteja totalmente formado.
- Ao usar MFI, certifique-se de que o feed de dados forneça volume, caso contrário o indicador retorna zero e nenhuma negociação será gerada.

## Dicas de otimização
- Otimize `OptimizingPeriods`, o período do oscilador e os multiplicadores de ATR juntos para corresponder ao regime de volatilidade do instrumento.
- Diferentes ativos podem se beneficiar de intervalos de nível mais estreitos (p. ex., 20–80) para reduzir o ruído.
- Considere testes prospectivos com análise walk-forward porque a estratégia adapta os limiares continuamente.

## Uso
1. Adicione a estratégia a um conector no Designer ou execute-a programaticamente.
2. Defina o instrumento desejado, a carteira e os valores dos parâmetros.
3. Inicie a estratégia; ela começará a negociar assim que candles suficientes sejam acumulados para a otimização.

## Limitações
- A otimização histórica ocorre em cada barra e pode ser intensiva em CPU para `OptimizingPeriods` muito grandes ou amplos intervalos de nível.
- Como os níveis são inteiros, limiares de granularidade fina (p. ex., 70.5) não são testados.
- A abordagem assume que o passado recente permanece preditivo; mudanças repentinas de regime podem degradar o desempenho, então monitore os resultados ao vivo e ajuste a configuração quando necessário.
