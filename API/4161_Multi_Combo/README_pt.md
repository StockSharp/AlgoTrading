# Estratégia combinada de múltiplas estratégias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Multi Strategy Combo Strategy** é uma conversão C# do consultor especialista MetaTrader 4 "Multi-Strategy iFSF". O EA original combina vários indicadores (MA, RSI, MACD, Stochastic, SAR) e os envolve com tendência, faixa Bollinger e filtros de ruído. A porta StockSharp preserva a mesma ideia usando fluxos `SubscribeCandles().Bind(...)` de alto nível e classes de indicadores. Cada indicador habilitado produz um voto de COMPRA/VENDA; somente quando todos os votos concordam é que a estratégia executa uma ordem. Filtros adicionais emulam os modos de combinação do EA.

## Lógica central
* **Mecanismo de consenso** – Médias móveis, RSI, MACD, Stochastic e Parabolic SAR fornecem, cada uma, um sinal discreto. Se todos os indicadores habilitados concordarem em COMPRA (ou VENDA), o consenso torna-se altista (ou baixista).
* **Fator de combinação (1–3)** – Espelha a lógica `Combo_Trader_Factor` do EA. Cada fator combina consenso com ADX detecção de tendência e Bollinger confirmação de intervalo de maneira diferente:
  * O *Fator 1* prefere condições de tendência. Os estados de intervalo dependem de Bollinger reversões quando ativados.
  * O *Fator 2* requer uma confirmação mais forte: os filtros de tendência e intervalo devem concordar com o consenso.
  * O *Fator 3* é a variante mais rigorosa, exigindo alinhamento entre todos os módulos ativos.
* **Detecção de tendências** – ADX em um período de tempo configurável rotula o mercado como tendência de alta/baixa ou variação de alta/baixa.
* **Bollinger filtro** – Usa bandas médias (2σ) e largas (3σ). Sinais longos exigem um salto da banda inferior confirmado por valores recentes de sobrevenda RSI; os shorts refletem o comportamento da faixa superior.
* **Filtro de ruído** – verificação baseada em ATR que bloqueia novas negociações quando a volatilidade é muito pequena (substituição do Volatômetro Damiani).
* **Fecho automático** – Quando ativado, a estratégia sai instantaneamente se o consenso mudar para a direção oposta.

## Indicadores e sinais
* **Médias móveis** – Três MAs configuráveis (método + comprimento). Os modos 1–5 reproduzem as combinações de crossover originais (rápido vs médio, médio vs lento, lógica agregada).
* **RSI** – Os modos 1–4 cobrem verificações de sobrecompra/sobrevenda, impulso, combinadas e de zona. Todos os limites são ajustáveis.
* **MACD** – Quatro modos imitam o EA: inclinação da tendência, cruzamento do histograma abaixo/acima de zero, confirmação combinada e cruzamento do zero da linha de sinal.
* **Stochastic oscilador** – Cruzamento simples de %K vs %D ou cruzamento com limites altos/baixos.
* **Parabolic SAR** – Votação direcional opcional, suportando o comportamento de "lembrar o último sinal" para evitar vários gatilhos por tendência.

## Gestão de risco
* Compensações opcionais de stop-loss/take-profit (distância de preço absoluta) configuradas via `StopLossOffset` e `TakeProfitOffset`.
* Suporte integrado ao trailing stop por meio do auxiliar StockSharp `StartProtection`.
* A proteção diária da posição segue a mecânica básica `Strategy`; nenhum gerenciamento manual de lote é necessário.

## Parâmetros principais
* **Geral** – `ComboFactor`, `CandleType`.
* **Médias móveis** – `UseMa`, `MaMode`, comprimentos/métodos individuais, período de vela, sinalizador "lembrar o último".
* **RSI** – `UseRsi`, `RsiMode`, `RsiPeriod`, níveis de sobrecompra/sobrevenda, limites de zona, sinalizador "lembrar o último".
* **MACD** – `UseMacd`, `MacdMode`, comprimentos de sinal rápido/lento/sinal, período de vela, sinalizador "lembrar do último".
* **Stochastic** – `UseStochastic`, parâmetros de suavização, limites e período de vela.
* **SAR** – `UseSar`, configurações de aceleração, período de vela.
* **Filtro de tendência** – `UseTrendDetection`, `AdxPeriod`, `AdxLevel`, período de vela.
* **Bollinger filtro** – `UseBollingerFilter`, `BollingerPeriod`, desvios médios/amplos, RSI comprimento do intervalo.
* **Filtro de ruído** – `UseNoiseFilter`, `NoiseAtrLength`, `NoiseThreshold`, período de vela.
* **Fechamento automático e risco** – `UseAutoClose`, `AllowOppositeAfterClose`, `StopLossOffset`, `TakeProfitOffset`, `UseTrailingStop`.

Todos os parâmetros são expostos como `StrategyParam<T>` para oferecer suporte à otimização, validação e agrupamento de IU.

## Diferenças em relação ao MT4 EA
* Apenas StockSharp indicadores integrados são usados. A opção original entre ZeroLag e clássico MACD é substituída pela implementação nativa MACD.
* Todas as médias móveis e osciladores operam com base nos preços de fechamento das velas. O tipo de preço e as compensações de turno do MT4 (por exemplo, `FastMa_Price`, `FastMa_Shift`) não estão disponíveis.
* O filtro de ruído Damiani é aproximado com ATR; o comportamento pode ser ajustado via `NoiseThreshold`.
* O gerenciamento de dinheiro e novas tentativas de pedidos são gerenciados por StockSharp (sem loops manuais `OrderSend`). A estratégia funciona com posições agregadas (`BuyMarket`/`SellMarket`).
* O painel de comentários e os objetos gráficos do EA são omitidos; em vez disso, o registro está disponível por meio de `LogInfo`.

## Uso
1. Adicione a classe `MultiStrategyComboStrategy` à sua solução StockSharp e compile.
2. Instancie a estratégia, defina `Security`, `Portfolio` e o `Volume` desejado.
3. Configure prazos para cada indicador se a confirmação de vários prazos for necessária (os padrões seguem as entradas do EA).
4. Opcionalmente, ajuste compensações de parada/tomada, comportamento de rastreamento e limites de filtro.
5. Comece a estratégia. As negociações serão acionadas em velas fechadas quando todos os módulos habilitados concordarem de acordo com o fator de combinação selecionado.

## Notas de conversão
* A estratégia depende exclusivamente de APIs de assinatura de alto nível (`SubscribeCandles().Bind(...)`) – nenhum buffer de indicador manual é usado.
* As guias são usadas para recuo de acordo com as diretrizes do repositório.
* Extensos comentários embutidos destacam como os conceitos de EA são mapeados para o código de StockSharp.
