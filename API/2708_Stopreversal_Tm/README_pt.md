# Estratégia Stopreversal Tm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Stopreversal Tm é uma tradução direta do consultor especialista original de MetaTrader 5 `Exp_Stopreversal_Tm.mq5`. A ideia de negociação segue o indicador personalizado Stopreversal, que mantém um trailing stop dinâmico em torno do preço e gera alertas de reversão sempre que o preço cruza esse limite de rastreamento. A estratégia opera em um único instrumento e um único feed de candles e é projetada para negociação de reversão de tendência com um filtro de sessão definido pelo usuário.

## Geração de sinais
O indicador Stopreversal calcula um preço de referência a partir do modo de preço aplicado selecionado e então ajusta um nível de trailing stop por `Sensitivity` (o parâmetro `nPips`). Sempre que o novo preço aplicado cruza acima do trailing stop enquanto a barra anterior estava abaixo, um sinal altista é produzido. Inversamente, um sinal baixista aparece quando o novo preço cai abaixo do trailing stop após ter estado acima. Cada sinal altista solicita simultaneamente o fechamento de posições vendidas existentes e a abertura de uma nova posição comprada, enquanto cada sinal baixista fecha comprados e abre vendidos.

Para reproduzir o comportamento da implementação original do MetaTrader, a estratégia pode atrasar a execução de sinais por várias barras completadas (`Signal Bar Delay`). Isso replica a entrada `SignalBar` do consultor especialista e impede a negociação no candle ainda em formação.

## Filtro de sessão e gerenciamento de posição
O consultor especialista permitia negociações apenas dentro de uma janela de tempo especificada. A estratégia convertida mantém a mesma lógica: quando o sinalizador `Use Time Filter` está habilitado, as ordens são permitidas apenas dentro da sessão configurada por `Start Hour/Minute` e `End Hour/Minute`. Se o horário atual sair da janela permitida, qualquer posição aberta é fechada imediatamente. As saídas impulsionadas por sinais permanecem ativas mesmo quando a sessão está desabilitada.

A estratégia trabalha em posições líquidas. Uma ação de fechamento é sempre executada antes de uma entrada oposta, garantindo que a direção muda sem exposições sobrepostas.

## Parâmetros
- **Allow Buy Entries / Allow Sell Entries** – habilitar ou desabilitar a abertura de novas posições compradas ou vendidas quando o sinal correspondente é recebido.
- **Allow Long Exits / Allow Short Exits** – controlar se os sinais opostos podem fechar posições existentes.
- **Use Time Filter** – ativa a janela de sessão de negociação.
- **Start Hour / Start Minute / End Hour / End Minute** – define o início inclusivo e o fim exclusivo da janela de negociação. O filtro de tempo suporta sessões noturnas onde o horário de término é anterior ao horário de início.
- **Sensitivity (`nPips`)** – distância relativa (expressa como multiplicador, ex.: `0.004 = 0.4%`) usada para mover o trailing stop mais perto ou mais longe do preço.
- **Signal Bar Delay (`SignalBar`)** – número de candles completados a aguardar antes de agir sobre um sinal. `0` executa imediatamente no candle de fechamento, `1` reproduz o comportamento padrão de agir na barra anterior.
- **Candle Type** – período da assinatura de candles usada para os cálculos do indicador.
- **Applied Price** – escolha da série de preço (fechamento, abertura, preço mediano, modos de seguimento de tendência, preço Demark, etc.) que alimenta o cálculo do trailing stop.

## Notas de implementação
- O indicador é implementado diretamente dentro da estratégia sem depender de buffers externos, garantindo que a lógica do trailing stop `nPips` corresponda ao código MQL5 original.
- O gerenciamento de sessão e o sequenciamento de sinais seguem o especialista original, incluindo a prioridade de fechar a exposição existente antes de abrir novas negociações.
- A conversão foca na API de alto nível do StockSharp: assinaturas de candles, fila de sinais atrasados e ordens de mercado (`BuyMarket` / `SellMarket`). As características de gerenciamento de dinheiro vinculadas às métricas de conta do MetaTrader foram omitidas porque as estratégias do StockSharp já operam com tamanhos de posição explícitos.
