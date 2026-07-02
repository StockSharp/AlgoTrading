# Sistema oscilador de vórtice 4153
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o especialista MetaTrader 4 "Vortex Oscillator System" usando a estratégia de alto nível de StockSharp API. Ele deriva um oscilador Vortex normalizado combinando os componentes padrão do indicador Vortex e reage sempre que o momento escapa de uma banda neutra configurável. O algoritmo negocia um único símbolo e trabalha sempre com posições totalmente fechadas ou invertidas.

## Regras de negociação
- Uma assinatura de vela definida por **CandleType** alimenta um indicador Vortex com período **VortexLength**. O oscilador é calculado como `(VI+ - VI-) / (VI+ + VI-)`, que mantém as leituras na faixa `[-1, 1]`.
- Uma configuração longa é armada quando o oscilador cai abaixo de **BuyThreshold** e, se **UseBuyStopLoss** estiver habilitado, permanece acima de **BuyStopLossLevel**. Uma configuração curta é armada quando o oscilador sobe acima de **SellThreshold** e, se **UseSellStopLoss** estiver habilitado, permanece abaixo de **SellStopLossLevel**.
- Sempre que o oscilador volta para dentro da banda neutra delimitada por **BuyThreshold** e **SellThreshold**, ambas as configurações são limpas, portanto a próxima quebra deve acontecer a partir de um estado neutro.
- Se uma configuração longa estiver ativa enquanto a posição atual estiver plana ou curta, a estratégia envia uma compra de mercado para lotes de **Volume** mais qualquer quantidade necessária para cobrir uma venda existente. As configurações curtas refletem esse comportamento vendendo lotes de **Volume** mais a quantidade longa pendente.
- As posições abertas podem ser fechadas sem uma configuração oposta: se **UseBuyStopLoss** estiver habilitado e o oscilador tocar em **BuyStopLossLevel** a negociação longa é liquidada; **UseBuyTakeProfit** sai de uma posição comprada quando o oscilador excede **BuyTakeProfitLevel**. Verificações equivalentes usando **SellStopLossLevel** e **SellTakeProfitLevel** gerenciam posições vendidas quando seus respectivos botões de alternância estão habilitados.

## Parâmetros
- **VortexLength** – número de velas usadas para calcular os valores VI+ e VI-.
- **CandleType** – período de tempo ou tipo de dados solicitado à fonte de dados de mercado.
- **Volume** – tamanho base do pedido para novas entradas; pedidos de reversão adicionam automaticamente a quantidade necessária para nivelar a posição atual.
- **BuyThreshold** – nível do oscilador que arma uma configuração longa uma vez violada.
- **UseBuyStopLoss** – requer que o oscilador permaneça acima de **BuyStopLossLevel** antes que uma entrada longa possa ser armada.
- **BuyStopLossLevel** – nível do oscilador que fecha imediatamente uma posição longa quando o filtro de parada está habilitado.
- **UseBuyTakeProfit** – alterna o take-profit baseado no oscilador para negociações longas.
- **BuyTakeProfitLevel** – nível do oscilador que realiza lucros em posições longas quando o filtro de take-profit está ativo.
- **SellThreshold** – nível do oscilador que arma uma configuração curta uma vez violada.
- **UseSellStopLoss** – requer que o oscilador permaneça abaixo de **SellStopLossLevel** antes que uma entrada curta possa ser armada.
- **SellStopLossLevel** – nível do oscilador que fecha imediatamente uma posição curta quando o filtro de parada está habilitado.
- **UseSellTakeProfit** – alterna o take-profit baseado no oscilador para negociações curtas.
- **SellTakeProfitLevel** – nível do oscilador que realiza lucros em posições vendidas quando o filtro de take-profit está ativo.

## Notas adicionais
- A estratégia desenha velas e executa negociações no gráfico automaticamente; a lógica do oscilador interno não adiciona painéis personalizados.
- Como o oscilador é normalizado, os limites padrão `-0.75`, `0.75`, `-1.00` e `1.00` são traduzidos diretamente do consultor especialista original e podem ser otimizados usando o sistema de parâmetros de StockSharp.
- A lógica nunca mantém posições compradas e vendidas simultâneas; cada reversão fecha primeiro a exposição atual e depois abre o lado oposto em uma única ordem de mercado.
