# Estratégia de gerenciamento de pedidos ARD Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **Ard Order Management** é uma StockSharp conversão do MetaTrader especialista `ARD_ORDER_MANAGEMENT_EA-BETA_1`. O script original se concentrava em fechar repetidamente as posições existentes antes de fazer novos pedidos e oferecia rotinas auxiliares para atualizações manuais de stop-loss e take-profit. A versão StockSharp mantém essa disciplina ao adicionar automação orientada por indicadores com base no oscilador Stochastic.

A configuração padrão visa a negociação forex intradiária em um gráfico de 5 minutos, mas o tipo de vela é totalmente configurável. Toda a lógica de negociação é executada em velas finalizadas para permanecer fiel ao estilo de execução de fim de barra do especialista de origem.

## Lógica de negociação
- Um oscilador Stochastic com períodos configuráveis de **lookback**, **sinal** e **desaceleração** gera sinais direcionais (padrões: 5/3/3).
- Quando %K fecha **acima do limite de compra** (80 por padrão), a estratégia cancela as ordens pendentes, fecha qualquer exposição curta aberta e entra em uma posição longa com o volume configurado.
- Quando %K fecha **abaixo do limite de venda** (20 por padrão), todas as ordens pendentes são canceladas, a exposição longa aberta é fechada e uma nova posição curta é aberta.
- A estratégia permanece na nova posição até que o sinal oposto seja acionado ou uma saída de proteção seja acionada.

## Gerenciamento de pedidos e riscos
- Antes de cada nova entrada, a estratégia emite ordens de mercado que nivelam totalmente a posição atual, replicando o comportamento `open_order(CLOSE)` do EA.
- `StartProtection` envia automaticamente ordens iniciais de stop-loss e take-profit de acordo com os parâmetros `StopLossPips` e `TakeProfitPips`.
- A lógica final opcional emula o ramo `MODIFY` do EA: cada vela finalizada pode atualizar um nível de stop dinâmico (`ModifyStopLossPips`) e uma meta de lucro flutuante (`ModifyTakeProfitPips`). Quando o preço atinge qualquer um dos níveis finais, a posição é fechada para garantir ganhos ou limitar o risco.
- A conversão de pip usa o `PriceStep` do instrumento (com um ajuste de 10× para símbolos forex de pip fracionário) para que os parâmetros baseados na distância permaneçam consistentes em todos os mercados.

## Parâmetros
- **Volume** – volume de negociação para novas entradas; tamanho adicional é adicionado automaticamente para fechar posições opostas.
- **TakeProfitPips / StopLossPips** – distâncias de proteção iniciais passadas para o módulo de proteção integrado. Defina como zero para desativar qualquer um dos pedidos.
- **ModifyTakeProfitPips / ModifyStopLossPips** – deslocamentos finais (em pips) recalculados após cada vela. Defina como zero para desativar as atualizações finais.
- **StochasticPeriod / SignalPeriod / SlowingPeriod** – configuração do oscilador que espelha a chamada `iStochastic` do especialista original.
- **BuyThreshold / SellThreshold** – níveis de sobrecompra/sobrevenda que desencadeiam reversões longas/curtas.
- **CandleType** – período de tempo ou fonte de dados de vela personalizada que alimenta o indicador.

Cada parâmetro expõe intervalos de otimização sensatos para que você possa testar novamente as configurações alternativas no otimizador StockSharp.

## Notas de uso
- Funciona melhor em instrumentos líquidos onde as paradas baseadas em pip são significativas (principais pares de divisas, CFDs de índices, futuros líquidos).
- Aumente o prazo ao negociar em mercados mais lentos para reduzir o ruído e as falsas reversões.
- Ao executar em contas ativas, verifique se o volume configurado respeita os mínimos do corretor e os tamanhos dos passos.
- A lógica final pode ser desabilitada deixando os parâmetros `Modify*` em zero, reproduzindo efetivamente a manutenção da ordem estática da fonte EA.
- Combine com filtros adicionais (tendência, volatilidade, sessões) se desejar entradas mais seletivas — o código expõe propriedades que podem ser estendidas.

## Detalhes da conversão
- Arquivo de origem: `MQL/9041/ARD_ORDER_MANAGEMENT_EA-BETA_1.mq4`.
- Recriada a lógica do gatilho Stochastic sugerida na rotina `start()` comentada.
- Preservada a disciplina de fechar antes de abrir e a colocação de ordens de proteção por meio do API de alto nível de StockSharp.
- Adicionadas saídas finais opcionais para refletir o bloco manual `MODIFY` do EA, mantendo a implementação orientada por indicadores e baseada em eventos.
