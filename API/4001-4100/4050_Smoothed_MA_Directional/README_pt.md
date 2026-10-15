# Estratégia Smoothed MA Directional Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta StockSharp de alto nível API do MetaTrader 4 especialistas `oc08_vy_m0moqesu15` da pasta `MQL/8615`. O especialista original alinha sua posição com uma única média móvel suavizada (SMMA) e atribui níveis fixos de stop-loss e take-profit a cada pedido. A versão C# mantém o mesmo comportamento direcional ao adotar componentes idiomáticos StockSharp.

## Ideia de negociação

- **Viés direcional:** O fechamento do preço acima da média móvel suavizada indica uma tendência de alta; fechar abaixo sinaliza uma tendência de baixa.
- **Alinhamento de posição:** A estratégia sempre tenta manter uma única posição na direção da tendência detectada. Se o mercado virar de lado, ele imediatamente inverte a posição.
- **Controle de risco:** Cada entrada é protegida por compensações de stop-loss e take-profit expressas em etapas de preço. O auxiliar `StartProtection` de StockSharp substitui a atribuição manual de SL/TP no código MQ4 original.
- **Estilo de execução:** As ordens são enviadas como ordens de mercado no fechamento da vela, replicando a lógica `OrdersTotal()==0` do especialista MetaTrader.

## Como funciona

1. Na inicialização, a estratégia assina velas do período configurado e vincula um indicador `SmoothedMovingAverage` ao período selecionado.
2. Quando uma vela termina, o valor do indicador é comparado com o fechamento da vela.
3. Se o fechamento for superior ao SMMA e a estratégia for plana ou curta, ele envia uma compra de mercado dimensionada para cobrir a exposição curta (se houver) e abre uma posição longa.
4. Se o fechamento for inferior ao SMMA e a estratégia for plana ou longa, ele envia uma venda de mercado dimensionada para cobrir a exposição longa (se houver) e abre uma posição curta.
5. As distâncias protetoras de stop-loss e take-profit são configuradas uma vez no início usando a segurança atual `PriceStep`. Se ambos os deslocamentos forem definidos como zero, a proteção será desativada.
6. A saída do gráfico (velas, indicadores, negociações) é desenhada automaticamente quando a estratégia é executada em ambientes que expõem uma área do gráfico.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `StopLossPoints` | 100 | Distância de stop-loss em etapas de preço. Defina como `0` para desativar a parada.
| `TakeProfitPoints` | 100 | Distância de lucro em etapas de preço. Defina como `0` para desativar o alvo.
| `MaPeriod` | 12 | Período da média móvel suavizada usada para avaliar a tendência.
| `TradeVolume` | 1 | Volume de ordens de mercado. A estratégia também grava esse valor em `Strategy.Volume` no início.
| `CandleType` | Período de 15 minutos | Tipo de vela (período de tempo) que aciona o indicador e os sinais.

Todos os parâmetros são configuráveis por meio do StockSharp Designer/Runner e incluem intervalos de otimização para testes automatizados.

## Diferenças da versão MetaTrader

- O dimensionamento de lote baseado em margem (`Lots`/`Prots`) foi substituído por um parâmetro `TradeVolume` fixo. Isso mantém o comportamento determinístico e compatível com a abstração do portfólio de StockSharp.
- Stop-loss e take-profit são tratados por `StartProtection` em vez de alterações manuais de pedidos, correspondendo às compensações originais, mas usando primitivos StockSharp.
- A estratégia ignora velas inacabadas para evitar negociações prematuras, espelhando a sinalização `New_Bar` no MQ4.

## Notas práticas

- Certifique-se de que a segurança conectada forneça um `PriceStep` válido. Caso contrário, a estratégia volta para um passo unitário de `1` ao calcular distâncias SL/TP.
- O comprimento do indicador é sincronizado com o valor atual do parâmetro em cada vela, permitindo ajustes de parâmetros ao vivo.
- Para reproduzir o comportamento original, configure o mesmo período de tempo do gráfico que hospedou o especialista MQ4 e mantenha o volume de negociação consistente com o tamanho do contrato desejado.
