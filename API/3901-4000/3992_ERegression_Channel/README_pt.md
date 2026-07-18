# Estratégia do Canal de Regressão E
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **E Regression Channel Strategy** reproduz o consultor especialista MetaTrader "e-Regr" usando a estratégia de alto nível de StockSharp API. Ajusta-se a uma curva de regressão polinomial aos preços de fecho recentes, constrói bandas equidistantes a partir do desvio padrão residual e reage quando o preço ultrapassa esses envelopes. A estratégia foi projetada para negociação de reversão à média com paradas de proteção opcionais, um filtro diário de volatilidade e uma janela de negociação intradiária.

## Lógica de negociação
1. Assine o período principal especificado por `Candle Type` e calcule um canal de regressão polinomial nos últimos fechamentos de `Regression Length`.
2. A faixa intermediária é o ajuste de regressão; as bandas superior e inferior são deslocadas por `Std Dev Multiplier` multiplicado pelo desvio padrão residual.
3. Feche qualquer posição comprada existente quando o fechamento da vela cruzar acima da faixa intermediária; fechar posições curtas quando o fechamento cair abaixo dele.
4. Abra uma posição longa (após fechar qualquer exposição curta existente) quando o mínimo da vela atual tocar ou romper a banda inferior.
5. Abra uma posição curta (após nivelar a exposição longa) quando a alta da vela atual tocar ou romper a faixa superior.
6. Opcionalmente, acompanhe as posições abertas usando `Trailing Activation` e `Trailing Distance` assim que o preço se mover o suficiente a favor da negociação.
7. Ignore novas entradas sempre que o intervalo da vela diária anterior exceder o limite `Daily Range Filter` ou o horário atual estiver fora da janela `[Trade Start, Trade End)`.

## Parâmetros
- `Volume` – tamanho da ordem usado para cada entrada no mercado (as posições líquidas são achatadas antes da reversão).
- `Trade Start` / `Trade End` – janela de negociação diária, suporta intervalos noturnos (por exemplo, 21h00–02h00).
- `Regression Length` – número de velas utilizadas para o ajuste da regressão polinomial.
- `Degree` – grau polinomial (1–6) aplicado ao modelo de regressão.
- `Std Dev Multiplier` – multiplicador aplicado ao desvio padrão residual da regressão para formar as bandas.
- `Enable Trailing` – alterna o gerenciamento do trailing stop.
- `Trailing Activation` – número de pontos de movimento favorável necessários antes do início do trailing.
- `Trailing Distance` – buffer de rastreamento mantido quando o rastreamento está ativo (em pontos).
- `Stop Loss` – distância de parada protetora em pontos (0 desativa a parada automática).
- `Take Profit` – distância da meta de lucro protetora em pontos (0 desativa a meta automática).
- `Daily Range Filter` – intervalo máximo permitido da vela diária anterior, expresso em pontos.
- `Candle Type` – período para a série de preços primária (período padrão de 30 minutos).

## Configurações padrão
- `Volume` = 0,1
- `Trade Start` = 03:00
- `Trade End` = 21:20
- `Regression Length` = 250 barras
- `Degree` = 3
- `Std Dev Multiplier` = 1,0
- `Enable Trailing` = falso
- `Trailing Activation` = 30 pontos
- `Trailing Distance` = 30 pontos
- `Stop Loss` = 0 pontos (desativado)
- `Take Profit` = 0 pontos (desativado)
- `Daily Range Filter` = 150 pontos
- `Candle Type` = velas de 30 minutos

## Notas adicionais
- A estratégia usa a última vela finalizada para todas as decisões e nunca negocia várias vezes na mesma barra.
- O trailing interrompe o fechamento de posições por mercado quando o preço atinge o nível de trailing calculado internamente.
- Se o dia anterior for muito volátil (faixa acima do filtro configurado), as posições existentes serão fechadas e novas entradas serão suspensas pelo restante da barra.
- O canal de regressão é redesenhado no gráfico a cada atualização para ajudar a visualizar as bandas média, superior e inferior.
