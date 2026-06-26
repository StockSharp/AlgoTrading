# Estratégia de Stochastic CG Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o assessor especialista MetaTrader 5 **Exp_StochasticCGOscillator** para o StockSharp. A conversão mantém a lógica original do oscilador Stochastic Center of Gravity, reconstrói o suavização da linha de disparo e executa negociações usando a API de estratégia de alto nível do StockSharp.

## Como funciona

1. **Pipeline de indicadores** – cada vela finalizada de `CandleType` alimenta o oscilador Stochastic CG personalizado. Os preços medianos impulsionam um loop center-of-gravity, os valores são normalizados sobre as últimas `Length` barras, e uma janela deslizante ponderada produz a linha do oscilador. A linha de disparo é recriada através do mesmo suavização `0.96 * (previous + 0.02)` que o EA aplica.
2. **Amostragem de sinal** – a estratégia inspeciona duas leituras históricas separadas por `SignalBar`. Uma compra é preparada quando a leitura mais antiga (shift `SignalBar + 1`) está acima do trigger enquanto a mais recente (shift `SignalBar`) cruza abaixo. Vendas curtas espelham a lógica na direção oposta.
3. **Gestão de posição** – posições longas são fechadas assim que a leitura mais antiga cai abaixo do trigger, enquanto posições curtas saem quando a leitura mais antiga sobe acima dele. Quando um novo eintry aparece no lado oposto, a posição atual é achatada antes de enviar a ordem de reversão.
4. **Tratamento de risco** – distâncias opcionais de stop-loss e take-profit são expressas em steps do instrumento e avaliadas no preço de fechamento de cada vela processada. Elas refletem os inputs protetores do EA sem depender de ordens pendentes.
5. **Controle de aquecimento** – a estratégia aguarda até que o indicador esteja completamente inicializado (histórico suficiente para o loop CG e o buffer de suavização de quatro valores) antes de emitir sinais, garantindo backtests deterministas.

## Gestão de risco e dimensionamento de posição

- **Stops/metas** – `StopLossPoints` e `TakeProfitPoints` são traduzidos em distâncias absolutas usando `Security.PriceStep`. Um valor de `0` desabilita o limite respectivo.
- **Posição ativa única** – o algoritmo nunca mantém exposição longa e curta ao mesmo tempo. Sinais opostos acionam um fechamento explícito antes de entrar na nova direção.
- **Dimensionamento de posição** – `SizingMode = FixedVolume` sempre negocia `FixedVolume`. `SizingMode = PortfolioShare` converte `DepositShare` do valor do portfólio em contratos usando o último fechamento e `Security.VolumeStep`.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período subscrito para velas e cálculos de indicadores. |
| `Length` | Período do oscilador Stochastic CG (afeta as janelas CG e de normalização). |
| `SignalBar` | Número de velas fechadas atrás usadas para avaliar sinais (`1` reproduz o padrão do EA). |
| `AllowLongEntry` / `AllowShortEntry` | Ativa/desativa entradas longas/curtas. |
| `AllowLongExit` / `AllowShortExit` | Ativa/desativa saídas automáticas para posições longas/curtas. |
| `StopLossPoints` / `TakeProfitPoints` | Distâncias protetoras em steps de preço. Defina como `0` para desabilitar. |
| `FixedVolume` | Tamanho da ordem quando o modo de dimensionamento é volume fixo. |
| `DepositShare` | Fração do portfólio usada no dimensionamento baseado em participação. |
| `SizingMode` | Escolhe entre volume fixo e dimensionamento de posição baseado em participação. |

## Notas de uso

- Alinhe `CandleType` com o período usado pelo indicador original (H8 na versão MQL). Valores maiores de `SignalBar` requerem um aquecimento mais longo porque o buffer de histórico do indicador deve cobrir o shift.
- Stops e metas atuam nos fechamentos de velas; não são ordens intrabarra. Ajuste os valores de pontos para se adequar ao tamanho do tick do instrumento.
- Quando o dimensionamento `PortfolioShare` estiver habilitado, certifique-se de que a valoração do portfólio esteja disponível; caso contrário, a estratégia recorre ao volume fixo.
- O indicador produz valores no intervalo `[-1, 1]` como a implementação original, permitindo reutilizar filtros baseados em limiar familiares se desejado.

## Diferenças em relação ao EA original

- As ordens de mercado são enviadas imediatamente sem o parâmetro `Deviation_`; o tratamento de slippage é delegado à camada de execução do StockSharp.
- O gerenciamento de dinheiro é simplificado para dois modos (`FixedVolume` e `PortfolioShare`). As opções adicionais de dimensionamento baseadas em margem do EA não são reproduzidas.
- Carimbos de tempo de ordens pendentes (`UpSignalTime` / `DnSignalTime`) são desnecessários porque as estratégias StockSharp trabalham em velas completadas e executam sincronicamente.
